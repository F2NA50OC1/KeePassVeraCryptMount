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
// CommandLine.cs - quoting: argument list -> the one string ProcessStartInfo.Arguments takes
// -------------------------------------------------------------------------------------------------
// VeraCrypt must receive exactly the arguments of the list, whatever characters a path or label
// contains (spaces, quotes, trailing backslashes, ; & | $ ...). No shell is involved on any platform
// (UseShellExecute = false), so nothing is expanded or interpreted on the way.
//   Windows:     rules of CommandLineToArgvW / the Microsoft C runtime, which VeraCrypt for Windows
//                uses (src/Mount/Mount.c, ExtractCommandLine). Tested against an independent parser
//                with random strings (test WindowsQuotingRandom).
//   Linux/macOS: Mono splits ProcessStartInfo.Arguments with shell-like rules (double quotes,
//                backslash escapes). Tested by starting a real process (test UnixQuotingRoundTrip).
// Control characters (line breaks, tabs, ...) are refused before quoting (VeraCryptArguments,
// PathResolver): they have no use in paths or labels.
//
// HISTORY
//   1.0.x: values were concatenated; quotes were only added around values with spaces.
//   2.0.0 (2026-11): this file.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    internal enum TargetPlatform
    {
        Windows,
        Unix,
    }

    /// <summary>
    /// Turns a list of arguments into the single command line string that
    /// <see cref="System.Diagnostics.ProcessStartInfo.Arguments"/> expects, so that the
    /// started program receives exactly these arguments, whatever characters they contain.
    /// The old plugin concatenated strings and only added quotes around values containing a
    /// space, which broke (or could be abused) with quotes, trailing backslashes, etc.
    /// </summary>
    internal static class CommandLine
    {
        public static string Join(IList<string> arguments, TargetPlatform platform)
        {
            if (arguments == null)
            {
                throw new ArgumentNullException("arguments");
            }

            var sb = new StringBuilder();
            foreach (string argument in arguments)
            {
                if (argument == null)
                {
                    throw new ArgumentException("Null argument.", "arguments");
                }

                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(platform == TargetPlatform.Windows ? QuoteWindows(argument) : QuoteUnix(argument));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Quotes one argument following the rules of CommandLineToArgvW / the Microsoft C runtime,
        /// which VeraCrypt for Windows uses to split its command line.
        /// </summary>
        public static string QuoteWindows(string argument)
        {
            if (argument.Length > 0 && argument.IndexOfAny(WindowsSpecialChars) < 0)
            {
                return argument;
            }

            var sb = new StringBuilder(argument.Length + 8);
            sb.Append('"');
            for (int i = 0; ; ++i)
            {
                int backslashes = 0;
                while (i < argument.Length && argument[i] == '\\')
                {
                    ++i;
                    ++backslashes;
                }

                if (i == argument.Length)
                {
                    // Backslashes before the closing quote must be doubled.
                    sb.Append('\\', backslashes * 2);
                    break;
                }

                if (argument[i] == '"')
                {
                    // Backslashes before a literal quote are doubled, and the quote is escaped.
                    sb.Append('\\', (backslashes * 2) + 1);
                    sb.Append('"');
                }
                else
                {
                    // Backslashes not followed by a quote are literal.
                    sb.Append('\\', backslashes);
                    sb.Append(argument[i]);
                }
            }

            sb.Append('"');
            return sb.ToString();
        }

        /// <summary>
        /// Quotes one argument for Mono on Linux/macOS, which splits
        /// ProcessStartInfo.Arguments with shell-like rules (no shell is involved, no variable
        /// expansion happens). Inside double quotes only backslash and double quote need escaping.
        /// </summary>
        public static string QuoteUnix(string argument)
        {
            if (argument.Length > 0 && IsUnixSafe(argument))
            {
                return argument;
            }

            var sb = new StringBuilder(argument.Length + 8);
            sb.Append('"');
            foreach (char c in argument)
            {
                if (c == '\\' || c == '"')
                {
                    sb.Append('\\');
                }

                sb.Append(c);
            }

            sb.Append('"');
            return sb.ToString();
        }

        /// <summary>True if the text contains characters that must never reach a command line.</summary>
        public static bool ContainsControlCharacters(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            foreach (char c in text)
            {
                if (char.IsControl(c))
                {
                    return true;
                }
            }

            return false;
        }

        private static readonly char[] WindowsSpecialChars = new[] { ' ', '\t', '\n', '\v', '"' };

        private static bool IsUnixSafe(string argument)
        {
            foreach (char c in argument)
            {
                bool safe = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') ||
                            c == '-' || c == '_' || c == '.' || c == '/' || c == '=' || c == ',' ||
                            c == ':' || c == '+' || c == '@' || c == '%';
                if (!safe)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
