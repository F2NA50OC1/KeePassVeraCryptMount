# Security and reliability notes

What version 1.0.7 got wrong and how version 2.0.0 handles it, the further safeguards of 2.0.0,
the risks that remain, and how the plugin is tested. Checked against the source of KeePass 2.62
and of VeraCrypt (release tags 1.26.29, 1.26.30 and 1.26.31: `src/Mount/Mount.c`,
`src/Mount/Mount.rc`, `src/Common/Common.rc`, `src/Common/Crypto.c`, `src/Common/Dlgcode.c`,
`src/Common/Pkcs5.c`, `src/Main/CommandLineInterface.cpp`).

## Why 1.0.7 no longer loads

KeePass 2.62 added "path validation for files extracted from a PLGX file". The 1.0.7 PLGX was
built with a third-party packer that stored two files as `..\..\License.txt` and
`..\..\LicenseInfo.txt`. Older KeePass versions therefore wrote these two files two folders above
their temporary build folder (into `%LOCALAPPDATA%`); KeePass 2.62 refuses the whole plugin with
"Attempted to perform an unauthorized operation". The plugin code itself compiles unchanged.
2.0.0 is packed by KeePass itself from a clean folder, and the build script fails if any stored
path points outside the plugin folder.

## Findings in 1.0.7

### Security

| # | Severity | Finding in 1.0.7 | Fix in 2.0.0 |
|---|----------|------------------|--------------|
| S1 | High | **Password typed into whatever window is in front.** After starting VeraCrypt the plugin called `WaitForInputIdle` (returns immediately on Mono and does not mean that the password window is shown or focused on Windows) and then auto-typed `{PASSWORD}{ENTER}` into the current foreground window. If VeraCrypt was slow, showed an error first, or the user switched windows, the password and an Enter went into another program (chat, browser, terminal) or into KeePass itself. Reproduced: in KeePass 2.62 the password was typed into KeePass' own window and the Enter opened the entry editor. | The plugin waits (without blocking KeePass) for the password window of *the VeraCrypt process it started*. Windows: a dialog box of that process with an input field and VeraCrypt's KDF list, which is the foreground window with the keyboard focus in an input field. Linux: the active X11 window, which must also have the keyboard focus, must belong to that process (xdotool). The state must hold on two checks in a row (three after the plugin brought the window to the front), is checked again right before typing (KeePass `FilterSend` event), and while typing KeePass checks before *every key* that the active window is unchanged and still has the title of VeraCrypt's window (otherwise it stops). Nothing is typed on timeout, if VeraCrypt exits, or if the database is closed or locked meanwhile. Test results: see "How this was tested". |
| S2 | Medium | **Command line built by string concatenation.** Values were quoted only when they contained a space; quotes and backslashes were never escaped. A crafted entry (e.g. in a shared or imported database) could add VeraCrypt options (key files, mount options, ...). No shell is involved, so no arbitrary commands. Normal paths ending in `\` also broke. | Arguments are built as a list and quoted per platform (Windows: the `CommandLineToArgvW` rules VeraCrypt uses; Linux: Mono's argument splitting). Every value is validated (drive letter A-Z, PIM digits in VeraCrypt's range, KDF from a fixed list, label length and characters, no control characters, no quotes in Windows paths). Verified with round-trip tests, including 3000 random strings. |
| S3 | Low | PRF value cut with `Substring(5)` and passed unchecked; values shorter than 5 characters crashed. | Fixed list of accepted values, matched against what VeraCrypt accepts on Windows and Linux. |
| S4 | Low | Saving the dialog silently overwrote the entry's Auto-Type sequence and switched on two-channel Auto-Type obfuscation (uses the clipboard). | Entry Auto-Type settings are never changed. The plugin types the entry's own sequence if it has one, else `{PASSWORD}{ENTER}`. |
| S5 | Low | VeraCrypt search: Linux ran `/bin/sh -c 'which veracrypt'`; Windows read the user-overridable file association first. | PATH searched in code; Windows uses the machine-wide registration and the standard install folder first. |
| S6 | Info | Only path placeholders are expanded in paths; field references such as `{PASSWORD}` are not, so no secret reaches the command line. The password itself was never on the command line. | Unchanged (environment variables added). |
| S7 | Info, unchanged | The **PIM** is passed on VeraCrypt's command line (`/pim`), which other programs of the same user can read. VeraCrypt has no other non-interactive way. | Documented in the dialog's tool tip and the README: leave "Use PIM" unchecked and type the PIM into VeraCrypt if this matters. |
| S8 | Info | At each update check, KeePass contacted the original project's server (`UpdateUrl`). | Removed: the plugin contacts no server. |

### Reliability

| # | Finding in 1.0.7 | Fix in 2.0.0 |
|---|------------------|--------------|
| R1 | "Mount" mounted the previously *saved* settings, not what the dialog showed; "Apply" then "Cancel" lost the applied changes. | "Mount" saves and mounts exactly what the dialog shows; "Apply" saves immediately (with a KeePass history entry). |
| R2 | `{DB_DIR}`, `{DB_PATH}` etc. never worked (no database passed to KeePass' placeholder engine); relative paths depended on KeePass' current folder, which changes when file dialogs are used. | Database passed; relative paths are relative to the KeePass program folder (`{APPDIR}`); missing volume or key files are reported before VeraCrypt starts. |
| R3 | If the entry's drive letter was in use (e.g. volume already mounted), saving the dialog silently changed it to "Auto". | The entry's letter is always listed. |
| R4 | PIM passed as typed: VeraCrypt reads command line numbers with a leading 0 as octal (`0123` = 83). Upper limit 9,999,999 instead of VeraCrypt's 2,147,468. | PIM normalised to a plain decimal number in VeraCrypt's range. |
| R5 | Linux: several key files were passed as repeated `-k` options; VeraCrypt for Linux takes one comma list, so only one key file was used. | `--keyfiles=a,b` with commas in names escaped as `,,`. |
| R6 | VeraCrypt 1.26.7 removed TrueCrypt mode and RIPEMD-160: VeraCrypt aborted with "command line error". | Clear message before VeraCrypt starts (version read from VeraCrypt.exe). |
| R7 | Unhandled exceptions shown as KeePass error dialogs: VeraCrypt failing to start (e.g. UAC cancelled), invalid paths, short PRF values, out-of-range timeout in the options dialog. | All handled with plain messages; VeraCrypt set to "run as administrator" is started via UAC. |
| R8 | KeePass froze while waiting (`WaitForInputIdle` + `Thread.Sleep(1000)` on the UI thread); a large timeout value in the config could become negative = wait forever. | Non-blocking timer; maximum wait clamped to 10-60 s. |
| R9 | Icon handle leaked each time a dialog opened; dialogs and process objects not disposed; one event handler not removed on exit. | Fixed. |
| R10 | `ID:` volumes case-sensitive and unvalidated; paths stored with quotes; device paths. | `ID:` + 64 hex digits validated; quotes removed; `\Device\...` and `\\?\Volume{...}` passed through. |
| R11 | Build depended on a NuGet build tool, a `git://` submodule of KeePass and produced a different version number on every build. | SDK project, fixed version, deterministic build, no submodule, no .resx; no images of its own. |

## Further safeguards in 2.0.0

| # | Severity | Risk | Safeguard |
|---|----------|------|-----------|
| T1 | Medium | **Security token PIN window taken for the password window (Windows).** With token key files and VeraCrypt's password cache, VeraCrypt can show its "Security token password/PIN required" dialog before the password window. Typing the volume password there would also count as a wrong PIN against the token's retry limit (tokens lock after a few). | The password window must contain VeraCrypt's password field *and* its KDF list (identified by their control IDs, see T12), which the PIN dialog (`IDD_TOKEN_PASSWORD`) and message boxes do not have. End-to-end scenario `tokenpin`. |
| T2 | Medium | **KeePass settings staying changed.** While typing, the plugin switches on two KeePass options (cancel on window change, and a window title filter). If KeePass saved its configuration in that second (KeePass closed, Windows shut down), they would stay on and restrict the user's own Auto-Type. | `TypingSafeguards`: restored right after typing, in the plugin's `Terminate` (KeePass unloads plugins before it saves its configuration), and through a marker in the configuration that is checked at the next start. Only filters created by the plugin are removed. |
| T3 | Low | **VeraCrypt run as administrator:** Windows (UIPI) silently drops keystrokes from a normal program into an elevated one; typing would silently go nowhere. | When Windows had to elevate VeraCrypt, the plugin does not type and says so in the status bar. |
| T4 | Info | **Database of the entry assumed to be the active one** (as in 1.0.7). KeePass' own entry functions look up the database that contains the entry instead. | Same lookup as KeePass (`SafeFindContainerOf`): changes are saved in, and `{DB_DIR}` refers to, the entry's own database. |
| T5 | Low | **Volume label erased on Linux:** 1.0.7 erased the label of an entry when its settings were saved on Linux; a database used on Windows and Linux lost it. | The hidden label is kept unchanged on Linux and only checked on Windows. |
| T6 | Low | **Minimum KeePass version:** the window title filter (`AutoTypeAbortOnWindows`) exists since KeePass 2.36; older versions cannot compile the plugin. | PLGX prerequisite 2.36 (older KeePass versions report "A newer KeePass version is required to open this file."); compiled against 2.35 (fails, as expected), 2.36-2.39, 2.40, 2.47, 2.53.1, 2.57.1, 2.61.1, 2.62. |
| T7 | Low | **Too short wait:** 1.0.x saved 3000 ms in almost every configuration (the options dialog stored its default on OK). Since typing now waits for the real window, a slow first start of VeraCrypt (after boot, virus scanner) would end with "nothing was typed". | Minimum and default 10 s; typing still starts as soon as the window is ready. |
| T8 | Low | **Argon2 on older VeraCrypt:** VeraCrypt for Windows accepts `/hash argon2id` only since 1.26.30 (1.26.29 introduced Argon2 but only knew an internal name); an older VeraCrypt would stop with "command line error". | The plugin reads VeraCrypt's version and explains it before starting VeraCrypt. Autodetection (the default) works with every version. |
| T9 | Info | **Images and licenses:** two button icons of 1.0.x were "free for non-commercial use" / CC BY-NC-ND, which conflicts with distributing the plugin under the GPL; the VeraCrypt logos and the picture of the menu came without license information. | The plugin ships no images and has no icon of its own; its small buttons use KeePass' own icons at run time (text if KeePass ever renames them). |
| T10 | Low | **Secure Desktop.** KeePass cannot type into VeraCrypt's Secure Desktop. VeraCrypt started with `/q` does not load its preferences (`Mount.c`, `OptionQuit`: `UsePreferences = FALSE`), so its password window is on the normal desktop even when "Use Secure Desktop for password entry" is on; relying on this side effect would break Auto-Type if VeraCrypt changed it. | For entries with Auto-Type checked, the plugin actively passes `/secureDesktop n` (VeraCrypt 1.20 and newer; the command line value overrides the preference, `Dlgcode.c`, `SecureDesktopDialogBoxParam`), so Auto-Type works as intended. Entries with Auto-Type unchecked get no such option. |
| T11 | Info | **Links to web pages:** the options dialog had a "Donate" link and a "Help" link to the original author's pages. | Removed: the plugin contains no URLs and opens no web pages. |
| T12 | Low | **Other fields and windows of VeraCrypt (Windows).** The check accepted the keyboard focus in any input field of a VeraCrypt dialog that had an input field and a list box. With the cursor in the PIM field, the password went into the PIM field (shown by a control test with the earlier build); VeraCrypt's main window (volume path field, whose history VeraCrypt can save) or its Mount Options dialog would also have qualified. | VeraCrypt's password field (`IDC_PASSWORD`, 1035) and KDF list (`IDC_PKCS5_PRF_ID`, 5127) are identified by their control IDs, the same in every VeraCrypt from 1.19 to 1.26.31, and the cursor must be in the password field itself; otherwise the plugin waits and types nothing. End-to-end scenario `pimfocus`. |

Reviewed and found correct (no change needed): command line quoting against VeraCrypt's parser
(`CommandLineToArgvW`), the label limit (1-32 characters, `Mount.c`), the PIM limit
(`MAX_PIM_VALUE` 2147468), the meaning of `/m ts` (do not preserve the container's timestamp),
KDF names and order (`get_pkcs5_prf_name`), KeePass' per-key checks in `SiEngineStd.ValidateState`
(identical in KeePass 2.36 and 2.62), KeePass' exit order (`CleanUpEx` unloads plugins before
saving the configuration), handling of locked or closed databases while waiting.

## Remaining risks (not fixable in a plugin)

* Auto-Type sends keystrokes. Malware running under your Windows account can record them, or
  attack KeePass directly; no plugin can prevent that. For the highest security, uncheck
  "Auto-Type" in the entry's mount settings and type the password into VeraCrypt yourself.
* VeraCrypt's Secure Desktop is not used for mounts started by the plugin (see T10): for
  entries with Auto-Type checked the plugin actively asks for the normal desktop; for entries
  with Auto-Type unchecked it passes nothing, and VeraCrypt (started with `/q`) does not read the
  preference. Mount with VeraCrypt itself to type a password on the Secure Desktop.
* On Linux, X11 cannot tell whether the focus is in a password field, nor tell VeraCrypt's
  password window from its other dialogs; the check is "the active, focused window belongs to the
  started VeraCrypt process".
* The PIM is on VeraCrypt's command line (S7).
* KeePass compiles `.plgx` plugins into a cache folder that your user account can write to
  (see KeePass' plugin security notes). The `.dll` in KeePass' own `Plugins` folder (in
  `C:\Program Files`, which needs admin rights to change) avoids that.

## How the plugin is tested

* **Logic tests** (`tests/KeePassVeraCryptMount.Tests`, 22 groups): quoting round trips (also 3000
  random strings against an independent parser), argument lists for Windows and Linux, injection
  attempts, reading 1.0.7 entries, PIM, drive letters, paths, KDF names and order against
  VeraCrypt, the Argon2 version check, label rules, the Secure Desktop argument, wait limits,
  the title filter.
* **End-to-end** with the real KeePass 2.62 and a fake VeraCrypt that records its arguments and
  what is typed: Linux (Mono; DLL and PLGX) and the Windows code path (KeePass 2.62 for Windows in
  Wine; fake `VeraCrypt.exe` with real Win32 dialogs built like VeraCrypt's). Scenarios: normal
  mount; no window while another program has the focus; an error box; a token PIN dialog; the
  cursor in VeraCrypt's PIM field;
  another window takes the focus right away and at random moments; Auto-Type disabled in the
  entry. Results: see the table below.
* **Compile matrix:** KeePass 2.36 to 2.62 (see T6); Mono's C# compiler with `-langversion:5`.
* **Real VeraCrypt on real Windows:** not automated (VeraCrypt's driver cannot run in the test
  environment); a manual test with a test database and a test volume, described in the release
  checklist of [MAINTAINING.md](MAINTAINING.md) (it covers Argon2 and the Secure Desktop). Try
  the plugin the same way before using it with a real vault.

### End-to-end results of 2.0.0

All scenarios pass with the release build:

| Scenario | Linux (Mono) | Windows code path (Wine) |
|---|---|---|
| normal: exact password arrives (spaces, quotes, `&`) | DLL and PLGX: pass | DLL and PLGX: pass; the fake VeraCrypt.exe (version 1.26.31 like the real one) receives `/secureDesktop n` |
| nowindow: other program in front, no VeraCrypt window | DLL and PLGX: pass, nothing typed | pass, nothing typed |
| msgbox: VeraCrypt shows only an error box | - | pass, nothing typed |
| tokenpin: VeraCrypt shows only a token PIN dialog | - | pass, nothing typed (T1) |
| pimfocus: cursor in VeraCrypt's PIM field | - | pass, nothing typed (T12) |
| steal: other program activated right after the window appears | pass, nothing reached it | pass, nothing reached it (VeraCrypt brought back, password typed there) |
| stealrand: other program activated at a random moment (0-1.2 s after the window appears) | 10 of 10 runs: nothing reached it (in one run VeraCrypt was brought back and the password typed there) | 10 of 10 runs: nothing reached it (in two runs VeraCrypt was brought back and the password typed there) |
| atoff: Auto-Type disabled in the entry | pass (message first, nothing typed) | - |

Also: the PLGX compiles and loads in KeePass 2.36, 2.47, 2.57.1 and 2.62 (Mono) and in KeePass
2.62 for Windows (Wine); the menu items and both dialogs show no plugin icon and no links; Esc
closes the mount dialog; after a mount and a normal exit, KeePass' configuration contains only the plugin's
two options (`veracrypt.exe.location`, `veracrypt.autotype.wait.timeout`).
