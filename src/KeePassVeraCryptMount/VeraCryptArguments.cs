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
// VeraCryptArguments.cs - the validated argument list for one VeraCrypt start
// -------------------------------------------------------------------------------------------------
// WHAT
//   Turns the entry's MountSettings (plus the volume and key file paths already resolved by
//   PathResolver) into a list of single arguments. CommandLine.Join quotes them for the platform.
//   Every value is checked here, so nothing stored in an entry can add options of its own.
//
// THE PASSWORD IS NEVER AN ARGUMENT
//   VeraCrypt's /p (--password) would expose the password to every program of the same user that
//   lists command lines, and to process logging tools. It is typed into VeraCrypt's password window
//   (Auto-Type) or by the user. The PIM is an argument (as in 1.0.x): the mount dialog's tool tip
//   says so.
//
// ARGUMENTS (checked against VeraCrypt 1.26.31 in November 2026)
//   Windows (src/Mount/Mount.c, ExtractCommandLine; documentation "Command Line Usage"):
//     /q background  [/secureDesktop n]  [/beep]  [/m ro] [/m ts] [/m rm] [/m "label=..."]
//     [/explore]  [/tc] or ([/pim N] [/hash KDF])  [/letter X]  /v VOLUME  [/k KEYFILE]...
//     "/q background": no VeraCrypt main window; VeraCrypt shows only its password window and
//     stays in the tray afterwards (same as 1.0.x). Side effect (Mount.c, OptionQuit:
//     UsePreferences = FALSE): VeraCrypt does not load its preferences, so its own defaults
//     apply, among them "no Secure Desktop" and "preserve timestamps"; the entry's options are
//     passed explicitly.
//     "/secureDesktop n" (VeraCrypt 1.20 and later, only with Auto-Type): the password window
//     must be on the normal desktop, where KeePass can type. Without /q's side effect VeraCrypt's
//     "Use Secure Desktop for password entry" preference would put it out of reach; passing it
//     explicitly keeps Auto-Type working if VeraCrypt ever reads its preferences with /q.
//     Without Auto-Type nothing is passed and VeraCrypt decides (currently: normal desktop).
//   Linux/macOS (src/Main/CommandLineInterface.cpp):
//     --background-task  [--mount-options=ro,ts]  [--explore]  [--truecrypt] or ([--pim=N]
//     [--hash=KDF])  [--slot=N]  [--keyfiles=A,B]  VOLUME
//     No label, "removable" or beep options on Linux; the drive letter A..Z becomes slot 1..26.
//
// VERSION CHECKS (only when the VeraCrypt version is known, i.e. on Windows, VeraCryptInfo)
//   * TrueCrypt mode and RIPEMD-160: removed in VeraCrypt 1.26.7 -> clear message instead of
//     VeraCrypt's "command line error".
//   * Argon2 (/hash argon2id): VeraCrypt for Windows 1.26.30 or newer (KdfAlgorithms).
//   * /secureDesktop exists since VeraCrypt 1.20 (with the Secure Desktop itself); it is only
//     passed when the version is known (an unknown option makes VeraCrypt stop).
//
// HISTORY
//   1.0.x: PwEntryExtension.ToVeraCryptArguments joined the raw entry strings into one command
//          line string (quotes were only added around values with spaces, so a quotation mark in
//          a path or label could change the command line; on Linux each key file was passed as
//          its own -k option and VeraCrypt used only one of them).
//   2.0.0 (2026-11): this class; same options and order of meaning as 1.0.x, plus the version
//          checks, the Argon2 / BLAKE2s KDF choices, and "/secureDesktop n" with Auto-Type.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    internal static class VeraCryptArguments
    {
        /// <summary>VeraCrypt 1.26.7 removed TrueCrypt mode and RIPEMD-160.</summary>
        public static readonly Version LegacyRemovedIn = new Version(1, 26, 7);

        /// <summary>VeraCrypt 1.20 added the Secure Desktop and the /secureDesktop option.</summary>
        public static readonly Version SecureDesktopOptionSince = new Version(1, 20);

        /// <summary>VeraCrypt for Windows accepts labels of at most 32 characters.</summary>
        public const int MaxLabelLength = 32;

        /// <param name="settings">The entry's settings.</param>
        /// <param name="volume">Volume path already resolved by <see cref="PathResolver"/>.</param>
        /// <param name="keyFiles">Key file paths already resolved by <see cref="PathResolver"/>.</param>
        /// <param name="platform">Platform VeraCrypt runs on.</param>
        /// <param name="veraCryptVersion">Installed VeraCrypt version, or null if unknown.</param>
        /// <param name="autoType">True if the plugin will type the password into VeraCrypt.</param>
        /// <exception cref="MountConfigurationException">A setting cannot be used; the message says why.</exception>
        public static List<string> Build(MountSettings settings, string volume, IList<string> keyFiles,
            TargetPlatform platform, Version veraCryptVersion, bool autoType = false)
        {
            if (settings == null)
            {
                throw new ArgumentNullException("settings");
            }

            if (string.IsNullOrEmpty(volume))
            {
                throw new MountConfigurationException(LanguageTexts.NoVolumeSpecified);
            }

            bool newVeraCrypt = veraCryptVersion != null && veraCryptVersion >= LegacyRemovedIn;
            if (settings.TrueCryptMode && newVeraCrypt)
            {
                throw new MountConfigurationException(string.Format(CultureInfo.InvariantCulture,
                    LanguageTexts.TrueCryptModeRemoved, veraCryptVersion));
            }

            KdfChoice kdf = null;
            int pim = 0;
            if (!settings.TrueCryptMode)
            {
                kdf = KdfAlgorithms.FromStoredValue(settings.Kdf);
                if (kdf == null)
                {
                    throw new MountConfigurationException(LanguageTexts.UnknownKdf + settings.Kdf);
                }

                if (kdf.RemovedInVeraCrypt126 && newVeraCrypt)
                {
                    throw new MountConfigurationException(string.Format(CultureInfo.InvariantCulture,
                        LanguageTexts.KdfRemoved, kdf.StoredValue, veraCryptVersion));
                }

                if (platform == TargetPlatform.Windows && kdf.MinimumWindowsVersion != null &&
                    veraCryptVersion != null && veraCryptVersion < kdf.MinimumWindowsVersion)
                {
                    throw new MountConfigurationException(string.Format(CultureInfo.InvariantCulture,
                        LanguageTexts.KdfNeedsNewerVeraCrypt, kdf.DisplayName, kdf.MinimumWindowsVersion,
                        veraCryptVersion));
                }

                if (!string.IsNullOrEmpty(settings.Pim) && !MountSettings.TryParsePim(settings.Pim, out pim))
                {
                    throw new MountConfigurationException(LanguageTexts.InvalidPim);
                }
            }

            string label = settings.Label ?? string.Empty;
            if (platform == TargetPlatform.Windows && !IsValidLabel(label))
            {
                throw new MountConfigurationException(LanguageTexts.InvalidLabel);
            }

            string driveLetter = MountSettings.NormalizeDriveLetter(settings.DriveLetter);
            CheckValue(volume);
            if (keyFiles != null)
            {
                foreach (string keyFile in keyFiles)
                {
                    CheckValue(keyFile);
                }
            }

            bool normalDesktop = autoType && veraCryptVersion != null && veraCryptVersion >= SecureDesktopOptionSince;
            return platform == TargetPlatform.Windows
                ? BuildWindows(settings, volume, keyFiles, kdf, pim, label, driveLetter, normalDesktop)
                : BuildUnix(settings, volume, keyFiles, kdf, pim, driveLetter);
        }

        /// <summary>
        /// A label VeraCrypt for Windows accepts and that cannot break the command line: empty, or at
        /// most 32 characters without quotation marks and control characters.
        /// </summary>
        public static bool IsValidLabel(string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return true;
            }

            return label.Length <= MaxLabelLength &&
                   label.IndexOf('"') < 0 &&
                   !CommandLine.ContainsControlCharacters(label);
        }

        private static List<string> BuildWindows(MountSettings s, string volume, IList<string> keyFiles,
            KdfChoice kdf, int pim, string label, string driveLetter, bool normalDesktop)
        {
            var args = new List<string>();

            args.Add("/q");
            args.Add("background");

            if (normalDesktop)
            {
                args.Add("/secureDesktop"); // see the file header: Auto-Type needs the normal desktop
                args.Add("n");
            }

            if (s.Beep)
            {
                args.Add("/beep");
            }

            if (s.ReadOnly)
            {
                args.Add("/m");
                args.Add("ro");
            }

            if (s.Timestamp)
            {
                args.Add("/m");
                args.Add("ts");
            }

            if (s.Removable)
            {
                args.Add("/m");
                args.Add("rm");
            }

            if (label.Length > 0)
            {
                args.Add("/m");
                args.Add("label=" + label); // one argument; CommandLine quotes it if it has spaces
            }

            if (s.Explore)
            {
                args.Add("/explore");
            }

            if (s.TrueCryptMode)
            {
                args.Add("/tc");
            }
            else
            {
                if (pim > 0)
                {
                    args.Add("/pim");
                    args.Add(pim.ToString(CultureInfo.InvariantCulture)); // never a leading 0 (octal in VeraCrypt)
                }

                string hash = kdf == null ? null : kdf.ArgumentFor(TargetPlatform.Windows);
                if (!string.IsNullOrEmpty(hash))
                {
                    args.Add("/hash");
                    args.Add(hash);
                }
            }

            if (driveLetter.Length == 1)
            {
                args.Add("/letter");
                args.Add(driveLetter);
            }

            args.Add("/v");
            args.Add(volume);

            if (keyFiles != null)
            {
                foreach (string keyFile in keyFiles)
                {
                    args.Add("/k");
                    args.Add(keyFile);
                }
            }

            return args;
        }

        private static List<string> BuildUnix(MountSettings s, string volume, IList<string> keyFiles,
            KdfChoice kdf, int pim, string driveLetter)
        {
            var args = new List<string>();

            args.Add("--background-task"); // keeps VeraCrypt's tray icon, like the Windows version

            var mountOptions = new List<string>();
            if (s.ReadOnly)
            {
                mountOptions.Add("ro");
            }

            if (s.Timestamp)
            {
                mountOptions.Add("ts");
            }

            if (mountOptions.Count > 0)
            {
                args.Add("--mount-options=" + string.Join(",", mountOptions.ToArray()));
            }

            if (s.Explore)
            {
                args.Add("--explore");
            }

            if (s.TrueCryptMode)
            {
                args.Add("--truecrypt");
            }
            else
            {
                if (pim > 0)
                {
                    args.Add("--pim=" + pim.ToString(CultureInfo.InvariantCulture));
                }

                string hash = kdf == null ? null : kdf.ArgumentFor(TargetPlatform.Unix);
                if (!string.IsNullOrEmpty(hash))
                {
                    args.Add("--hash=" + hash);
                }
            }

            if (driveLetter.Length == 1)
            {
                int slot = driveLetter[0] - 'A' + 1; // A = slot 1 ... Z = slot 26
                args.Add("--slot=" + slot.ToString(CultureInfo.InvariantCulture));
            }

            if (keyFiles != null && keyFiles.Count > 0)
            {
                // ONE comma separated list; a comma inside a name is written as ",,".
                var sb = new StringBuilder();
                foreach (string keyFile in keyFiles)
                {
                    if (sb.Length > 0)
                    {
                        sb.Append(',');
                    }

                    sb.Append(keyFile.Replace(",", ",,"));
                }

                args.Add("--keyfiles=" + sb);
            }

            // The volume is the last, positional argument. Resolved paths are absolute, so it can
            // never start with '-' and be taken for an option.
            args.Add(volume);
            return args;
        }

        private static void CheckValue(string value)
        {
            if (string.IsNullOrEmpty(value) || CommandLine.ContainsControlCharacters(value))
            {
                throw new MountConfigurationException(LanguageTexts.InvalidPath + value);
            }
        }
    }
}
