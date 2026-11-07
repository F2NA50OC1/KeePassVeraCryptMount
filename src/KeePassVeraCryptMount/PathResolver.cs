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
// PathResolver.cs - volume and key file texts of an entry -> checked, absolute paths
// -------------------------------------------------------------------------------------------------
// RULES
//   * Quotes around a path (some users added them for 1.0.x) are removed.
//   * Placeholders: KeePass path placeholders ({DB_DIR}, {APPDIR}, ...) and %ENVIRONMENT%
//     variables (MountController.ExpandPlaceholders); on Linux/macOS also "~/".
//   * Relative paths are relative to the KeePass program folder ({APPDIR}). 1.0.x used the current
//     folder, which changes when a file dialog is used. A container next to the database:
//     {DB_DIR}\container.hc.
//   * Must exist: volume files, key files, key file folders. Not checked: Windows volume IDs
//     ("ID:" + 64 hex digits, Windows only), device paths (\Device\..., \\?\..., \\.\...,
//     /dev/...), security token and EMV key files (token://..., emv://...).
//   * Refused: control characters; quotation marks on Windows (not allowed in Windows paths).
//   A problem becomes a MountConfigurationException whose message names the path; VeraCrypt is
//   not started then.
// The file system and the placeholder engine are passed in, so the tests run without KeePass.
//
// HISTORY
//   1.0.x: placeholders without the database ({DB_DIR} did not work), relative to the current
//          folder, key files not checked (PwEntryExtension).
//   2.0.0 (2026-11): this class.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text.RegularExpressions;

    /// <summary>A problem the user has to fix in the entry's mount settings.</summary>
    internal sealed class MountConfigurationException : Exception
    {
        public MountConfigurationException(string message)
            : base(message)
        {
        }
    }

    /// <summary>
    /// Turns the volume and key file texts stored in the entry into absolute paths:
    /// expands KeePass placeholders such as {DB_DIR} and %ENVVARS%, resolves relative paths
    /// against the KeePass program folder (the same folder as {APPDIR}; the old version used the
    /// current working directory, which KeePass changes when file dialogs are used) and checks
    /// that the files exist. The file system and placeholder engine are injected for testing.
    /// </summary>
    internal sealed class PathResolver
    {
        private static readonly Regex VolumeIdRegex = new Regex("^ID:[0-9A-Fa-f]{64}$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private readonly TargetPlatform platform;
        private readonly string baseDirectory;
        private readonly Func<string, string> expandPlaceholders;
        private readonly Func<string, bool> fileExists;
        private readonly Func<string, bool> directoryExists;
        private readonly Func<string, string> getFullPath;

        /// <summary>Resolver for the platform KeePass is running on, using the real file system.</summary>
        public PathResolver(TargetPlatform platform, string baseDirectory, Func<string, string> expandPlaceholders)
            : this(platform, baseDirectory, expandPlaceholders, File.Exists, Directory.Exists, Path.GetFullPath)
        {
        }

        public PathResolver(TargetPlatform platform, string baseDirectory, Func<string, string> expandPlaceholders,
            Func<string, bool> fileExists, Func<string, bool> directoryExists, Func<string, string> getFullPath)
        {
            this.platform = platform;
            this.baseDirectory = baseDirectory ?? string.Empty;
            this.expandPlaceholders = expandPlaceholders ?? (s => s);
            this.fileExists = fileExists;
            this.directoryExists = directoryExists;
            this.getFullPath = getFullPath;
        }

        public string ResolveVolume(string stored)
        {
            string value = StripQuotes(stored);
            if (value.Length == 0)
            {
                throw new MountConfigurationException(LanguageTexts.NoVolumeSpecified);
            }

            if (value.StartsWith("ID:", StringComparison.OrdinalIgnoreCase))
            {
                if (this.platform != TargetPlatform.Windows)
                {
                    throw new MountConfigurationException(LanguageTexts.VolumeIdWindowsOnly);
                }

                if (!VolumeIdRegex.IsMatch(value))
                {
                    throw new MountConfigurationException(LanguageTexts.VolumeIdInvalid + value);
                }

                return "ID:" + value.Substring(3).ToUpperInvariant();
            }

            string path = this.Expand(value);
            if (this.IsDevicePath(path))
            {
                CheckCharacters(path);
                return path;
            }

            path = this.MakeAbsolute(path);
            CheckCharacters(path);
            if (!this.fileExists(path))
            {
                throw new MountConfigurationException(LanguageTexts.VolumeNotFound + path);
            }

            return path;
        }

        public List<string> ResolveKeyFiles(string stored)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(stored))
            {
                return result;
            }

            foreach (string part in stored.Split(';'))
            {
                string value = StripQuotes(part);
                if (value.Length == 0)
                {
                    continue;
                }

                // Security token / EMV key files are not files on disk.
                if (value.StartsWith("token://", StringComparison.OrdinalIgnoreCase) ||
                    value.StartsWith("emv://", StringComparison.OrdinalIgnoreCase))
                {
                    CheckCharacters(value);
                    result.Add(value);
                    continue;
                }

                string path = this.MakeAbsolute(this.Expand(value));
                CheckCharacters(path);
                if (!this.fileExists(path) && !this.directoryExists(path))
                {
                    throw new MountConfigurationException(LanguageTexts.KeyFileNotFound + path);
                }

                result.Add(path);
            }

            return result;
        }

        private static string StripQuotes(string text)
        {
            string s = (text ?? string.Empty).Trim();
            if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
            {
                s = s.Substring(1, s.Length - 2).Trim();
            }

            return s;
        }

        private string Expand(string value)
        {
            string expanded = (this.expandPlaceholders(value) ?? string.Empty).Trim();

            // "~/..." on Linux/macOS means the home folder (never worked in the old version).
            if (this.platform == TargetPlatform.Unix && expanded.StartsWith("~/", StringComparison.Ordinal))
            {
                string home = Environment.GetEnvironmentVariable("HOME");
                if (!string.IsNullOrEmpty(home))
                {
                    expanded = home.TrimEnd('/') + expanded.Substring(1);
                }
            }

            return expanded;
        }

        private bool IsDevicePath(string path)
        {
            if (this.platform == TargetPlatform.Windows)
            {
                return path.StartsWith(@"\Device\", StringComparison.OrdinalIgnoreCase) ||
                       path.StartsWith(@"\\?\", StringComparison.Ordinal) ||
                       path.StartsWith(@"\\.\", StringComparison.Ordinal);
            }

            return path.StartsWith("/dev/", StringComparison.Ordinal);
        }

        private string MakeAbsolute(string path)
        {
            try
            {
                string p = path;
                if (!this.IsRooted(p))
                {
                    char separator = this.platform == TargetPlatform.Windows ? '\\' : '/';
                    p = this.baseDirectory.TrimEnd('\\', '/') + separator + p;
                }

                return this.getFullPath(p);
            }
            catch (Exception ex)
            {
                if (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException ||
                    ex is System.Security.SecurityException)
                {
                    throw new MountConfigurationException(LanguageTexts.InvalidPath + path);
                }

                throw;
            }
        }

        private bool IsRooted(string path)
        {
            if (this.platform == TargetPlatform.Windows)
            {
                // "C:\x" or "\\server\share"; "C:x" and "\x" are relative to a current drive/dir.
                return (path.Length >= 3 && char.IsLetter(path[0]) && path[1] == ':' && (path[2] == '\\' || path[2] == '/')) ||
                       path.StartsWith(@"\\", StringComparison.Ordinal);
            }

            return path.StartsWith("/", StringComparison.Ordinal);
        }

        private void CheckCharacters(string path)
        {
            if (CommandLine.ContainsControlCharacters(path) ||
                (this.platform == TargetPlatform.Windows && path.IndexOf('"') >= 0))
            {
                throw new MountConfigurationException(LanguageTexts.InvalidPath + path);
            }
        }
    }
}
