/*
  Plugin for KeePass Password Safe - The Open-Source Password Manager
  KeePassVeraCryptMount
  Copyright (C) 2015-2017 Frédéric Bourqui
  Copyright (C) 2026 KeePassVeraCryptMount contributors

  fork of:
  KeePassTrueCryptMount
  Copyright (C) 2010-2013 Patrick Schaller

  This program is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

// =================================================================================================
// LanguageTexts.cs - every text the plugin shows (menus, messages, status bar, tool tips)
// -------------------------------------------------------------------------------------------------
// English only, as in 1.0.x. Texts with {0}/{1}/{2} are used with string.Format (invariant
// culture). Status bar texts start with "VeraCrypt Mount:" so the user sees where they come from.
// Line breaks are "\r\n" (KeePass message boxes on Windows and Mono).
//
// HISTORY
//   1.0.x: menu and banner texts (kept unchanged) and a few error messages.
//   2.0.0 (2026-11): messages for every check of the new version; tool tips moved here from the
//          designer .resx files.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    internal static class LanguageTexts
    {
        public const string MenuItemOpenDialogSuffix = "...";

        public const string BannerPluginTitleText = "VeraCrypt Mount Plugin";

        public const string BannerOptionsDescriptionText = "Setup global VeraCrypt options";

        public const string BannerMountFormDescriptionText = "Edit VeraCrypt mount entry settings";

        public const string TCMountMenuItemText = "Mount volume";

        public const string TCOptionsMenuItemText = "VeraCrypt Mount Plugin Options";

        public const string MountErrorTitle = "VeraCrypt Mount";

        // --- settings problems -------------------------------------------------------------

        public const string NoVolumeSpecified = "No volume has been specified.";

        public const string VolumeNotFound = "Volume not found:\r\n";

        public const string KeyFileNotFound = "Key file not found:\r\n";

        public const string InvalidPath = "This path cannot be used:\r\n";

        public const string VolumeIdInvalid = "A volume ID must be \"ID:\" followed by 64 hexadecimal characters:\r\n";

        public const string VolumeIdWindowsOnly = "Volume IDs (ID:...) are only supported by VeraCrypt for Windows.";

        public const string InvalidPim = "The PIM must be a whole number from 1 to 2147468.";

        public const string InvalidLabel = "The volume label must have at most 32 characters and must not contain quotation marks.";

        public const string UnknownKdf = "Unknown PRF/KDF setting in this entry: ";

        public const string TrueCryptModeRemoved =
            "TrueCrypt mode is not supported by VeraCrypt 1.26.7 and later (installed: {0}).\r\n\r\n" +
            "Turn off \"TrueCrypt Mode\" for this entry. To keep using an old TrueCrypt volume, " +
            "convert it with VeraCrypt 1.25.9 first.";

        public const string KdfRemoved =
            "{0} is not supported by VeraCrypt 1.26.7 and later (installed: {1}).\r\n\r\n" +
            "Choose another PRF/KDF (or Autodetection) for this entry.";

        public const string KdfNeedsNewerVeraCrypt =
            "{0} needs VeraCrypt {1} or later for Windows (installed: {2}).\r\n\r\n" +
            "Update VeraCrypt, or choose Autodetection as PRF/KDF for this entry.";

        // --- starting VeraCrypt and auto-type --------------------------------------------------

        public const string ExecutableMissing =
            "The VeraCrypt program was not found. Set its location in Tools > VeraCrypt Mount Plugin Options.";

        public const string StartFailed = "VeraCrypt could not be started:\r\n";

        public const string AutoTypeFailed = "Auto-Type failed:\r\n";

        public const string WaitingForVeraCrypt = "VeraCrypt Mount: waiting for VeraCrypt's password window...";

        public const string AutoTypeDone = "VeraCrypt Mount: Auto-Type done. If VeraCrypt still asks for the password, type it there yourself.";

        public const string AutoTypeNoWindow =
            "VeraCrypt Mount: VeraCrypt did not show its password window in time, nothing was typed.";

        public const string VeraCryptExited =
            "VeraCrypt Mount: VeraCrypt finished without asking for a password, nothing was typed.";

        public const string AutoTypeCancelled =
            "VeraCrypt Mount: auto-type cancelled, nothing was typed.";

        public const string AutoTypeNotVerified =
            "The plugin did not type the password, because it could not confirm that VeraCrypt's " +
            "password window was the active window with the cursor in an input field.\r\n\r\n" +
            "Please type the password into VeraCrypt yourself.\r\n\r\n" +
            "Possible reason: another window took the focus while VeraCrypt was starting.";

        public const string AutoTypeUnsupported =
            "The plugin did not type the password, because it cannot check which window is " +
            "active on this system (on Linux the 'xdotool' package is required).\r\n\r\n" +
            "Please type the password into VeraCrypt yourself, or install xdotool.";

        public const string AutoTypeElevated =
            "VeraCrypt Mount: VeraCrypt runs as administrator, so KeePass cannot type into it. Please type the password yourself.";

        public const string AutoTypeDisabledForEntry =
            "Auto-Type is disabled for this entry (or its group) in KeePass, so the plugin will " +
            "not type the password. VeraCrypt opens next: please type the password there yourself, " +
            "or enable Auto-Type in the entry's \"Auto-Type\" tab.";

        // --- tool tips ---------------------------------------------------------------------------

        public const string HideDialogToolTip =
            "When checked, the next \"Mount volume\" starts without showing this dialog.\r\n" +
            "To show it again, select the entry and press Ctrl+M.";

        public const string LabelToolTip =
            "Optional label of the mounted volume in Windows Explorer.\r\n" +
            "At most 32 characters for NTFS volumes and 11 characters for FAT volumes.\r\n" +
            "Do not add quotes, even if the label contains spaces.";

        public const string AutoTypeToolTip =
            "Types the entry's password into VeraCrypt's password window, only after the plugin " +
            "has confirmed that this window belongs to the VeraCrypt process it started.\r\n" +
            "Sequence used: the entry's own Auto-Type sequence if it has one, otherwise {PASSWORD}{ENTER}.\r\n" +
            "UNCHECK to type the password into VeraCrypt yourself.";

        public const string TrueCryptToolTip =
            "Mount a TrueCrypt volume. Only works with VeraCrypt 1.25.9 or older: " +
            "VeraCrypt 1.26.7 removed TrueCrypt mode.";

        public const string VolumeToolTip =
            "Path of a volume file, a device such as \\Device\\Harddisk1\\Partition3, or ID:<64 hex digits>.\r\n" +
            "Placeholders like {DB_DIR} or {APPDIR} and %ENVIRONMENT% variables can be used.\r\n" +
            "Relative paths are relative to the KeePass program folder. Do not add quotes.";

        public const string KeyFilesToolTip =
            "Key files or key file folders, separated by ';'.\r\n" +
            "Placeholders like {DB_DIR} and %ENVIRONMENT% variables can be used. Do not add quotes.";

        public const string KdfToolTip =
            "The PRF / key derivation function of the volume, as VeraCrypt calls it in its password window.\r\n" +
            "Autodetection works for every volume; choosing the right one only makes mounting faster.\r\n" +
            "Argon2 needs VeraCrypt 1.26.30 or later on Windows.";

        public const string PimToolTip =
            "Personal Iterations Multiplier: 1 to 2147468. Leave \"Use PIM\" unchecked for the default.\r\n" +
            "Note: the PIM is passed to VeraCrypt on its command line.";
    }
}
