# Changelog

## 2.0.0

First version after 1.0.7 (2017). Every finding behind these changes is described in
[SECURITY-REVIEW.md](SECURITY-REVIEW.md).

### Compatibility

* Loads again in **KeePass 2.62** (the 1.0.7 PLGX contained the path `..\..\License.txt`, which
  KeePass 2.62 refuses). Works with KeePass 2.36 or newer.
* Entries set up with 1.0.x keep their settings (same field names and formats), the plugin options
  keep their values, the menu items and Ctrl+M work as before.

### License

* **GNU GPL version 3 or (at your option) any later version.** Up to 1.0.7 the plugin was
  released under version 2 or any later version, which allows this. The original authors'
  copyright notices are kept (`LicenseInfo.txt`, top of every source file); `LICENSE` holds the
  GPL v3 text.

### New

* **PRF / KDF list as in VeraCrypt 1.26**, with VeraCrypt's own names: SHA512-PBKDF2,
  Whirlpool-PBKDF2, SHA256-PBKDF2, BLAKE2S-PBKDF2, STREEBOG-PBKDF2 and **Argon2** (Argon2 needs
  VeraCrypt 1.26.30 or newer on Windows; the plugin checks the installed version). Autodetection
  stays the default and works for every volume.
* Esc closes the mount dialog.

### Security

* **Auto-Type types only into the password window of the VeraCrypt process the plugin started**,
  once it is the active window with the cursor in its password field; never into another window,
  and on Windows never into another field (such as the PIM field) or another VeraCrypt window.
  KeePass checks before every key that this window is still in front. Nothing is typed on
  timeout, error, or when the database is closed or locked meanwhile.
* VeraCrypt's error messages and its security token PIN window are never typed into (a wrong PIN
  would count against the token's retry limit).
* VeraCrypt set to "Run as administrator": Windows blocks typing into it; the plugin types nothing
  and says so.
* For entries with Auto-Type checked, the plugin actively passes `/secureDesktop n`, so that
  VeraCrypt's password window is on the normal desktop and Auto-Type works as intended, also when
  "Use Secure Desktop for password entry" is on in VeraCrypt (KeePass cannot type into the Secure
  Desktop). Entries with Auto-Type unchecked get no such option. (VeraCrypt started with `/q`, as
  by 1.0.7, does not load its preferences; Auto-Type no longer depends on that.)
* The command line is built from validated values with correct quoting: no value stored in an
  entry can add VeraCrypt options. The password is never on the command line (as before).
* The entry's Auto-Type settings are no longer overwritten when saving mount settings.
* KeePass settings the plugin switches on while typing are always restored (also after a crash).
* No update check: the plugin contacts no server.
* No links and no web pages: the "Donate" and "Help" links of the options dialog were removed.

### Reliability

* **Mount** saves and mounts what the dialog shows; **Apply** saves immediately, with a history
  entry; the database that contains the entry is used when several databases are open.
* `{DB_DIR}` and the other path placeholders work; relative paths are relative to the KeePass
  program folder; missing volume or key files are reported before VeraCrypt starts.
* The entry's drive letter is kept even while it is in use; the PIM is normalised (a leading 0
  would have been read as an octal number) and range-checked.
* Linux: several key files work; the volume label is no longer erased when an entry is saved on
  Linux; `~/` paths; on Wayland, VeraCrypt is started under XWayland so Auto-Type can reach it.
* VeraCrypt 1.26.7 and newer: a clear message for TrueCrypt mode and HMAC-RIPEMD-160 (removed by
  VeraCrypt) instead of VeraCrypt's "command line error".
* KeePass no longer freezes while waiting for VeraCrypt; no unhandled exceptions; no handle leaks.
* Maximum wait for the password window: 10-60 s (default 10 s; 1.0.x default: 3 s). Typing starts
  as soon as the window is ready.

### Neutral look

* No icon of its own: the menu items and the banners of both dialogs show none (1.0.x showed
  VeraCrypt logos there, and the options dialog a picture of the menu). KeePass' Plugins dialog
  shows its standard plugin icon, as for every plugin without an icon.
* The plugin ships no images: the small buttons use KeePass' own icons, which also replace the
  two button icons of 1.0.x with non-commercial licenses.
* "Mount volume" stays enabled when VeraCrypt is not found and then says where to set the path
  (1.0.7 greyed it out with an error icon).
* Author field "KeePassVeraCryptMount contributors"; the copyright line names every author.

### Build and documentation

* SDK-style project (C# 5 / .NET Framework 4.0, as KeePass' PLGX compiler requires),
  deterministic build, PLGX packed by KeePass itself, logic tests and end-to-end tests.
* Assembly GUID derived from the project name (`uuid5(NAMESPACE_DNS, "KeePassVeraCryptMount")`)
  instead of a random one; no update manifest file; the PLGX carries no build time.
* Every source file starts with a header explaining what it does, why, and its history;
  [MAINTAINING.md](MAINTAINING.md) describes the architecture, the rules, and what to
  check when KeePass or VeraCrypt changes.

## 1.0.7 and older

Original project by Frédéric Bourqui (2015-2017).
