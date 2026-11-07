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
// MountSettings.cs - the mount settings of one entry, read from and written to its fields
// -------------------------------------------------------------------------------------------------
// READING (ReadFrom) never fails: malformed values fall back to the defaults of 1.0.x, and old
//   quirks are normalised: drive "K:\" -> "K", KDF with trailing spaces, PIM "0123" -> "123"
//   (VeraCrypt reads a leading 0 on its command line as an octal number: 0123 would be 83).
// WRITING (WriteTo) changes only fields that differ and reports whether anything changed, so a
//   history entry is only created for real changes (KeePassVeraCryptMountExt.SaveIfChanged). It
//   never touches the entry's Auto-Type settings (1.0.x overwrote the default sequence with
//   {PASSWORD}{ENTER} and switched on two-channel obfuscation in every entry it configured).
// VALUE FORMATS (same as 1.0.x)
//   booleans "True"/"False"; drive "X:\" or empty (= first free letter); key files separated by
//   ";"; PIM: digits, protected string, missing = no PIM; KDF: see KdfAlgorithms, missing =
//   Autodetection; "veracrypt.option.askpassword" = True means NO Auto-Type (inverted name).
//
// HISTORY
//   1.0.x: the fields were read and written separately by VeraCryptMountForm and PwEntryExtension.
//   2.0.0 (2026-11): this class.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;
    using System.Globalization;

    using KeePassLib;
    using KeePassLib.Security;

    /// <summary>
    /// The per-entry mount settings, read from and written to the entry's custom string fields
    /// (see <see cref="EntryStrings"/>). Reading never throws: malformed values fall back to
    /// the defaults the old plugin used.
    /// </summary>
    internal sealed class MountSettings
    {
        /// <summary>VeraCrypt's MAX_PIM_VALUE (src/Common/Password.h).</summary>
        public const int MaxPim = 2147468;

        public MountSettings()
        {
            this.Volume = string.Empty;
            this.KeyFiles = string.Empty;
            this.DriveLetter = string.Empty;
            this.Label = string.Empty;
            this.Pim = string.Empty;
            this.Kdf = string.Empty;
            this.AutoType = true;
        }

        public bool Enabled { get; set; }

        public string Volume { get; set; }

        /// <summary>Key files separated by ';' (format of the old versions).</summary>
        public string KeyFiles { get; set; }

        /// <summary>A single upper case letter, or empty for "first free letter".</summary>
        public string DriveLetter { get; set; }

        public string Label { get; set; }

        public bool ReadOnly { get; set; }

        public bool Removable { get; set; }

        public bool Timestamp { get; set; }

        public bool Beep { get; set; }

        public bool Explore { get; set; }

        public bool TrueCryptMode { get; set; }

        public bool HideDialog { get; set; }

        /// <summary>Type the entry password into VeraCrypt (stored inverted as "askpassword").</summary>
        public bool AutoType { get; set; }

        /// <summary>Digits only, or empty when no PIM is used.</summary>
        public string Pim { get; set; }

        /// <summary>Stored KDF value, see <see cref="KdfAlgorithms"/>. Empty = auto-detection.</summary>
        public string Kdf { get; set; }

        public static MountSettings ReadFrom(PwEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            var s = new MountSettings();
            s.Enabled = ReadBool(entry, EntryStrings.Enabled, false);
            s.Volume = entry.Strings.ReadSafe(EntryStrings.Volume);
            s.KeyFiles = entry.Strings.ReadSafe(EntryStrings.KeyFiles);
            s.DriveLetter = NormalizeDriveLetter(entry.Strings.ReadSafe(EntryStrings.DriveLetter));
            s.Label = entry.Strings.ReadSafe(EntryStrings.Label);
            s.ReadOnly = ReadBool(entry, EntryStrings.Readonly, false);
            s.Removable = ReadBool(entry, EntryStrings.Removable, false);
            s.Timestamp = ReadBool(entry, EntryStrings.TimeStamp, false);
            s.Beep = ReadBool(entry, EntryStrings.Beep, false);
            s.Explore = ReadBool(entry, EntryStrings.Explorer, false);
            s.TrueCryptMode = ReadBool(entry, EntryStrings.TrueCryptMode, false);
            s.HideDialog = ReadBool(entry, EntryStrings.MountWithoutDialog, false);
            s.AutoType = !ReadBool(entry, EntryStrings.AskForPassword, false);

            string pim = entry.Strings.ReadSafe(EntryStrings.PIM).Trim();
            int pimValue;
            s.Pim = TryParsePim(pim, out pimValue) ? pimValue.ToString(CultureInfo.InvariantCulture) : string.Empty;

            s.Kdf = entry.Strings.ReadSafe(EntryStrings.PKCS5).Trim();
            return s;
        }

        /// <summary>True if the entry was set up for this plugin (old or new version).</summary>
        public static bool IsEnabledFor(PwEntry entry)
        {
            return entry != null && ReadBool(entry, EntryStrings.Enabled, false);
        }

        public static bool ShouldHideDialogFor(PwEntry entry)
        {
            return entry != null && ReadBool(entry, EntryStrings.MountWithoutDialog, false);
        }

        /// <summary>
        /// Writes the settings into the entry. Returns true if anything changed. Does not touch
        /// the entry's Auto-Type settings (the old version silently overwrote them).
        /// </summary>
        public bool WriteTo(PwEntry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException("entry");
            }

            bool changed = false;
            changed |= SetString(entry, EntryStrings.Enabled, bool.TrueString, false);
            changed |= SetString(entry, EntryStrings.Volume, this.Volume, false);
            changed |= SetString(entry, EntryStrings.KeyFiles, this.KeyFiles, false);
            changed |= SetString(entry, EntryStrings.Label, this.Label, false);
            changed |= SetString(entry, EntryStrings.DriveLetter,
                string.IsNullOrEmpty(this.DriveLetter) ? string.Empty : this.DriveLetter + ":\\", false);
            changed |= SetString(entry, EntryStrings.MountWithoutDialog, this.HideDialog.ToString(), false);
            changed |= SetString(entry, EntryStrings.TrueCryptMode, this.TrueCryptMode.ToString(), false);
            changed |= SetString(entry, EntryStrings.Removable, this.Removable.ToString(), false);
            changed |= SetString(entry, EntryStrings.Readonly, this.ReadOnly.ToString(), false);
            changed |= SetString(entry, EntryStrings.TimeStamp, this.Timestamp.ToString(), false);
            changed |= SetString(entry, EntryStrings.Beep, this.Beep.ToString(), false);
            changed |= SetString(entry, EntryStrings.Explorer, this.Explore.ToString(), false);
            changed |= SetString(entry, EntryStrings.AskForPassword, (!this.AutoType).ToString(), false);

            // Obsolete fields of version 1.0.3 and older.
            changed |= entry.Strings.Remove(EntryStrings.Background);
            changed |= entry.Strings.Remove(EntryStrings.Silent);

            if (string.IsNullOrEmpty(this.Kdf))
            {
                changed |= entry.Strings.Remove(EntryStrings.PKCS5);
            }
            else
            {
                changed |= SetString(entry, EntryStrings.PKCS5, this.Kdf, false);
            }

            int pim;
            if (TryParsePim(this.Pim, out pim))
            {
                changed |= SetString(entry, EntryStrings.PIM, pim.ToString(CultureInfo.InvariantCulture), true);
            }
            else
            {
                changed |= entry.Strings.Remove(EntryStrings.PIM);
            }

            return changed;
        }

        /// <summary>Accepts 1 to <see cref="MaxPim"/>, digits only (no sign, no spaces).</summary>
        public static bool TryParsePim(string text, out int value)
        {
            value = 0;
            if (string.IsNullOrEmpty(text) || text.Length > 7)
            {
                return false;
            }

            foreach (char c in text)
            {
                if (c < '0' || c > '9')
                {
                    return false;
                }
            }

            int parsed;
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out parsed))
            {
                return false;
            }

            if (parsed < 1 || parsed > MaxPim)
            {
                return false;
            }

            value = parsed;
            return true;
        }

        /// <summary>"x", "X:", "X:\" become "X"; anything else becomes empty (= automatic).</summary>
        public static string NormalizeDriveLetter(string stored)
        {
            if (string.IsNullOrEmpty(stored))
            {
                return string.Empty;
            }

            string s = stored.Trim();
            if (s.Length == 0)
            {
                return string.Empty;
            }

            char c = char.ToUpperInvariant(s[0]);
            if (c < 'A' || c > 'Z')
            {
                return string.Empty;
            }

            string rest = s.Substring(1);
            if (rest.Length == 0 || rest == ":" || rest == ":\\")
            {
                return c.ToString();
            }

            return string.Empty;
        }

        private static bool ReadBool(PwEntry entry, string field, bool defaultValue)
        {
            string text = entry.Strings.ReadSafe(field);
            bool value;
            return bool.TryParse(text.Trim(), out value) ? value : defaultValue;
        }

        private static bool SetString(PwEntry entry, string field, string value, bool protect)
        {
            string newValue = value ?? string.Empty;
            ProtectedString old = entry.Strings.Get(field);
            if (old != null && old.IsProtected == protect && old.ReadString() == newValue)
            {
                return false;
            }

            entry.Strings.Set(field, new ProtectedString(protect, newValue));
            return true;
        }
    }
}
