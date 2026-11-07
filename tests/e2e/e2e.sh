#!/bin/bash
REPO="$(cd "$(dirname "$0")/../.." && pwd)"
# End-to-end test on Linux: real KeePass 2.62 (Mono) + plugin + a fake "veracrypt" program.
#   e2e.sh PLUGIN_FILE OUT_DIR SCENARIO
# Scenarios:
#   normal   - fake VeraCrypt shows a password window (zenity, same PID) -> password must arrive there
#   nowindow - fake VeraCrypt never shows a window while another program's window ("victim")
#              has the focus -> nothing may be typed anywhere
#   steal    - fake VeraCrypt shows its window, then another window is activated -> the plugin
#              must bring VeraCrypt back or not type; the victim must receive nothing
set -u
PLUGIN="$1"; OUT="$2"; SCENARIO="$3"
KPSRC=${KPSRC:-$REPO/lib/KeePass}
TESTS=$REPO/build/bin/KeePassVeraCryptMount.Tests/Release/KeePassVeraCryptMount.Tests.exe
DISP=:${E2E_DISPLAY:-81}
SECRET='S3cret pass! with spaces & "quotes"'

rm -rf "$OUT"; mkdir -p "$OUT/home" "$OUT/vc"
cp -r "$KPSRC" "$OUT/kp"
cp "$PLUGIN" "$OUT/kp/Plugins/"
touch "$OUT/vc/test volume.hc"

# --- the fake VeraCrypt -----------------------------------------------------------
FAKE="$OUT/vc/veracrypt"
case "$SCENARIO" in
  normal|steal|stealrand|atoff)
    cat > "$FAKE" <<EOF
#!/bin/bash
printf '%s\n' "\$@" > "$OUT/vc/argv.txt"
echo \$\$ > "$OUT/vc/pid.txt"
if [ "$SCENARIO" = steal ]; then
  # Once the fake password window is shown, activate the victim window (simulates the user
  # clicking elsewhere).
  ( xdotool search --sync --onlyvisible --pid \$\$ >/dev/null 2>&1; sleep 0.05; xdotool search --name 'VICTIM' windowactivate >/dev/null 2>&1 ) &
fi
if [ "$SCENARIO" = stealrand ]; then
  # Same, at a random moment 0-1.2 s after the window appears (hits every phase of the plugin).
  D=\$(python3 -c "import random; print(round(random.uniform(0, 1.2), 3))")
  echo \$D > "$OUT/vc/delay.txt"
  ( while ! xdotool search --onlyvisible --pid \$\$ >/dev/null 2>&1; do sleep 0.01; done; sleep \$D; xdotool search --name 'VICTIM' windowactivate >/dev/null 2>&1 ) &
fi
exec zenity --password --title="Enter password for test volume.hc" > "$OUT/vc/typed.txt" 2>/dev/null
EOF
    ;;
  nowindow)
    cat > "$FAKE" <<EOF
#!/bin/bash
printf '%s\n' "\$@" > "$OUT/vc/argv.txt"
sleep 25
EOF
    ;;
esac
chmod +x "$FAKE"

# --- KeePass configuration: plugin options, no update check, short timeout --------------
cat > "$OUT/kp/KeePass.config.xml" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<Configuration xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Application><Start><CheckForUpdate>false</CheckForUpdate><CheckForUpdateConfigured>true</CheckForUpdateConfigured></Start></Application>
  <Custom>
    <Item><Key>veracrypt.exe.location</Key><Value>$FAKE</Value></Item>
    <Item><Key>veracrypt.autotype.wait.timeout</Key><Value>5000</Value></Item>
  </Custom>
</Configuration>
EOF

export HOME="$OUT/home"
AT=1; [ "$SCENARIO" = atoff ] && AT=0
mono "$TESTS" makedb "$OUT/vc/test.kdbx" "master" "$SECRET" "{DB_DIR}/test volume.hc" $AT >/dev/null

Xvfb $DISP -screen 0 1280x900x24 >/dev/null 2>&1 &
XPID=$!
sleep 1.5
export DISPLAY=$DISP
openbox >/dev/null 2>&1 &
WMPID=$!
sleep 1

VICTIM_PID=""
if [ "$SCENARIO" != normal ] && [ "$SCENARIO" != atoff ]; then
  # A window of another program that must never receive the password.
  zenity --entry --title="VICTIM" --text="Nothing must be typed here" > "$OUT/victim.txt" 2>/dev/null &
  VICTIM_PID=$!
  sleep 1.5
fi

( cd "$OUT/kp" && exec mono KeePass.exe "$OUT/vc/test.kdbx" -pw:master ) >"$OUT/keepass.log" 2>&1 &
KPID=$!

# Wait for the main window, select the entry, open the context menu, click "Mount volume".
for i in $(seq 1 60); do
  W=$(xdotool search --onlyvisible --name 'test.kdbx' 2>/dev/null | head -1)
  [ -n "$W" ] && break; sleep 0.5
done
sleep 3
xdotool windowactivate --sync "$W" 2>/dev/null; sleep 0.5
eval "$(xdotool getwindowgeometry --shell "$W")"
import -window root "$OUT/1_keepass.png" 2>/dev/null
# The entry list is to the right of the group tree; first row about 120 px below the window top.
# Absolute position of the first entry row (window placed the same way by openbox each time).
EX=${ENTRY_X:-450}; EY=${ENTRY_Y:-252}
xdotool mousemove "$EX" "$EY" click 1; sleep 0.7
xdotool mousemove "$EX" "$EY" click 3; sleep 1.2
import -window root "$OUT/2_menu.png" 2>/dev/null
if [ "$SCENARIO" != normal ] && [ "$SCENARIO" != atoff ]; then
  # Give the focus to the victim right before the mount starts.
  xdotool key Escape; sleep 0.3
  xdotool search --name 'VICTIM' windowactivate 2>/dev/null; sleep 0.6
  # Trigger the mount through the hidden Ctrl+M item would open the dialog; use the menu instead:
  xdotool windowactivate --sync "$W"; sleep 0.4
  xdotool mousemove "$EX" "$EY" click 3; sleep 1.2
fi
# "Mount volume" is the first item of the context menu.
xdotool mousemove $((EX + 40)) $((EY + 12)) click 1
if [ "$SCENARIO" = atoff ]; then
  # The plugin must explain that Auto-Type is off for this entry, then start VeraCrypt.
  sleep 2.5; import -window root "$OUT/2b_atoff_message.png" 2>/dev/null; xdotool key Return
fi
if [ "$SCENARIO" = nowindow ]; then
  # Immediately put the victim in front, as if the user switched windows.
  sleep 0.3; xdotool search --name 'VICTIM' windowactivate 2>/dev/null
fi
sleep "${E2E_WAIT:-9}"
import -window root "$OUT/3_after.png" 2>/dev/null
xdotool search --onlyvisible --name '.' getwindowname %@ 2>/dev/null | sort -u > "$OUT/windows.txt"

# Close remaining dialogs: the victim gets an Enter so that it prints whatever it received.
if [ -n "$VICTIM_PID" ]; then
  xdotool search --name 'VICTIM' windowactivate --sync key Return 2>/dev/null; sleep 0.8
  kill $VICTIM_PID 2>/dev/null
fi
pkill -f "zenity --password" 2>/dev/null
kill $KPID 2>/dev/null; sleep 1; kill -9 $KPID 2>/dev/null
kill $WMPID $XPID 2>/dev/null

echo "== scenario: $SCENARIO"
echo "== arguments received by the fake VeraCrypt:"; cat "$OUT/vc/argv.txt" 2>/dev/null || echo "(not started)"
echo "== typed into the fake VeraCrypt password window:"; cat "$OUT/vc/typed.txt" 2>/dev/null; echo
echo "== received by the victim window:"; cat "$OUT/victim.txt" 2>/dev/null; echo
echo "== windows at the end:"; cat "$OUT/windows.txt"
TYPED=$(cat "$OUT/vc/typed.txt" 2>/dev/null)
VICTIM=$(cat "$OUT/victim.txt" 2>/dev/null)
# A run in which the plugin never started VeraCrypt proves nothing ("nothing typed" would pass).
if [ ! -f "$OUT/vc/argv.txt" ]; then echo "RESULT: NO RESULT (VeraCrypt was not started; run again)"; exit 0; fi
case "$SCENARIO" in
  normal)   [ "$TYPED" = "$SECRET" ] && echo "RESULT: PASS" || echo "RESULT: FAIL" ;;
  atoff)    [ -f "$OUT/vc/argv.txt" ] && [ -z "$TYPED" ] && echo "RESULT: PASS" || echo "RESULT: FAIL" ;;
  nowindow) [ -z "$VICTIM" ] && [ -z "$TYPED" ] && echo "RESULT: PASS" || echo "RESULT: FAIL" ;;
  steal|stealrand) [ -z "$VICTIM" ] && { [ "$TYPED" = "$SECRET" ] || [ -z "$TYPED" ]; } && echo "RESULT: PASS (typed into VeraCrypt: $([ -n "$TYPED" ] && echo yes || echo no))" || echo "RESULT: FAIL" ;;
esac
