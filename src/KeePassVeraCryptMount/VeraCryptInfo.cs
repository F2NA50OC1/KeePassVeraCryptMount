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
// VeraCryptInfo.cs - where VeraCrypt is installed, and which version it is
// -------------------------------------------------------------------------------------------------
// FIND (only when the user clicks the "find" button in the options dialog; nothing is started):
//   Windows: the file type "VeraCryptVolume" registered by VeraCrypt's installer
//   (HKLM\SOFTWARE\Classes), %ProgramW6432%, %ProgramFiles%, %ProgramFiles(x86)% + \VeraCrypt\
//   VeraCrypt.exe, then the per-user file association. Linux/macOS: every folder of PATH,
//   /usr/bin, /usr/local/bin, /Applications/VeraCrypt.app (macOS).
// VERSION (Windows only): the file version of VeraCrypt.exe (e.g. 1.26.31.0), used for the checks
//   in VeraCryptArguments. If it is unknown (Linux, a copy without version information) the
//   checks are skipped and VeraCrypt itself reports options it does not support.
//
// HISTORY
//   1.0.x: Windows registry only (a regular expression over the command); Linux ran
//          "/bin/sh -c 'which veracrypt'".
//   2.0.0 (2026-11): rewritten.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    using Microsoft.Win32;

    /// <summary>Finds the VeraCrypt program and its version.</summary>
    internal static class VeraCryptInfo
    {
        public static bool ExecutableExists(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            try
            {
                return Path.IsPathRooted(filePath) && File.Exists(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("KeePassVeraCryptMount: cannot check executable: " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// Looks for an installed VeraCrypt. Windows: machine-wide registration and the standard
        /// installation folders first, then the per-user file association. Linux/macOS: the PATH
        /// and the usual folders (the old version ran "/bin/sh -c 'which veracrypt'").
        /// Returns an empty string if nothing was found.
        /// </summary>
        public static string FindExecutable()
        {
            try
            {
                foreach (string candidate in Platform.IsUnix ? UnixCandidates() : WindowsCandidates())
                {
                    if (ExecutableExists(candidate))
                    {
                        return candidate;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("KeePassVeraCryptMount: executable search failed: " + ex.Message);
            }

            return string.Empty;
        }

        /// <summary>The VeraCrypt version on Windows (from the file version), otherwise null.</summary>
        public static Version GetVersion(string executable)
        {
            if (Platform.IsUnix || !ExecutableExists(executable))
            {
                return null;
            }

            try
            {
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(executable);
                if (info.FileMajorPart <= 0)
                {
                    return null;
                }

                return new Version(info.FileMajorPart, info.FileMinorPart, Math.Max(0, info.FileBuildPart));
            }
            catch (Exception ex)
            {
                Debug.WriteLine("KeePassVeraCryptMount: cannot read VeraCrypt version: " + ex.Message);
                return null;
            }
        }

        /// <summary>
        /// Extracts the program path from a registry command such as
        /// "C:\Program Files\VeraCrypt\VeraCrypt.exe" /v "%1".
        /// </summary>
        public static string ExecutableFromCommand(string command)
        {
            if (string.IsNullOrEmpty(command))
            {
                return string.Empty;
            }

            string c = command.Trim();
            if (c.StartsWith("\"", StringComparison.Ordinal))
            {
                int end = c.IndexOf('"', 1);
                return end > 1 ? c.Substring(1, end - 1) : string.Empty;
            }

            int exe = c.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
            return exe > 0 ? c.Substring(0, exe + 4) : string.Empty;
        }

        private static IEnumerable<string> WindowsCandidates()
        {
            const string ClassKey = @"SOFTWARE\Classes\VeraCryptVolume\Shell\open\command";

            yield return ExecutableFromCommand(ReadRegistryDefault(RegistryHive.LocalMachine, ClassKey));

            foreach (string variable in new[] { "ProgramW6432", "ProgramFiles", "ProgramFiles(x86)" })
            {
                string folder = Environment.GetEnvironmentVariable(variable);
                if (!string.IsNullOrEmpty(folder))
                {
                    yield return Path.Combine(Path.Combine(folder, "VeraCrypt"), "VeraCrypt.exe");
                }
            }

            // Per-user association (merged view), like the old version did.
            yield return ExecutableFromCommand(ReadRegistryDefault(RegistryHive.ClassesRoot, @"VeraCryptVolume\Shell\open\command"));
        }

        private static IEnumerable<string> UnixCandidates()
        {
            string path = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (string dir in path.Split(':'))
            {
                if (dir.Length > 0 && dir.StartsWith("/", StringComparison.Ordinal))
                {
                    yield return Path.Combine(dir, "veracrypt");
                }
            }

            yield return "/usr/bin/veracrypt";
            yield return "/usr/local/bin/veracrypt";
            yield return "/Applications/VeraCrypt.app/Contents/MacOS/VeraCrypt";
        }

        private static string ReadRegistryDefault(RegistryHive hive, string subKey)
        {
            try
            {
                RegistryView view = Environment.Is64BitOperatingSystem ? RegistryView.Registry64 : RegistryView.Default;
                using (RegistryKey root = RegistryKey.OpenBaseKey(hive, view))
                using (RegistryKey key = root.OpenSubKey(subKey, false))
                {
                    object value = key != null ? key.GetValue(string.Empty) : null;
                    return value as string ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("KeePassVeraCryptMount: registry read failed: " + ex.Message);
                return string.Empty;
            }
        }
    }
}
