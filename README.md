# KeePassVeraCryptMount

A [KeePass](https://keepass.info) plugin that mounts a [VeraCrypt](https://veracrypt.io) volume
(file container, partition or disk) from a KeePass entry and types the entry's password into
VeraCrypt's password window.

Version 2.0.0 continues KeePassVeraCryptMount 1.0.7 (2017), which **no longer loads in KeePass
2.62**. It loads again, keeps the settings of existing entries, supports the current VeraCrypt
(including Argon2), and types the password only into VeraCrypt's own password window. The plugin
contains no links and no update check, and has no icon or images of its own.

**Version 2.0.0 was made entirely by AI**: the code changes, the security review, the tests and
this documentation. It builds on the original code by the authors named under
[Credits and license](#credits-and-license).

* What changed: [docs/CHANGELOG.md](docs/CHANGELOG.md)
* Security and reliability notes: [docs/SECURITY-REVIEW.md](docs/SECURITY-REVIEW.md)
* Reporting a security problem: [docs/SECURITY.md](docs/SECURITY.md)
* Try it with a test database and a test volume before using it with your real vault.

## Requirements

* **KeePass 2.36 or newer** (tested with 2.62). Windows, or Linux/macOS with Mono.
* **VeraCrypt** (checked against VeraCrypt 1.26.31).
* Linux: `xdotool` (KeePass needs it for Auto-Type anyway).

## Install

1. Close KeePass.
2. If you have the old plugin, delete `KeePassVeraCryptMount.plgx` from KeePass' `Plugins` folder.
3. Copy **one** of the two files of the release into the `Plugins` folder (not both):
   * `KeePassVeraCryptMount.dll` — recommended for KeePass 2.62 or newer;
   * `KeePassVeraCryptMount.plgx` — for any KeePass from 2.36 on (KeePass compiles it at start-up).
4. Start KeePass. Open **Tools → VeraCrypt Mount Plugin Options** and check that the path to
   `VeraCrypt.exe` is right (the search button finds a normal installation).

Optionally compare the files with `SHA256SUMS.txt` of the release
(PowerShell: `Get-FileHash .\KeePassVeraCryptMount.dll`).

## Upgrading from 1.0.7

Nothing to convert: entries keep their mount settings (same field names), and the plugin options
keep their values. Differences you may notice:

* **Mount** in the dialog now saves the settings and mounts what the dialog shows (1.0.7 mounted
  the previously saved settings). **Apply** saves without mounting. KeePass keeps the previous
  values in the entry's History, and asks to save the database as usual.
* The plugin no longer changes the entry's Auto-Type settings. Entries set up with 1.0.7 already
  have the sequence `{PASSWORD}{ENTER}`; that is what is typed.
* Relative volume paths are relative to the KeePass program folder. For a container next to the
  database, use `{DB_DIR}\container.hc`.
* The maximum wait for VeraCrypt's password window is at least 10 seconds (it was 3 seconds).
  Typing starts as soon as the window is ready, so a longer maximum costs nothing.
* TrueCrypt mode and HMAC-RIPEMD-160 were removed from VeraCrypt in 1.26.7. With a newer
  VeraCrypt, the plugin tells you before VeraCrypt starts.
* Neutral look: no icon of its own (1.0.7 showed VeraCrypt logos) and no "Donate" / "Help"
  links in the options dialog. If VeraCrypt is not found, "Mount volume" says where to set its
  path (1.0.7 greyed the item out).

## Use

* **Set up an entry:** select the entry whose password is the volume password and press
  **Ctrl+M**. Choose the volume, the drive letter and the options, then click **Mount** (or
  **Apply** to save only).
* **Mount:** right-click the entry → **Mount volume**. With "Hide dialog next time" checked the
  menu item has no "..." and mounts directly; Ctrl+M always opens the dialog.

The dialog:

| Setting | Meaning |
|---|---|
| Volume File \| Partition \| Harddrive | The volume: a file, a device path such as `\Device\Harddisk1\Partition2`, or a VeraCrypt volume ID `ID:` + 64 hex digits. KeePass placeholders like `{DB_DIR}` and `%ENVIRONMENT%` variables work. |
| KeyFile(s) | Key files or key file folders, separated by `;`. Security token key files (`token://...`) work too. |
| Drive letter | A letter, or `<Auto Select>` for the first free one (Linux: the VeraCrypt slot number). |
| Readonly, Removable, TimeStamp, Beep, Open Explorer | VeraCrypt's mount options `ro`, `rm`, `ts` (do not keep the container's modification time), `/beep`, `/explore`. Removable and Beep do not exist on Linux. |
| Windows explorer volume label | Optional name of the drive in Explorer (at most 32 characters, Windows only). |
| Auto-Type | Type the entry's password into VeraCrypt (see below). Uncheck it to type the password yourself. |
| PRF / KDF | The volume's key derivation function, as VeraCrypt's own list calls it. **Autodetection** always works; choosing the right one only makes mounting faster. **Argon2** needs VeraCrypt 1.26.30 or newer on Windows. |
| Use PIM | The volume's PIM, if it has one. Note: the PIM is passed to VeraCrypt on its command line, which other programs of your account can read; leave it unchecked and type the PIM into VeraCrypt if that matters to you. The password is never passed on the command line. |
| TrueCrypt Mode | Only for TrueCrypt volumes with VeraCrypt 1.25.9 or older. |
| Hide dialog next time | One-click mounting: the menu item becomes "Mount volume" (no "...") and mounts with the saved settings without opening this dialog. Ctrl+M still opens it. |

## How Auto-Type is kept safe

The plugin starts VeraCrypt and then waits — without blocking KeePass — until the password
window of *that* VeraCrypt process is the active window and the cursor is in its password field.
Only then does KeePass type the sequence, once. While it types, KeePass checks before every key
that VeraCrypt's window is still the active one, and stops otherwise. If any of this cannot be
confirmed, nothing is typed and you type the password into VeraCrypt yourself.

What is typed: the entry's own Auto-Type sequence (entry → Auto-Type tab) if it has one,
otherwise `{PASSWORD}{ENTER}`. Two-channel Auto-Type obfuscation is used if it is switched on
for the entry.

**VeraCrypt's Secure Desktop.** KeePass cannot type into VeraCrypt's Secure Desktop. For entries
with Auto-Type checked, the plugin therefore actively passes `/secureDesktop n` (VeraCrypt 1.20
and newer): the password window opens on the normal desktop and Auto-Type works as intended, also
when "Use Secure Desktop for password entry" is on in VeraCrypt. For entries with Auto-Type
unchecked, the plugin passes no such option. VeraCrypt started by the plugin (`/q background`, as
by 1.0.7) does not load VeraCrypt's preferences, so in practice its password window is on the
normal desktop there as well; the entry's mount options apply instead. Mounting with VeraCrypt
itself is not affected. The Secure Desktop protects a password typed there against programs that
watch the normal desktop (keyloggers, screen capture). Auto-Type on the normal desktop does not
have that protection; two-channel obfuscation (entry → Auto-Type tab) makes it harder for a
keylogger to record the password.

## Messages and what they mean

| You see | Meaning |
|---|---|
| Status bar "Auto-Type done. If VeraCrypt still asks for the password, type it there yourself." | The password was typed. |
| "The VeraCrypt program was not found. Set its location in Tools > VeraCrypt Mount Plugin Options." | The path in the plugin options is empty or wrong; the search button there finds a normal installation. |
| Status bar "VeraCrypt did not show its password window in time, nothing was typed." | No password window appeared (VeraCrypt showed an error, or the volume was mounted with a cached password). Increase the wait in the plugin options on a slow computer. |
| Status bar "VeraCrypt finished without asking for a password, nothing was typed." | VeraCrypt ended on its own, e.g. it mounted with a cached password or reported an error. |
| "The plugin did not type the password, because it could not confirm that VeraCrypt's password window was the active window..." | Another window was in front. Type the password into VeraCrypt yourself. |
| "Auto-Type is disabled for this entry (or its group) in KeePass..." | The entry's Auto-Type tab has Auto-Type switched off; VeraCrypt still opens, type the password there. |
| KeePass: "Auto-Type has been aborted, because the target window is disallowed by the application policy (defined by your administrator)." | Another window came to the front while typing; the plugin's safety check stopped KeePass. Despite the wording, no administrator policy is involved. |
| Status bar "VeraCrypt runs as administrator, so KeePass cannot type into it." | VeraCrypt.exe is set to "Run as administrator"; Windows blocks typing into it. Type the password yourself. |
| "Argon2 needs VeraCrypt 1.26.30 or later for Windows" | Update VeraCrypt, or choose Autodetection. |
| "TrueCrypt mode is not supported by VeraCrypt 1.26.7 and later" / "HMAC-RIPEMD-160 is not supported..." | Old entry settings: uncheck TrueCrypt Mode, or choose another PRF/KDF (Autodetection always works). |

## Build from source

```
bash tools/build.sh   # Linux: .NET 8 SDK, Mono, xvfb-run, Python 3; KeePass portable ZIP in lib/KeePass
```

Output: `dist/KeePassVeraCryptMount.dll`, `dist/KeePassVeraCryptMount.plgx`, `dist/SHA256SUMS.txt`.
The code must stay C# 5 compatible (KeePass compiles `.plgx` plugins with the C# 5 compiler on
Windows). Architecture, rules, tests and the release checklist: [docs/MAINTAINING.md](docs/MAINTAINING.md).

## Credits and license

* KeePassTrueCryptMount: Patrick Schaller (2010-2013)
* KeePassVeraCryptMount 1.0.x: Frédéric Bourqui (2015-2017)
* Version 2.0.0: KeePassVeraCryptMount contributors (2026), made entirely by AI

GNU General Public License, version 3 or (at your option) any later version: [LICENSE](LICENSE)
(up to 1.0.7: version 2 or any later version, which allows this). Copyright notices and history
of included material: [LicenseInfo.txt](LicenseInfo.txt).
This plugin is not affiliated with KeePass or VeraCrypt (IDRIX).
