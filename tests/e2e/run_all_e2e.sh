#!/bin/bash
REPO="$(cd "$(dirname "$0")/../.." && pwd)"
# Runs every end-to-end scenario against the final build and prints a summary table.
DIST=$REPO/dist
R=${RUNS:-/tmp/kpvcm-runs}/final
mkdir -p "$R"
summary=()
run() { # label, command...
  local label="$1"; shift
  local out; out=$("$@" 2>&1 | grep "RESULT:" | tail -1)
  summary+=("$(printf '%-58s %s' "$label" "${out:-RESULT: (no result)}")")
}
run "Linux/Mono  DLL   normal (password reaches VeraCrypt)"    env E2E_DISPLAY=81 bash $REPO/tests/e2e/e2e.sh "$DIST/KeePassVeraCryptMount.dll"  "$R/lx_dll_normal" normal
run "Linux/Mono  DLL   nowindow (other window has focus)"      env E2E_DISPLAY=82 bash $REPO/tests/e2e/e2e.sh "$DIST/KeePassVeraCryptMount.dll"  "$R/lx_dll_nowindow" nowindow
run "Linux/Mono  DLL   steal (focus taken at the last moment)" env E2E_DISPLAY=83 bash $REPO/tests/e2e/e2e.sh "$DIST/KeePassVeraCryptMount.dll"  "$R/lx_dll_steal" steal
run "Linux/Mono  DLL   stealrand (focus taken at a random moment) x1" env E2E_DISPLAY=88 bash $REPO/tests/e2e/e2e.sh "$DIST/KeePassVeraCryptMount.dll" "$R/lx_dll_stealrand" stealrand
run "Linux/Mono  DLL   atoff (Auto-Type disabled in entry)"    env E2E_DISPLAY=84 bash $REPO/tests/e2e/e2e.sh "$DIST/KeePassVeraCryptMount.dll"  "$R/lx_dll_atoff" atoff
run "Linux/Mono  PLGX  normal (compiled by KeePass 2.62)"      env E2E_DISPLAY=85 E2E_WAIT=12 bash $REPO/tests/e2e/e2e.sh "$DIST/KeePassVeraCryptMount.plgx" "$R/lx_plgx_normal" normal
run "Linux/Mono  PLGX  nowindow"                               env E2E_DISPLAY=86 E2E_WAIT=12 bash $REPO/tests/e2e/e2e.sh "$DIST/KeePassVeraCryptMount.plgx" "$R/lx_plgx_nowindow" nowindow
run "Windows/Wine DLL  normal"                                 env ENTRY_DY=62 WE2E_DISPLAY=96 bash $REPO/tests/e2e/wine_e2e.sh "$DIST/KeePassVeraCryptMount.dll"  "$R/w_dll_normal" normal
run "Windows/Wine DLL  nowindow"                               env ENTRY_DY=62 WE2E_DISPLAY=96 bash $REPO/tests/e2e/wine_e2e.sh "$DIST/KeePassVeraCryptMount.dll"  "$R/w_dll_nowindow" nowindow
run "Windows/Wine DLL  msgbox (VeraCrypt error box)"           env ENTRY_DY=62 WE2E_DISPLAY=96 bash $REPO/tests/e2e/wine_e2e.sh "$DIST/KeePassVeraCryptMount.dll"  "$R/w_dll_msgbox" msgbox
run "Windows/Wine DLL  tokenpin (token PIN dialog, not typed)" env ENTRY_DY=62 WE2E_DISPLAY=96 bash $REPO/tests/e2e/wine_e2e.sh "$DIST/KeePassVeraCryptMount.dll"  "$R/w_dll_tokenpin" tokenpin
run "Windows/Wine DLL  pimfocus (cursor in PIM field, not typed)" env ENTRY_DY=62 WE2E_DISPLAY=96 bash $REPO/tests/e2e/wine_e2e.sh "$DIST/KeePassVeraCryptMount.dll"  "$R/w_dll_pimfocus" pimfocus
run "Windows/Wine DLL  steal (other window activated)"         env ENTRY_DY=62 WE2E_DISPLAY=96 bash $REPO/tests/e2e/wine_e2e.sh "$DIST/KeePassVeraCryptMount.dll"  "$R/w_dll_steal" steal
run "Windows/Wine DLL  stealrand (random moment) x1"   env ENTRY_DY=62 WE2E_DISPLAY=96 bash $REPO/tests/e2e/wine_e2e.sh "$DIST/KeePassVeraCryptMount.dll"  "$R/w_dll_stealrand" stealrand
run "Windows/Wine PLGX normal (compiled by KeePass in Wine)"   env ENTRY_DY=62 WE2E_DISPLAY=96 E2E_WAIT=14 bash $REPO/tests/e2e/wine_e2e.sh "$DIST/KeePassVeraCryptMount.plgx" "$R/w_plgx_normal" normal
printf '%s\n' "${summary[@]}"
