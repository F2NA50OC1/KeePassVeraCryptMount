#!/bin/bash
REPO="$(cd "$(dirname "$0")/../.." && pwd)"
# End-to-end test of the WINDOWS code path: KeePass 2.62 for Windows running in Wine (wine-mono),
# the plugin, and a fake VeraCrypt.exe that shows a real Win32 password dialog (#32770 + ES_PASSWORD).
#   wine_e2e.sh PLUGIN_FILE OUT_DIR SCENARIO
#   (normal | nowindow | msgbox | tokenpin | pimfocus | steal | stealrand; see fakevc.c)
set -u
PLUGIN="$1"; OUT="$2"; SCENARIO="$3"
KPSRC=${KPSRC:-$REPO/lib/KeePass}
TESTS=$REPO/build/bin/KeePassVeraCryptMount.Tests/Release/KeePassVeraCryptMount.Tests.exe
export WINEPREFIX=${WINEPREFIX:-$HOME/.wine-kpvcm} WINEDEBUG=-all HOME=$HOME
DISP=:${WE2E_DISPLAY:-96}
SECRET='S3cret pass! with spaces & "quotes"'
C="$WINEPREFIX/drive_c"

rm -rf "$OUT" "$C/kp" "$C/vc"; mkdir -p "$OUT/home"
cp -r "$KPSRC" "$C/kp"; cp "$PLUGIN" "$C/kp/Plugins/"
mkdir -p "$C/vc"; cp $REPO/build/fakevc/VeraCrypt.exe "$C/vc/"; touch "$C/vc/test volume.hc"
cat > "$C/kp/KeePass.config.xml" <<'EOF'
<?xml version="1.0" encoding="utf-8"?>
<Configuration xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Application>
    <LastUsedFile><Path>C:\vc\test.kdbx</Path></LastUsedFile>
    <Start><CheckForUpdate>false</CheckForUpdate><CheckForUpdateConfigured>true</CheckForUpdateConfigured><OpenLastFile>true</OpenLastFile></Start>
  </Application>
  <Custom>
    <Item><Key>veracrypt.exe.location</Key><Value>C:\vc\VeraCrypt.exe</Value></Item>
    <Item><Key>veracrypt.autotype.wait.timeout</Key><Value>6000</Value></Item>
  </Custom>
</Configuration>
EOF
mono "$TESTS" makedb "$C/vc/test.kdbx" "master" "$SECRET" '{DB_DIR}\test volume.hc' >/dev/null

Xvfb $DISP -screen 0 1280x900x24 >/dev/null 2>&1 & XPID=$!
sleep 1.5; export DISPLAY=$DISP
openbox >/dev/null 2>&1 & WMPID=$!
sleep 1
export FAKEVC_MODE=$SCENARIO
[ "$SCENARIO" = stealrand ] && export FAKEVC_MODE=steal
wineserver -k 2>/dev/null; sleep 0.5

VICTIM_PID=""
if [ "$SCENARIO" = nowindow ] || [ "$SCENARIO" = steal ] || [ "$SCENARIO" = stealrand ]; then
  zenity --entry --title="VICTIM" --text="Nothing must be typed here" > "$OUT/victim.txt" 2>/dev/null &
  VICTIM_PID=$!; sleep 1.5
fi

# (Some wine-mono versions abort with two command line arguments, so the database is opened as
#  "last used file" and the master password is typed into KeePass' own dialog.)
# Minimal environment: wine-mono can fail on environment variables that contain line breaks.
env -i HOME="$HOME" DISPLAY="$DISPLAY" WINEPREFIX="$WINEPREFIX" WINEDEBUG="$WINEDEBUG" PATH=/usr/bin:/bin \
    LANG=C.UTF-8 FAKEVC_MODE="$FAKEVC_MODE" wine 'C:\kp\KeePass.exe' >"$OUT/keepass.log" 2>&1 &
for i in $(seq 1 120); do
  D=$(xdotool search --onlyvisible --name 'Open Database' 2>/dev/null | head -1); [ -n "$D" ] && break; sleep 0.5
done
sleep 2; xdotool windowactivate --sync "$D" 2>/dev/null; sleep 0.5; xdotool type --delay 60 master; sleep 0.3; xdotool key Return
for i in $(seq 1 120); do
  W=$(xdotool search --onlyvisible --name 'test.kdbx' 2>/dev/null | head -1); [ -n "$W" ] && break; sleep 0.5
done
sleep 4
xdotool windowactivate --sync "$W" 2>/dev/null; sleep 0.6
import -window root "$OUT/1_keepass.png" 2>/dev/null
# First entry row, relative to the KeePass window (Wine places it differently than Mono).
# (Wine sometimes replaces the window right after start: look it up again if needed.)
GEOM=""
for i in 1 2 3 4 5; do
  GEOM=$(xdotool getwindowgeometry --shell "$W" 2>/dev/null) && [ -n "$GEOM" ] && break
  sleep 1; W=$(xdotool search --onlyvisible --name 'test.kdbx' 2>/dev/null | head -1)
done
eval "$GEOM"
EX=$((X + ${ENTRY_DX:-210})); EY=$((Y + ${ENTRY_DY:-115}))
xdotool mousemove "$EX" "$EY" click 1; sleep 0.8
xdotool mousemove "$EX" "$EY" click 3; sleep 1.5
import -window root "$OUT/2_menu.png" 2>/dev/null
xdotool mousemove $((EX + 40)) $((EY + 12)) click 1
if [ "$SCENARIO" = nowindow ]; then
  sleep 0.3; xdotool search --name 'VICTIM' windowactivate 2>/dev/null
fi
if [ "$SCENARIO" = steal ] || [ "$SCENARIO" = stealrand ]; then
  # Activate the victim as soon as (steal) / at a random moment after (stealrand) the dialog appears.
  for i in $(seq 1 200); do xdotool search --onlyvisible --name 'Enter password for' >/dev/null 2>&1 && break; sleep 0.02; done
  if [ "$SCENARIO" = stealrand ]; then D=$(python3 -c "import random; print(round(random.uniform(0, 1.0), 3))"); echo $D > "$OUT/delay.txt"; sleep $D; fi
  xdotool search --name 'VICTIM' windowactivate 2>/dev/null
fi
sleep "${E2E_WAIT:-10}"
import -window root "$OUT/3_after.png" 2>/dev/null
xdotool search --onlyvisible --name '.' getwindowname %@ 2>/dev/null | sort -u > "$OUT/windows.txt"
if [ -n "$VICTIM_PID" ]; then
  xdotool search --name 'VICTIM' windowactivate --sync key Return 2>/dev/null; sleep 0.8; kill $VICTIM_PID 2>/dev/null
fi
cp "$C/vc/argv.txt" "$C/vc/typed.txt" "$C/vc/pim.txt" "$C/vc/pid.txt" "$OUT/" 2>/dev/null
wineserver -k 2>/dev/null; sleep 1
kill $WMPID $XPID 2>/dev/null

echo "== scenario: $SCENARIO (Windows code path in Wine)"
echo "== arguments received by the fake VeraCrypt.exe:"; cat "$OUT/argv.txt" 2>/dev/null || echo "(not started)"
echo "== typed into its password dialog:"; cat "$OUT/typed.txt" 2>/dev/null; echo
echo "== typed into its PIM field:"; cat "$OUT/pim.txt" 2>/dev/null; echo
echo "== received by the victim window:"; cat "$OUT/victim.txt" 2>/dev/null; echo
TYPED=$(cat "$OUT/typed.txt" 2>/dev/null); PIM=$(cat "$OUT/pim.txt" 2>/dev/null); VICTIM=$(cat "$OUT/victim.txt" 2>/dev/null)
# A run in which the plugin never started VeraCrypt proves nothing ("nothing typed" would pass).
if [ ! -f "$OUT/argv.txt" ]; then echo "RESULT: NO RESULT (VeraCrypt was not started; run again)"; exit 0; fi
case "$SCENARIO" in
  normal)          [ "$TYPED" = "$SECRET" ] && [ -z "$PIM" ] && echo "RESULT: PASS" || echo "RESULT: FAIL" ;;
  nowindow|msgbox|tokenpin|pimfocus) [ -z "$TYPED" ] && [ -z "$PIM" ] && [ -z "$VICTIM" ] && echo "RESULT: PASS" || echo "RESULT: FAIL" ;;
  steal|stealrand) [ -z "$VICTIM" ] && echo "RESULT: PASS (typed into VeraCrypt: $([ -n "$TYPED" ] && echo yes || echo no))" || echo "RESULT: FAIL" ;;
esac
