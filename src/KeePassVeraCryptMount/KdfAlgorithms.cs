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
// KdfAlgorithms.cs - the "PRF / KDF" list of the mount dialog
// -------------------------------------------------------------------------------------------------
// WHAT
//   For each choice: the value stored in the entry, the name shown in the list, and the value
//   passed to VeraCrypt's /hash (Windows) or --hash= (Linux). "Autodetection" passes nothing:
//   VeraCrypt then tries every KDF (Argon2 too), which always works but takes longer, most of all
//   with Argon2 (it is slow on purpose).
//
// COMPATIBILITY CONTRACT
//   StoredValue strings are written into entries ("veracrypt.option.pkcs5") and must never change.
//   1.0.x stored "HMAC-SHA-512", "HMAC-Whirlpool", "HMAC-SHA-256  " (with two trailing spaces, a
//   1.0.x bug), "HMAC-RIPEMD-160" and "HMAC-Streebog"; FromStoredValue() trims and ignores case,
//   so all of them are recognised. 2.0.0 added "HMAC-BLAKE2s-256" and "Argon2id". An unknown
//   stored value is shown as "Unknown: ..." and never passed to VeraCrypt (VeraCryptArguments).
//   The same database used with plugin 1.0.x (older KeePass elsewhere): 1.0.x passes the text after
//   "HMAC-" to VeraCrypt, so "HMAC-BLAKE2s-256" still works there, while "Argon2id" ends in
//   VeraCrypt's "command line error" (use Autodetection for such shared entries).
//
// WHERE THE VALUES COME FROM (VeraCrypt source, tags 1.26.29 to 1.26.31, checked November 2026)
//   Display names: VeraCrypt's own names in its password dialog, get_pkcs5_prf_name() in
//     src/Common/Pkcs5.c, in VeraCrypt's order (ids SHA512, WHIRLPOOL, SHA256, BLAKE2S, STREEBOG,
//     ARGON2 in src/Common/Crypto.h). Older VeraCrypt versions showed "HMAC-SHA-512" etc.
//   Windows /hash: src/Mount/Mount.c "case OptionPkcs5": sha512, sha256, blake2s, blake2s-256,
//     else HashGetIdByName() (src/Common/Crypto.c), case-insensitive; anything unknown makes
//     VeraCrypt stop with "command line error".
//   Argon2: added in VeraCrypt 1.26.29. The names "Argon2"/"Argon2id" for /hash exist since
//     1.26.30 (1.26.29 for Windows only knew the internal table name "BLAKE2b-512"), hence
//     MinimumWindowsVersion. Linux: --hash=Argon2id since 1.26.29 (FindKdfAlgorithm()).
//   Linux --hash: src/Main/CommandLineInterface.cpp FindKdfAlgorithm(): KDF names, hash names
//     and alternative names (SHA512, SHA256, BLAKE2s, Whirlpool, Streebog), case-insensitive.
//   Removed: RIPEMD-160 (and TrueCrypt mode) in VeraCrypt 1.26.7 -> kept as a clearly marked
//     legacy choice for old entries, refused with a message for VeraCrypt 1.26.7 and later.
//
// WHEN VERACRYPT CHANGES
//   Check the places above, add new choices before the legacy entry (never rename StoredValue),
//   and extend the tests KdfValues / KdfListMatchesVeraCrypt (tests/KeePassVeraCryptMount.Tests).
//
// HISTORY
//   1.0.x: a fixed string list in the designer; the value after "HMAC-" was passed to VeraCrypt.
//   2.0.0 (2026-11): this file; Argon2 and BLAKE2s added, names as in VeraCrypt 1.26.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;

    /// <summary>One choice of the "PRF / KDF" list of the mount dialog.</summary>
    internal sealed class KdfChoice
    {
        public KdfChoice(string storedValue, string displayName, string windowsArgument, string unixArgument,
            Version minimumWindowsVersion, bool removedInVeraCrypt126)
        {
            this.StoredValue = storedValue;
            this.DisplayName = displayName;
            this.WindowsArgument = windowsArgument;
            this.UnixArgument = unixArgument;
            this.MinimumWindowsVersion = minimumWindowsVersion;
            this.RemovedInVeraCrypt126 = removedInVeraCrypt126;
        }

        /// <summary>Value stored in the entry. Never change existing values (compatibility).</summary>
        public string StoredValue { get; private set; }

        /// <summary>Text shown in the drop-down (VeraCrypt's own name for the KDF).</summary>
        public string DisplayName { get; private set; }

        /// <summary>Value for /hash on Windows; null means auto-detection (no /hash).</summary>
        public string WindowsArgument { get; private set; }

        /// <summary>Value for --hash= on Linux/macOS; null means auto-detection.</summary>
        public string UnixArgument { get; private set; }

        /// <summary>Oldest VeraCrypt for Windows that accepts <see cref="WindowsArgument"/>; null = any.</summary>
        public Version MinimumWindowsVersion { get; private set; }

        /// <summary>True for RIPEMD-160, which VeraCrypt 1.26.7 removed completely.</summary>
        public bool RemovedInVeraCrypt126 { get; private set; }

        public bool IsAutoDetection
        {
            get { return this.WindowsArgument == null; }
        }

        public string ArgumentFor(TargetPlatform platform)
        {
            return platform == TargetPlatform.Windows ? this.WindowsArgument : this.UnixArgument;
        }

        public override string ToString()
        {
            return this.DisplayName;
        }
    }

    internal static class KdfAlgorithms
    {
        /// <summary>First VeraCrypt for Windows that accepts "/hash argon2id".</summary>
        public static readonly Version Argon2WindowsMinimum = new Version(1, 26, 30);

        public static readonly KdfChoice AutoDetection =
            new KdfChoice(string.Empty, "Autodetection", null, null, null, false);

        /// <summary>In the order of VeraCrypt's own list; legacy entries last.</summary>
        public static readonly KdfChoice[] All = new[]
        {
            AutoDetection,
            new KdfChoice("HMAC-SHA-512", "SHA512-PBKDF2", "sha512", "sha512", null, false),
            new KdfChoice("HMAC-Whirlpool", "Whirlpool-PBKDF2", "whirlpool", "whirlpool", null, false),
            new KdfChoice("HMAC-SHA-256", "SHA256-PBKDF2", "sha256", "sha256", null, false),
            new KdfChoice("HMAC-BLAKE2s-256", "BLAKE2S-PBKDF2", "blake2s", "blake2s", null, false),
            new KdfChoice("HMAC-Streebog", "STREEBOG-PBKDF2", "streebog", "streebog", null, false),
            new KdfChoice("Argon2id", "Argon2", "argon2id", "argon2id", Argon2WindowsMinimum, false),
            new KdfChoice("HMAC-RIPEMD-160", "HMAC-RIPEMD-160 (VeraCrypt 1.25.9 and older)", "ripemd160", "ripemd160", null, true),
        };

        /// <summary>
        /// The choice for a value stored in an entry: <see cref="AutoDetection"/> for an empty value,
        /// null for an unknown value (which must then not be passed to VeraCrypt).
        /// </summary>
        public static KdfChoice FromStoredValue(string stored)
        {
            string value = (stored ?? string.Empty).Trim();
            if (value.Length == 0)
            {
                return AutoDetection;
            }

            foreach (KdfChoice choice in All)
            {
                if (string.Equals(choice.StoredValue, value, StringComparison.OrdinalIgnoreCase))
                {
                    return choice;
                }
            }

            return null;
        }
    }
}
