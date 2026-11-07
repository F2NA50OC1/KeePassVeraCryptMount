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
// EntryStrings.cs - names of the entry fields that hold a volume's mount settings
// -------------------------------------------------------------------------------------------------
// COMPATIBILITY CONTRACT
//   The names, and the value formats described in MountSettings, are the same as in 1.0.x:
//   entries set up with 1.0.x work unchanged, and 1.0.x still reads entries saved by 2.0.0 (for the
//   new KDF values see KdfAlgorithms). Never rename a field. A new setting needs a new field name,
//   which older versions simply ignore.
//   The fields are ordinary custom string fields: visible in KeePass' entry editor (Advanced tab),
//   saved in the database and kept in the entry's history like any other field.
//
// HISTORY
//   1.0.0-1.0.7: these names. "veracrypt.option.silent" and "veracrypt.option.quit" are obsolete
//                since 1.0.4; 2.0.0 removes them when it saves an entry.
//   2.0.0 (2026-11): names unchanged; comments added.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    /// <summary>
    /// Names of the custom string fields in which the plugin stores its settings inside a
    /// KeePass entry. These names are unchanged since version 1.0 so that entries created with
    /// older versions keep working. Do not rename them.
    /// </summary>
    internal static class EntryStrings
    {
        public const string Enabled = "veracrypt.enabled";

        /// <summary>Stored as "X:\" (only the first letter is used).</summary>
        public const string DriveLetter = "veracrypt.drive";

        public const string Volume = "veracrypt.container";

        /// <summary>Several key files separated by ';'.</summary>
        public const string KeyFiles = "veracrypt.keyfiles";

        public const string Readonly = "veracrypt.option.readonly";

        public const string Removable = "veracrypt.option.removable";

        /// <summary>Obsolete since 1.0.4, removed from entries when settings are saved.</summary>
        public const string Silent = "veracrypt.option.silent";

        /// <summary>Obsolete since 1.0.4, removed from entries when settings are saved.</summary>
        public const string Background = "veracrypt.option.quit";

        public const string Beep = "veracrypt.option.beep";

        public const string Explorer = "veracrypt.option.explorer";

        public const string MountWithoutDialog = "veracrypt.mount.without.dialog";

        /// <summary>"True" means: do NOT auto-type, let the user type the password. (Inverted name kept for compatibility.)</summary>
        public const string AskForPassword = "veracrypt.option.askpassword";

        public const string TrueCryptMode = "veracrypt.option.truecryptmode";

        public const string TimeStamp = "veracrypt.option.timestamp";

        /// <summary>Stored as a protected string.</summary>
        public const string PIM = "veracrypt.option.pim";

        /// <summary>Key derivation function, e.g. "HMAC-SHA-512". Missing means auto-detection.</summary>
        public const string PKCS5 = "veracrypt.option.pkcs5";

        public const string Label = "veracrypt.option.label";
    }
}
