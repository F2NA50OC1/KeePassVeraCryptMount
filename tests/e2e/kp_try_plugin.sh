#!/bin/bash
REPO="$(cd "$(dirname "$0")/../.." && pwd)"
# Start KeePass 2.62 (Mono, virtual X display) with one plugin file in Plugins/,
# then record what happened: window titles, a screenshot, and the plugin cache.
#   kp_try_plugin.sh PLUGIN_FILE OUT_DIR [extra KeePass args...]
set -u
PLUGIN="$1"; OUT="$2"; shift 2
KPSRC=${KPSRC:-$REPO/lib/KeePass}
DISP=:${KP_DISPLAY:-77}

rm -rf "$OUT"; mkdir -p "$OUT/home"
cp -r "$KPSRC" "$OUT/kp"
cp "$PLUGIN" "$OUT/kp/Plugins/"
# Pre-answer the first-start questions (update check) so no extra dialog appears.
cat > "$OUT/kp/KeePass.config.xml" <<'EOF'
<?xml version="1.0" encoding="utf-8"?>
<Configuration xmlns:xsd="http://www.w3.org/2001/XMLSchema" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <Application><Start><CheckForUpdate>false</CheckForUpdate><CheckForUpdateConfigured>true</CheckForUpdateConfigured></Start></Application>
</Configuration>
EOF

export HOME="$OUT/home"
Xvfb $DISP -screen 0 1280x900x24 >/dev/null 2>&1 &
XPID=$!
sleep 2
export DISPLAY=$DISP
( cd "$OUT/kp" && exec mono KeePass.exe "$@" ) >"$OUT/keepass.log" 2>&1 &
KPID=$!
sleep "${KP_WAIT:-35}"

xdotool search --onlyvisible --name '.' getwindowname %@ 2>/dev/null | sort -u > "$OUT/windows.txt"
import -window root "$OUT/screen.png" 2>/dev/null
find "$HOME" -path '*PluginCache*' -type f > "$OUT/plugincache.txt" 2>/dev/null

kill $KPID 2>/dev/null; sleep 1; kill -9 $KPID 2>/dev/null
kill $XPID 2>/dev/null
echo "== windows"; cat "$OUT/windows.txt"
echo "== plugin cache"; cat "$OUT/plugincache.txt"
