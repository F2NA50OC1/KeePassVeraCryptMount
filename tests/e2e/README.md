# End-to-end tests (Linux test machine)

Scripts used to test the plugin inside the real KeePass 2.62 with a fake VeraCrypt that records
the arguments it receives and what is typed into its password window. Run them with `bash`
(for example `bash tests/e2e/run_all_e2e.sh`).

* `e2e.sh PLUGIN OUT normal|nowindow|steal|stealrand|atoff` - KeePass on Mono; fake VeraCrypt = zenity.
* `wine_e2e.sh PLUGIN OUT normal|nowindow|msgbox|tokenpin|pimfocus|steal|stealrand` - KeePass for Windows in Wine
  (wine-mono), fake `VeraCrypt.exe` built from `fakevc.c`/`fakevc.rc` with MinGW:
  `x86_64-w64-mingw32-windres fakevc.rc -O coff -o r.o && x86_64-w64-mingw32-gcc -municode -mwindows -o VeraCrypt.exe fakevc.c r.o -lshell32`
  (put it in `build/fakevc/`). The Wine prefix needs wine-mono; the scripts run Wine with a clean
  environment.
* `kp_try_plugin.sh PLUGIN OUT` - only starts KeePass with the plugin and records the windows
  (does the plugin load / compile?). `KPSRC=/path/to/other/KeePass` tests another KeePass version.
* `run_all_e2e.sh` - all scenarios, summary table. Environment: `WINEPREFIX` (a prefix with
  wine-mono), `RUNS` (output folder, default /tmp/kpvcm-runs).

What each scenario proves:

| Scenario | Fake VeraCrypt | Must happen |
|---|---|---|
| normal | password window | the exact password (spaces, quotes, `&`) arrives in it |
| nowindow | no window, another program in front | nothing typed anywhere |
| msgbox | only an error message box | nothing typed |
| tokenpin | only a token PIN dialog (Windows) | nothing typed (no wrong PIN on the token) |
| pimfocus | password dialog with the cursor in its PIM field (Windows) | nothing typed into the PIM field or anywhere else |
| steal | other program activated right after the window appears | nothing reaches the other program |
| stealrand | same, at a random moment (run it many times) | nothing reaches the other program |
| atoff | Auto-Type disabled in the entry | message first, VeraCrypt starts, nothing typed |

In the steal scenarios "typed into VeraCrypt: yes/no" says whether the fake password window
received the complete sequence including the final Enter. "no" is also correct: the plugin did
not type, or KeePass stopped typing when the other window came to the front (the characters typed
until then are in VeraCrypt's field). The test fails only if the other window received anything.

Needs: Xvfb, openbox, xdotool, zenity, ImageMagick (`import`), mono, wine. The entry row is found
by screen coordinates (`ENTRY_X`/`ENTRY_Y`, `ENTRY_DY` for Wine), adjust if the layout differs.
