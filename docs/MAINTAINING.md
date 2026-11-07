# Maintaining KeePassVeraCryptMount

For whoever looks after this plugin next. It explains how the plugin is put together, which
rules keep old entries working and the password safe, and what to check when KeePass or
VeraCrypt changes. Each source file starts with a header that covers its own details (what it
does, why, its history). This document is the overview.

State described here: version 2.0.0, checked against KeePass 2.62 and VeraCrypt 1.26.31.

---

## 1. What happens when the user clicks "Mount volume"

```
KeePassVeraCryptMountExt       menu item / Ctrl+M -> (optional) VeraCryptMountForm -> save
        |
MountController.Mount          1. VeraCrypt program present?           PluginOptions, VeraCryptInfo
        |                      2. read the entry's settings             MountSettings (EntryStrings)
        |                      3. paths -> absolute, files exist        PathResolver
        |                      4. argument list, version checks         VeraCryptArguments, KdfAlgorithms
        |                      5. start VeraCrypt (no shell)            CommandLine (quoting)
        v
PendingAutoType                timer every 150 ms, KeePass stays responsive
        |                      PasswordWindowProbe: is VeraCrypt's password window in front and ready?
        |                      ready on 2-3 checks in a row -> type ONCE, else give up (nothing typed)
        v
KeePass AutoType.PerformIntoCurrentWindow(entry, database, sequence)
                               FilterSend event: last check (PendingAutoType.OnFilterSend)
                               TypingSafeguards: KeePass checks the window before every key
```

The password is never put on VeraCrypt's command line. It is typed into VeraCrypt's own password
window by KeePass' Auto-Type, or by the user.

## 2. Source files

| File | Responsibility |
|---|---|
| `KeePassVeraCryptMountExt.cs` | Plugin class KeePass loads: menus, Ctrl+M, dialogs, saving entries. |
| `MountController.cs` | One mount: checks, starting VeraCrypt, handing over to Auto-Type. |
| `PendingAutoType.cs` | Waiting for the password window and typing once. The safety layers are listed in its header. |
| `PasswordWindowProbe.cs` | Recognises VeraCrypt's password window (Windows: Win32 API, Linux: xdotool). |
| `TypingSafeguards.cs` | KeePass options switched on only while typing, restored afterwards (also after a crash). |
| `NativeMethods.cs` | Windows API declarations. |
| `VeraCryptArguments.cs` | The validated argument list (Windows `/...`, Linux `--...`). |
| `KdfAlgorithms.cs` | The PRF / KDF list: stored value, display name, VeraCrypt argument. |
| `CommandLine.cs` | Quoting an argument list for Windows or Mono. |
| `PathResolver.cs` | Placeholders, relative paths, existence checks. |
| `MountSettings.cs` / `EntryStrings.cs` | The settings stored in the entry, and their field names. |
| `PluginOptions.cs` | Global settings in KeePass.config.xml. |
| `VeraCryptInfo.cs` | Finding VeraCrypt and reading its version. |
| `VeraCryptMountForm*.cs`, `VeraCryptOptionsForm*.cs` | The two dialogs. |
| `MountMenuItemState.cs`, `LogicalDriveLetters.cs`, `Platform.cs`, `Resources.cs`, `LanguageTexts.cs` | Helpers, KeePass' button icons, texts. |
| `tests/KeePassVeraCryptMount.Tests` | Logic tests (no KeePass window needed). |
| `tests/e2e` | End-to-end tests with the real KeePass and a fake VeraCrypt (Linux and Wine). |
| `tools/plgx.py`, `tools/apply-headers.py` | PLGX inspection; license header / encoding of the sources. |

## 3. Rules that must not be broken

**Compatibility (entries and settings of existing users)**

* Entry field names in `EntryStrings.cs` and their value formats (see `MountSettings.cs`) are the
  same as in 1.0.x. Never rename a field or change a format; a new setting gets a new field.
* KDF values stored in entries (`KdfAlgorithms.StoredValue`) never change; new ones are added.
* Global option keys in `PluginOptions.cs` stay the same.
* Assembly name, namespace and class name `KeePassVeraCryptMount.KeePassVeraCryptMountExt`
  (KeePass finds the plugin by these names).
* The entry's own Auto-Type settings are read, never changed by the plugin.

**Security**

* Never pass the password on VeraCrypt's command line (`/p`, `--password`): other programs can
  read command lines.
* Expand only path placeholders and environment variables in paths
  (`MountController.ExpandPlaceholders`), never field references such as `{PASSWORD}`.
* Every value that goes to VeraCrypt is validated in `VeraCryptArguments` / `PathResolver`, and
  quoted by `CommandLine`. No shell is used.
* Type only after `PasswordWindowProbe` reports "Ready" for the VeraCrypt process the plugin
  started, on several checks in a row; check again in `FilterSend`; type at most once per mount;
  type nothing on any doubt (timeout, error, database closed or locked).
* Every temporary change to the user's KeePass settings is undone (`TypingSafeguards`).

**Simplicity and neutrality**

* The plugin offers the mount options of 1.0.x plus the current KDF list. Rarely used VeraCrypt
  options were left out on purpose: each one is another way to misconfigure a mount.
* No URLs in the plugin (the only URL in the sources is the GPL's address in the license
  notice), no web pages, no update check, no icon or images of its own, nothing that
  identifies a person or a machine. The assembly GUID is derived from the project name
  (`uuid5(NAMESPACE_DNS, "KeePassVeraCryptMount")`), the solution's project GUIDs likewise.
  The DLL is built deterministically (no time stamps, no paths). In the `.plgx`, `tools/build.sh`
  removes the creation time KeePass writes; the only per-build value left is the file ID KeePass
  generates. It must stay unique per build, because KeePass names its compiled-plugin cache after
  it (a fixed ID would make KeePass keep running an old cached version after an update).

## 4. Build constraints

* **C# 5 and .NET Framework 4.0.** KeePass on Windows compiles a `.plgx` with the C# 5 compiler
  of the .NET Framework. `LangVersion` 5 in the project makes `dotnet build` refuse newer syntax.
* **Every file listed explicitly** in `KeePassVeraCryptMount.csproj` (Compile items, no
  wildcards). KeePass' PLGX loader reads only the explicit Compile, EmbeddedResource and
  Reference items and `AssemblyName`.
* **Nothing outside the plugin folder.** KeePass 2.62 refuses a PLGX that contains paths like
  `..\..\License.txt`; that broke 1.0.7. `tools/build.sh` packs the PLGX with KeePass itself
  (`--plgx-create`) from a clean copy and fails if a stored path leaves the folder.
* **No .resx files, no icon or images of its own.** Menu items and banners show no icon, and
  `SmallIcon` is not overridden (KeePass' Plugins dialog then shows its standard plugin icon).
  The few button icons are KeePass' own, taken from KeePass at run time (`Resources.cs`); the
  project has no embedded resources.
* **UTF-8 with BOM, CRLF** for all `.cs` files, and the license header at the top:
  `python3 tools/apply-headers.py` fixes both; `tools/build.sh` checks it. `.gitattributes` keeps CRLF
  in every checkout. New file: start it with the line `// @@LICENSE@@` and run the script.
* **Minimum KeePass 2.36** (first version with `Integration.AutoTypeAbortOnWindows`, used by
  `TypingSafeguards`). `tools/build.sh` writes it into the PLGX (`PLGX_MIN_KEEPASS`).
* **Deterministic build**: the same sources give the same DLL (fixed version, no timestamps).

## 5. Build and test

```
bash tools/build.sh                          # dist/KeePassVeraCryptMount.dll, .plgx, SHA256SUMS.txt
dotnet build tests/KeePassVeraCryptMount.Tests/KeePassVeraCryptMount.Tests.csproj -c Release
mono build/bin/KeePassVeraCryptMount.Tests/Release/KeePassVeraCryptMount.Tests.exe
bash tests/e2e/run_all_e2e.sh                # needs Xvfb, openbox, xdotool, zenity, wine (see tests/e2e/README.md)
```

`tools/build.sh` needs the .NET 8 SDK, Mono, `xvfb-run`, Python 3 and the KeePass portable ZIP
unpacked in `lib/KeePass` (not part of the repository).

Compile against another KeePass version (API check), e.g. 2.36:

```
dotnet build src/KeePassVeraCryptMount/KeePassVeraCryptMount.csproj -c Release -p:KeePassExe=/path/to/2.36/KeePass.exe
```

What the tests cover:

* Logic tests: quoting round trips (also 3000 random strings), argument lists, injection
  attempts, reading 1.0.7 entries, PIM, drive letters, paths, KDF list against VeraCrypt's names,
  Argon2 version check, label rules, the Secure Desktop argument, timeout limits, the title filter.
* End-to-end (real KeePass 2.62; Linux/Mono and the Windows build in Wine): the password arrives
  exactly; nothing is typed when VeraCrypt shows no window, an error box or a token PIN dialog,
  or when the cursor is in its PIM field;
  nothing reaches another program when it is activated at any moment; Auto-Type disabled in the
  entry. See the table in `tests/e2e/README.md`.
* Not automated: the real VeraCrypt on real Windows; the manual test is step 4 of the release
  checklist.

## 6. Release checklist

1. Version: `Properties/AssemblyInfo.cs` (AssemblyVersion and AssemblyFileVersion),
   `docs/CHANGELOG.md`, `README.md`, `docs/SECURITY.md` (supported versions).
2. A line under HISTORY in the header of every source file you changed, with the month.
3. `bash tools/build.sh`, the logic tests, `bash tests/e2e/run_all_e2e.sh`, and a compile against
   the minimum KeePass version (2.36).
4. A manual test on Windows with the real VeraCrypt, a test database and a test volume:
   * a normal mount with the KDF, PIM and key file settings in use, and one with Argon2;
   * click another window right after "Mount volume": nothing may be typed there;
   * "Mount volume" for a volume that is already mounted: VeraCrypt's message, nothing typed;
   * VeraCrypt's "Use Secure Desktop for password entry" switched on: the password window is
     on the normal desktop and the password is typed;
   * an entry with Auto-Type unchecked: VeraCrypt asks, nothing is typed.
5. Git tag with the version (e.g. `v2.0.0`); a GitHub release with `KeePassVeraCryptMount.dll`,
   `KeePassVeraCryptMount.plgx` and `SHA256SUMS.txt` from `dist/`.
6. Keep it neutral: no links in the plugin, and no KeePass `UpdateUrl` (an update check would
   contact a server on every KeePass start).

## 7. When VeraCrypt changes

Get the source (`git clone https://github.com/veracrypt/VeraCrypt`, check out the release tag)
and check:

| What | Where in VeraCrypt | Where in the plugin |
|---|---|---|
| Windows options `/q /v /k /letter /m /pim /hash /explore /beep /tc /secureDesktop` | `src/Mount/Mount.c`, `ExtractCommandLine` (search `OptionPkcs5`, `OptionMountOption`, `OptionSecureDesktop`) | `VeraCryptArguments.BuildWindows` |
| `/q` does not load VeraCrypt's preferences; defaults then (no Secure Desktop, preserve timestamps) | `Mount.c`: `OptionQuit` sets `UsePreferences = FALSE`; defaults in `WM_INITDIALOG` | the plugin passes every option it needs explicitly |
| Which desktop the password window uses | `src/Common/Dlgcode.c`, `SecureDesktopDialogBoxParam` (`bCmdUseSecureDesktopValid`) | `/secureDesktop n` with Auto-Type |
| Linux options `--background-task --mount-options --pim --hash --slot --keyfiles --explore --truecrypt` | `src/Main/CommandLineInterface.cpp` | `VeraCryptArguments.BuildUnix` |
| KDF names on the command line | Windows: `case OptionPkcs5` + `HashGetIdByName` (`src/Common/Crypto.c`); Linux: `FindKdfAlgorithm` | `KdfAlgorithms` (and `MinimumWindowsVersion`) |
| KDF names and order in VeraCrypt's list | `get_pkcs5_prf_name` (`src/Common/Pkcs5.c`), ids in `src/Common/Crypto.h` | `KdfAlgorithms.All`, test `KdfListMatchesVeraCrypt` |
| Password window | `IDD_PASSWORD_DLG` in `src/Mount/Mount.rc`: EDIT `IDC_PASSWORD` (1035, `src/Mount/Resource.h`) and COMBOBOX `IDC_PKCS5_PRF_ID` (5127, `src/Common/Resource.h`); `PasswordDlgProc` puts the focus into the password field | `WindowsProbe.PasswordFieldId`, `KdfListId` (types only with the focus in the password field); the same IDs in `tests/e2e/fakevc.rc` |
| Token PIN window | `IDD_TOKEN_PASSWORD` in `src/Common/Common.rc`: EDIT only | must stay unrecognised |
| Label length (1-32), PIM maximum (2147468) | `Mount.c` (`label=`), `MAX_PIM_VALUE` | `VeraCryptArguments.MaxLabelLength`, `MountSettings.MaxPim` |
| Removed features | release notes | `VeraCryptArguments` version checks, `KdfChoice.RemovedInVeraCrypt126` |

If VeraCrypt changes these control IDs or removes the KDF list, Auto-Type stops working (safely:
nothing is typed, and the plugin says it could not confirm the window). Then adjust `WindowsProbe`
and `tests/e2e/fakevc.rc`, and make sure the plugin still ignores message boxes, the token PIN
window and the PIM field.

## 8. When KeePass changes

| What | Where in KeePass' source | Used by |
|---|---|---|
| PLGX rules (paths, project items, compiler) | `Plugins/PlgxPlugin.cs`, `Plugins/PlgxCsprojLoader.cs` | build, `.csproj` |
| `AutoType.PerformIntoCurrentWindow(entry, db, sequence)`, `AutoType.FilterSend`, `AutoTypeEventArgs.Sequence` | `Util/AutoType.cs` | `PendingAutoType` |
| Checks before every key (`AutoTypeCancelOnWindowChange`, `AutoTypeAbortOnWindows`) | `Util/SendInputExt/SiEngineStd.cs`, `ValidateState` | `TypingSafeguards` |
| Plugins unloaded before the configuration is saved | `Forms/MainForm_Functions.cs`, `CleanUpEx` | `KeePassVeraCryptMountExt.Terminate` |
| Icon names `B16x16_Folder_Yellow_Open`, `B16x16_Reload_Page`, `B16x16_XMag` | `Properties/Resources.resx` | `Resources` (missing icon: text on the button) |
| Placeholders `SprCompileFlags.Paths`, `EnvVars` | `Util/Spr/SprEngine.cs` | `MountController.ExpandPlaceholders` |

After a KeePass update: `KEEPASS_DIR=/path/to/new/KeePass bash tools/build.sh`, run the logic and
end-to-end tests with the new version (`KPSRC=/path/to/new/KeePass` for the e2e scripts).

## 9. Known limitations (deliberate)

* **Linux** cannot tell which control of a window has the focus (X11), so the Linux check is
  "the active, focused window belongs to the VeraCrypt process". Wayland: only through XWayland
  (`GDK_BACKEND=x11` is set for VeraCrypt); not tested on Wayland.
* **VeraCrypt's Secure Desktop** is not used for mounts started by the plugin. KeePass cannot
  type there, so for entries with Auto-Type checked the plugin actively passes
  `/secureDesktop n` (Auto-Type works as intended, whatever VeraCrypt's preference). For entries
  with Auto-Type unchecked nothing is passed; VeraCrypt started with `/q` does not load its
  preferences, so the window is on the normal desktop there too (as in 1.0.x).
* **VeraCrypt run as administrator**: Windows blocks keystrokes from a normal program into it;
  the plugin types nothing and says so in the status bar.
* **The PIM** is passed on VeraCrypt's command line (as in 1.0.x); the dialog's tool tip says so.
* **English only**, like 1.0.x.

## 10. History

* 2010-2013: KeePassTrueCryptMount by Patrick Schaller.
* 2015-2017: KeePassVeraCryptMount 1.0.0-1.0.7 by Frédéric Bourqui.
* 2026: 2.0.0. 1.0.7 no longer loaded in KeePass 2.62 (PLGX path validation). The plugin was
  modernised, given a neutral look, and reviewed for security and reliability; findings
  and fixes are in `docs/SECURITY-REVIEW.md`, user-visible changes in `docs/CHANGELOG.md`.
