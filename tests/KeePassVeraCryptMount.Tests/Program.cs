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

// Tests for the KeePassVeraCryptMount plugin logic (command line building, quoting, settings,
// path handling). Run:  mono KeePassVeraCryptMount.Tests.exe        (Linux)
//                       KeePassVeraCryptMount.Tests.exe             (Windows)
// Extra mode used by the end-to-end test script:
//                       ... makedb <file.kdbx> <master password> <entry password> <volume> [autotype=0|1]
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

using KeePassLib;
using KeePassLib.Keys;
using KeePassLib.Security;
using KeePassLib.Serialization;

namespace KeePassVeraCryptMount.Tests
{
    internal static class Program
    {
        private static int failures;
        private static int passed;

        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "makedb")
            {
                return MakeTestDatabase(args);
            }

            Run("Windows quoting: fixed cases", WindowsQuotingFixed);
            Run("Windows quoting: random round trip (CommandLineToArgvW rules)", WindowsQuotingRandom);
            Run("Unix quoting: real round trip through Mono's process start", UnixQuotingRoundTrip);
            Run("Windows arguments: all options", WindowsAllOptions);
            Run("Windows arguments: minimal", WindowsMinimal);
            Run("Unix arguments: all options", UnixAllOptions);
            Run("TrueCrypt mode and RIPEMD-160 vs VeraCrypt version", LegacyOptions);
            Run("Injection attempts are rejected or neutralised", InjectionAttempts);
            Run("Old entries (version 1.0.7) are read correctly", OldEntryCompatibility);
            Run("Writing settings: change detection, no Auto-Type changes", WriteSettings);
            Run("PIM parsing", PimParsing);
            Run("Drive letter normalisation", DriveLetters);
            Run("Path resolution (Windows rules)", PathsWindows);
            Run("Path resolution (Unix rules)", PathsUnix);
            Run("KDF values are accepted by VeraCrypt 1.26.31", KdfValues);
            Run("Registry command parsing", RegistryCommand);
            Run("Argon2 needs VeraCrypt 1.26.30+ on Windows", Argon2Version);
            Run("KDF list = VeraCrypt 1.26.31's list (names, order)", KdfListMatchesVeraCrypt);
            Run("Volume label rules (Windows only)", LabelRules);
            Run("Normal desktop requested only for Auto-Type (Windows, VeraCrypt 1.20+)", SecureDesktopArgument);
            Run("Timeout clamping", Timeouts);
            Run("Title filter used while typing", TitleFilter);

            Console.WriteLine();
            Console.WriteLine("Passed: {0}   Failed: {1}", passed, failures);
            return failures == 0 ? 0 : 1;
        }

        // ------------------------------------------------------------------ helpers

        private static void Run(string name, Action test)
        {
            try
            {
                test();
                passed++;
                Console.WriteLine("PASS  " + name);
            }
            catch (Exception ex)
            {
                failures++;
                Console.WriteLine("FAIL  " + name);
                Console.WriteLine("      " + ex.Message.Replace("\n", "\n      "));
            }
        }

        private static void Check(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception(message);
            }
        }

        private static void Equal(string expected, string actual, string what)
        {
            if (expected != actual)
            {
                throw new Exception(what + "\n expected: " + expected + "\n actual:   " + actual);
            }
        }

        private static void SameList(IList<string> expected, IList<string> actual, string what)
        {
            Equal(Show(expected), Show(actual), what);
        }

        private static string Show(IEnumerable<string> list)
        {
            return "[" + string.Join(" | ", list.Select(s => "<" + s + ">")) + "]";
        }

        private static void Throws<T>(Action action, string what) where T : Exception
        {
            try
            {
                action();
            }
            catch (T)
            {
                return;
            }

            throw new Exception("expected " + typeof(T).Name + ": " + what);
        }

        /// <summary>
        /// Independent implementation of the Microsoft C runtime / CommandLineToArgvW splitting
        /// rules (the parser VeraCrypt for Windows uses), to check the quoting code.
        /// </summary>
        private static List<string> ParseWindowsCommandLine(string cmd)
        {
            var result = new List<string>();
            int i = 0;
            int n = cmd.Length;
            while (true)
            {
                while (i < n && (cmd[i] == ' ' || cmd[i] == '\t'))
                {
                    i++;
                }

                if (i >= n)
                {
                    break;
                }

                var sb = new StringBuilder();
                bool inQuotes = false;
                while (i < n)
                {
                    char c = cmd[i];
                    if (!inQuotes && (c == ' ' || c == '\t'))
                    {
                        break;
                    }

                    if (c == '\\')
                    {
                        int backslashes = 0;
                        while (i < n && cmd[i] == '\\')
                        {
                            backslashes++;
                            i++;
                        }

                        if (i < n && cmd[i] == '"')
                        {
                            sb.Append('\\', backslashes / 2);
                            if (backslashes % 2 == 1)
                            {
                                sb.Append('"');
                                i++;
                            }
                        }
                        else
                        {
                            sb.Append('\\', backslashes);
                        }

                        continue;
                    }

                    if (c == '"')
                    {
                        if (inQuotes && i + 1 < n && cmd[i + 1] == '"')
                        {
                            sb.Append('"');
                            i += 2;
                            continue;
                        }

                        inQuotes = !inQuotes;
                        i++;
                        continue;
                    }

                    sb.Append(c);
                    i++;
                }

                result.Add(sb.ToString());
            }

            return result;
        }

        private static readonly string[] TrickyArguments =
        {
            "plain", "with space", "", " ", "trailing\\", "trailing space\\", "C:\\Program Files\\VeraCrypt\\",
            "quote\"inside", "\"", "\\\"", "\\\\\"", "a\\\\b", "tab\there", "ünïcødé €", "it's", "$HOME", "`cmd`",
            "a;b&c|d>e<f", "--password=x", "/p", "label=My Data", "-k", "100%", "!bang", "*?[]", "~user", "#hash",
            "\\\\server\\share\\v.hc",
        };

        // ------------------------------------------------------------------ quoting

        private static void WindowsQuotingFixed()
        {
            Equal("plain", CommandLine.QuoteWindows("plain"), "plain");
            Equal("\"with space\"", CommandLine.QuoteWindows("with space"), "space");
            Equal("\"\"", CommandLine.QuoteWindows(""), "empty");
            Equal("\"C:\\Program Files\\VeraCrypt\\\\\"", CommandLine.QuoteWindows("C:\\Program Files\\VeraCrypt\\"), "trailing backslash");
            Equal("\"a\\\"b\"", CommandLine.QuoteWindows("a\"b"), "quote");
            Equal("C:\\x\\y", CommandLine.QuoteWindows("C:\\x\\y"), "backslashes alone stay");

            List<string> parsed = ParseWindowsCommandLine(CommandLine.Join(TrickyArguments, TargetPlatform.Windows));
            SameList(TrickyArguments, parsed, "tricky arguments");
        }

        private static void WindowsQuotingRandom()
        {
            var random = new Random(12345);
            const string alphabet = "ab \t\"\\/:é'";
            for (int round = 0; round < 3000; round++)
            {
                var args = new List<string>();
                int count = random.Next(1, 5);
                for (int a = 0; a < count; a++)
                {
                    int length = random.Next(0, 9);
                    var sb = new StringBuilder();
                    for (int k = 0; k < length; k++)
                    {
                        sb.Append(alphabet[random.Next(alphabet.Length)]);
                    }

                    args.Add(sb.ToString());
                }

                string line = CommandLine.Join(args, TargetPlatform.Windows);
                SameList(args, ParseWindowsCommandLine(line), "round " + round + ", command line: " + line);
            }
        }

        private static void UnixQuotingRoundTrip()
        {
            if (Environment.OSVersion.Platform != PlatformID.Unix)
            {
                Console.WriteLine("      (skipped: not on Unix)");
                return;
            }

            string script = Path.Combine(Path.GetTempPath(), "kpvcm_printargs.py");
            File.WriteAllText(script, "import sys\nfor a in sys.argv[1:]:\n    sys.stdout.write(a.encode('utf-8').hex() + '\\n')\n");

            var args = new List<string>(TrickyArguments.Where(a => a.IndexOf('\t') < 0));
            args.Add("--keyfiles=/tmp/a b,,c,/tmp/d");
            args.Add("/tmp/a\" --password=x \"b.hc");

            var psi = new ProcessStartInfo("python3", "-I " + CommandLine.QuoteUnix(script) + " " + CommandLine.Join(args, TargetPlatform.Unix));
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            using (Process p = Process.Start(psi))
            {
                string output = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                var received = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(h => Encoding.UTF8.GetString(Enumerable.Range(0, h.Length / 2).Select(x => Convert.ToByte(h.Substring(x * 2, 2), 16)).ToArray()))
                    .ToList();

                // Empty arguments produce an empty line, removed above: compare without them.
                SameList(args.Where(a => a.Length > 0).ToList(), received.Where(a => a.Length > 0).ToList(), "arguments received by the child process");
                Check(received.Count(a => a.Length > 0) == args.Count(a => a.Length > 0), "argument count");
            }
        }

        // ------------------------------------------------------------------ argument building

        private static MountSettings AllOptions()
        {
            var s = new MountSettings();
            s.Volume = "ignored, resolved separately";
            s.Beep = true;
            s.ReadOnly = true;
            s.Timestamp = true;
            s.Removable = true;
            s.Label = "My Data";
            s.Explore = true;
            s.Pim = "485";
            s.Kdf = "HMAC-SHA-512";
            s.DriveLetter = "X";
            return s;
        }

        private static void WindowsAllOptions()
        {
            List<string> args = VeraCryptArguments.Build(AllOptions(), "C:\\Vol\\a b.hc", new[] { "C:\\k1", "D:\\k 2" },
                TargetPlatform.Windows, new Version(1, 26, 31));
            SameList(new[]
            {
                "/q", "background", "/beep", "/m", "ro", "/m", "ts", "/m", "rm", "/m", "label=My Data", "/explore",
                "/pim", "485", "/hash", "sha512", "/letter", "X", "/v", "C:\\Vol\\a b.hc", "/k", "C:\\k1", "/k", "D:\\k 2",
            }, args, "argument list");

            Equal("/q background /beep /m ro /m ts /m rm /m \"label=My Data\" /explore /pim 485 /hash sha512 /letter X /v \"C:\\Vol\\a b.hc\" /k C:\\k1 /k \"D:\\k 2\"",
                CommandLine.Join(args, TargetPlatform.Windows), "command line");
        }

        private static void WindowsMinimal()
        {
            var s = new MountSettings();
            List<string> args = VeraCryptArguments.Build(s, "ID:" + new string('A', 64), new string[0], TargetPlatform.Windows, null);
            SameList(new[] { "/q", "background", "/v", "ID:" + new string('A', 64) }, args, "minimal");
        }

        private static void UnixAllOptions()
        {
            List<string> args = VeraCryptArguments.Build(AllOptions(), "/vol/a b.hc", new[] { "/k/a,b", "/k/c" },
                TargetPlatform.Unix, null);
            SameList(new[]
            {
                "--background-task", "--mount-options=ro,ts", "--explore", "--pim=485", "--hash=sha512", "--slot=24",
                "--keyfiles=/k/a,,b,/k/c", "/vol/a b.hc",
            }, args, "argument list (no beep / removable / label on Linux; one keyfile list)");
        }

        private static void LegacyOptions()
        {
            var tc = new MountSettings();
            tc.TrueCryptMode = true;
            tc.Pim = "77";
            tc.Kdf = "HMAC-SHA-512";
            Throws<MountConfigurationException>(() => VeraCryptArguments.Build(tc, "C:\\v", null, TargetPlatform.Windows, new Version(1, 26, 31)), "TC mode on 1.26.31");
            Throws<MountConfigurationException>(() => VeraCryptArguments.Build(tc, "C:\\v", null, TargetPlatform.Windows, new Version(1, 26, 7)), "TC mode on 1.26.7");
            List<string> old = VeraCryptArguments.Build(tc, "C:\\v", null, TargetPlatform.Windows, new Version(1, 25, 9));
            Check(old.Contains("/tc") && !old.Contains("/pim") && !old.Contains("/hash"), "TC mode on 1.25.9: " + Show(old));
            Check(VeraCryptArguments.Build(tc, "C:\\v", null, TargetPlatform.Windows, null).Contains("/tc"), "TC mode, unknown version");

            var ripemd = new MountSettings();
            ripemd.Kdf = "HMAC-RIPEMD-160";
            Throws<MountConfigurationException>(() => VeraCryptArguments.Build(ripemd, "C:\\v", null, TargetPlatform.Windows, new Version(1, 26, 7)), "RIPEMD on 1.26.7");
            List<string> r = VeraCryptArguments.Build(ripemd, "C:\\v", null, TargetPlatform.Windows, new Version(1, 25, 9));
            Check(r.Contains("ripemd160"), "RIPEMD on 1.25.9: " + Show(r));
        }

        private static void InjectionAttempts()
        {
            // Values below would have added a password / extra options with the old string concatenation.
            var s = new MountSettings();
            s.Label = "x\" /p \"secret";
            Throws<MountConfigurationException>(() => VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, null), "quote in label");

            s = new MountSettings();
            s.Pim = "1 /p secret";
            Throws<MountConfigurationException>(() => VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, null), "PIM with text");

            s = new MountSettings();
            s.Kdf = "HMAC-SHA-512 /p secret";
            Throws<MountConfigurationException>(() => VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, null), "KDF with text");

            s = new MountSettings();
            s.DriveLetter = "X /p secret";
            List<string> args = VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, null);
            Check(!args.Contains("/letter") && !args.Contains("/p"), "bad drive letter ignored: " + Show(args));

            var resolver = WindowsResolver(new[] { "C:\\v.hc" });
            Throws<MountConfigurationException>(() => resolver.ResolveVolume("C:\\v.hc\" /p \"secret"), "quote in volume path");
            Throws<MountConfigurationException>(() => resolver.ResolveKeyFiles("C:\\k\n/p secret"), "newline in key file");

            // A volume path with spaces and quotes stays ONE argument on Linux.
            List<string> unix = VeraCryptArguments.Build(new MountSettings(), "/tmp/a\" --password=x \"b.hc", null, TargetPlatform.Unix, null);
            Check(unix.Count == 2 && unix[1] == "/tmp/a\" --password=x \"b.hc", "unix volume as one argument: " + Show(unix));
            Check(CommandLine.Join(unix, TargetPlatform.Unix) == "--background-task \"/tmp/a\\\" --password=x \\\"b.hc\"", "unix quoting");

            // The password itself is never part of the arguments.
            foreach (string a in VeraCryptArguments.Build(AllOptions(), "C:\\v", null, TargetPlatform.Windows, null))
            {
                Check(a != "/p" && a != "/password", "no password option");
            }
        }

        // ------------------------------------------------------------------ settings

        private static PwEntry OldEntry()
        {
            // Exactly what version 1.0.7 wrote into an entry.
            var e = new PwEntry(true, true);
            e.Strings.Set("Password", new ProtectedString(true, "S3cret pass!"));
            e.Strings.Set("veracrypt.enabled", new ProtectedString(false, "True"));
            e.Strings.Set("veracrypt.container", new ProtectedString(false, "C:\\Data\\vault.hc"));
            e.Strings.Set("veracrypt.keyfiles", new ProtectedString(false, "C:\\k1.key;C:\\k2.key;"));
            e.Strings.Set("veracrypt.option.label", new ProtectedString(false, ""));
            e.Strings.Set("veracrypt.drive", new ProtectedString(false, "K:\\"));
            e.Strings.Set("veracrypt.mount.without.dialog", new ProtectedString(false, "True"));
            e.Strings.Set("veracrypt.option.truecryptmode", new ProtectedString(false, "False"));
            e.Strings.Set("veracrypt.option.removable", new ProtectedString(false, "False"));
            e.Strings.Set("veracrypt.option.readonly", new ProtectedString(false, "True"));
            e.Strings.Set("veracrypt.option.timestamp", new ProtectedString(false, "False"));
            e.Strings.Set("veracrypt.option.beep", new ProtectedString(false, "False"));
            e.Strings.Set("veracrypt.option.explorer", new ProtectedString(false, "True"));
            e.Strings.Set("veracrypt.option.askpassword", new ProtectedString(false, "False"));
            e.Strings.Set("veracrypt.option.pkcs5", new ProtectedString(false, "HMAC-SHA-256  "));
            e.Strings.Set("veracrypt.option.pim", new ProtectedString(true, "0123"));
            e.Strings.Set("veracrypt.option.quit", new ProtectedString(false, "True")); // pre-1.0.4 leftover
            e.AutoType.DefaultSequence = "{PASSWORD}{ENTER}";
            e.AutoType.ObfuscationOptions = KeePassLib.Collections.AutoTypeObfuscationOptions.UseClipboard;
            return e;
        }

        private static void OldEntryCompatibility()
        {
            MountSettings s = MountSettings.ReadFrom(OldEntry());
            Check(s.Enabled && s.HideDialog && s.AutoType && s.ReadOnly && s.Explore && !s.Removable, "flags");
            Equal("C:\\Data\\vault.hc", s.Volume, "volume");
            Equal("K", s.DriveLetter, "drive");
            Equal("HMAC-SHA-256", s.Kdf, "kdf (trailing spaces of old versions)");
            Equal("123", s.Pim, "PIM 0123 (VeraCrypt would read 0123 on its command line as octal 83!)");
            Check(KdfAlgorithms.FromStoredValue(s.Kdf).ArgumentFor(TargetPlatform.Windows) == "sha256", "kdf argument");

            var resolver = WindowsResolver(new[] { "C:\\Data\\vault.hc", "C:\\k1.key", "C:\\k2.key" });
            List<string> args = VeraCryptArguments.Build(s, resolver.ResolveVolume(s.Volume), resolver.ResolveKeyFiles(s.KeyFiles),
                TargetPlatform.Windows, new Version(1, 26, 31));
            Equal("/q background /m ro /explore /pim 123 /hash sha256 /letter K /v C:\\Data\\vault.hc /k C:\\k1.key /k C:\\k2.key",
                CommandLine.Join(args, TargetPlatform.Windows), "command line for an old entry");

            Check(MountSettings.IsEnabledFor(OldEntry()) && MountSettings.ShouldHideDialogFor(OldEntry()), "menu helpers");
            Check(!MountSettings.IsEnabledFor(new PwEntry(true, true)) && !MountSettings.IsEnabledFor(null), "menu helpers, plain entry");
        }

        private static void WriteSettings()
        {
            PwEntry e = OldEntry();
            MountSettings s = MountSettings.ReadFrom(e);
            Check(s.WriteTo(e), "first write normalises old values -> changed");
            Check(!e.Strings.Exists("veracrypt.option.quit"), "obsolete field removed");
            Equal("K:\\", e.Strings.ReadSafe("veracrypt.drive"), "drive stored in old format");
            Equal("HMAC-SHA-256", e.Strings.ReadSafe("veracrypt.option.pkcs5"), "kdf stored");
            Check(e.Strings.Get("veracrypt.option.pim").IsProtected, "PIM stays protected");
            Equal("123", e.Strings.ReadSafe("veracrypt.option.pim"), "PIM normalised");
            Check(!MountSettings.ReadFrom(e).WriteTo(e), "second write -> unchanged");

            Equal("{PASSWORD}{ENTER}", e.AutoType.DefaultSequence, "auto-type sequence untouched");
            Check(e.AutoType.ObfuscationOptions == KeePassLib.Collections.AutoTypeObfuscationOptions.UseClipboard, "obfuscation untouched");

            var fresh = new PwEntry(true, true);
            var n = new MountSettings();
            n.Volume = "C:\\v.hc";
            n.Kdf = string.Empty;
            n.Pim = string.Empty;
            Check(n.WriteTo(fresh), "new entry");
            Check(!fresh.Strings.Exists("veracrypt.option.pkcs5") && !fresh.Strings.Exists("veracrypt.option.pim"), "no kdf/pim stored");
            Equal("False", fresh.Strings.ReadSafe("veracrypt.option.askpassword"), "auto-type on by default");
            Equal(string.Empty, fresh.AutoType.DefaultSequence, "new entry: sequence not set by plugin");
        }

        private static void PimParsing()
        {
            int v;
            Check(MountSettings.TryParsePim("1", out v) && v == 1, "1");
            Check(MountSettings.TryParsePim("2147468", out v) && v == 2147468, "max");
            Check(!MountSettings.TryParsePim("2147469", out v), "max + 1");
            Check(!MountSettings.TryParsePim("0", out v), "0 means default, not a PIM");
            Check(!MountSettings.TryParsePim("-5", out v) && !MountSettings.TryParsePim("+5", out v), "signs");
            Check(!MountSettings.TryParsePim(" 5", out v) && !MountSettings.TryParsePim("0x10", out v), "spaces / hex");
            Check(!MountSettings.TryParsePim("12345678", out v), "8 digits");
            Check(MountSettings.TryParsePim("0010", out v) && v == 10, "leading zeros are decimal");
        }

        private static void DriveLetters()
        {
            Equal("X", MountSettings.NormalizeDriveLetter("X:\\"), "X:\\");
            Equal("X", MountSettings.NormalizeDriveLetter("x"), "x");
            Equal("X", MountSettings.NormalizeDriveLetter("X:"), "X:");
            Equal(string.Empty, MountSettings.NormalizeDriveLetter("1:\\"), "digit");
            Equal(string.Empty, MountSettings.NormalizeDriveLetter("XY"), "two letters");
            Equal(string.Empty, MountSettings.NormalizeDriveLetter(""), "empty");
            Equal(string.Empty, MountSettings.NormalizeDriveLetter(null), "null");
        }

        // ------------------------------------------------------------------ paths

        /// <summary>Minimal Windows path normaliser (the test may run on Linux).</summary>
        private static string WindowsFullPath(string path)
        {
            string prefix = path.StartsWith("\\\\") ? "\\\\" : path.Substring(0, 2) + "\\";
            string rest = path.Substring(prefix.Length);
            var parts = new List<string>();
            foreach (string part in rest.Split('\\', '/'))
            {
                if (part.Length == 0 || part == ".")
                {
                    continue;
                }

                if (part == "..")
                {
                    if (parts.Count > 0)
                    {
                        parts.RemoveAt(parts.Count - 1);
                    }

                    continue;
                }

                parts.Add(part);
            }

            return prefix + string.Join("\\", parts);
        }

        private static PathResolver WindowsResolver(IEnumerable<string> existingFiles, IEnumerable<string> existingDirs = null)
        {
            var files = new HashSet<string>(existingFiles, StringComparer.OrdinalIgnoreCase);
            var dirs = new HashSet<string>(existingDirs ?? new string[0], StringComparer.OrdinalIgnoreCase);
            return new PathResolver(TargetPlatform.Windows, "C:\\Program Files\\KeePass Password Safe 2",
                s => s.Replace("{DB_DIR}", "D:\\Safe").Replace("%USERPROFILE%", "C:\\Users\\me"),
                files.Contains, dirs.Contains, WindowsFullPath);
        }

        private static void PathsWindows()
        {
            var r = WindowsResolver(new[]
            {
                "D:\\Safe\\vault.hc", "C:\\Program Files\\KeePass Password Safe 2\\portable.hc", "C:\\Users\\me\\key file.bin",
                "C:\\a b\\v.hc", "C:\\Program Files\\Containers\\up.hc",
            }, new[] { "C:\\Keys" });

            Equal("D:\\Safe\\vault.hc", r.ResolveVolume("{DB_DIR}\\vault.hc"), "placeholder");
            Equal("C:\\Program Files\\KeePass Password Safe 2\\portable.hc", r.ResolveVolume("portable.hc"), "relative to KeePass folder");
            Equal("C:\\Program Files\\Containers\\up.hc", r.ResolveVolume("..\\Containers\\up.hc"), "relative with ..");
            Equal("C:\\a b\\v.hc", r.ResolveVolume("\"C:\\a b\\v.hc\""), "quotes added by old users are removed");
            Equal("ID:" + new string('A', 64), r.ResolveVolume("id:" + new string('a', 64)), "volume ID normalised");
            Equal("\\Device\\Harddisk1\\Partition2", r.ResolveVolume("\\Device\\Harddisk1\\Partition2"), "device path");
            Equal("\\\\?\\Volume{01234567-89ab-cdef-0123-456789abcdef}\\", r.ResolveVolume("\\\\?\\Volume{01234567-89ab-cdef-0123-456789abcdef}\\"), "volume name");
            Throws<MountConfigurationException>(() => r.ResolveVolume("ID:1234"), "short ID");
            Throws<MountConfigurationException>(() => r.ResolveVolume("C:\\missing.hc"), "missing file");
            Throws<MountConfigurationException>(() => r.ResolveVolume("   "), "empty");

            SameList(new[] { "C:\\Users\\me\\key file.bin", "C:\\Keys", "token://slot/1/file/k" },
                r.ResolveKeyFiles("%USERPROFILE%\\key file.bin; C:\\Keys ;;token://slot/1/file/k;"), "key files");
            Throws<MountConfigurationException>(() => r.ResolveKeyFiles("C:\\nokey.bin"), "missing key file");
            Check(r.ResolveKeyFiles("").Count == 0 && r.ResolveKeyFiles(null).Count == 0, "no key files");
        }

        private static void PathsUnix()
        {
            var files = new HashSet<string> { "/opt/keepass/c.hc", "/home/me/v 1.hc", "/home/me/k,1" };
            var r = new PathResolver(TargetPlatform.Unix, "/opt/keepass", s => s.Replace("{DB_DIR}", "/home/me"),
                files.Contains, d => false, p => p);
            Equal("/opt/keepass/c.hc", r.ResolveVolume("c.hc"), "relative");
            Equal("/home/me/v 1.hc", r.ResolveVolume("{DB_DIR}/v 1.hc"), "placeholder");
            Equal("/dev/sdb1", r.ResolveVolume("/dev/sdb1"), "device");
            Throws<MountConfigurationException>(() => r.ResolveVolume("ID:" + new string('A', 64)), "IDs are Windows only");
            SameList(new[] { "/home/me/k,1" }, r.ResolveKeyFiles("{DB_DIR}/k,1"), "key file with comma");
        }

        // ------------------------------------------------------------------ misc

        private static void KdfValues()
        {
            // From VeraCrypt 1.26.31: Mount.c (Windows, /hash, case-insensitive) and
            // CommandLineInterface.cpp FindKdfAlgorithm (Linux, --hash, case-insensitive).
            var windows = new[] { "sha512", "sha256", "blake2s", "blake2s-256", "argon2", "argon2id", "sha-512", "sha-256", "blake2s-256", "whirlpool", "streebog", "blake2b-512" };
            var linux = new[] { "hmac-sha-512", "hmac-sha-256", "hmac-blake2s-256", "hmac-whirlpool", "hmac-streebog", "argon2", "argon2id", "sha-512", "sha512", "sha-256", "sha256", "blake2s-256", "blake2s", "whirlpool", "streebog" };
            foreach (KdfChoice k in KdfAlgorithms.All)
            {
                if (k.IsAutoDetection || k.RemovedInVeraCrypt126)
                {
                    continue;
                }

                Check(windows.Contains(k.WindowsArgument), k.WindowsArgument + " accepted on Windows");
                Check(linux.Contains(k.UnixArgument), k.UnixArgument + " accepted on Linux");
            }

            Check(KdfAlgorithms.FromStoredValue("") == KdfAlgorithms.AutoDetection, "empty -> auto");
            Check(KdfAlgorithms.FromStoredValue("hmac-whirlpool").ArgumentFor(TargetPlatform.Unix) == "whirlpool", "case-insensitive");
            Check(KdfAlgorithms.FromStoredValue("  ").IsAutoDetection, "blank -> auto");
            Check(KdfAlgorithms.FromStoredValue("nonsense") == null, "unknown");
        }

        private static void RegistryCommand()
        {
            Equal("C:\\Program Files\\VeraCrypt\\VeraCrypt.exe", VeraCryptInfo.ExecutableFromCommand("\"C:\\Program Files\\VeraCrypt\\VeraCrypt.exe\" /v \"%1\""), "quoted");
            Equal("C:\\VC\\VeraCrypt.exe", VeraCryptInfo.ExecutableFromCommand("C:\\VC\\VeraCrypt.exe /v %1"), "unquoted");
            Equal(string.Empty, VeraCryptInfo.ExecutableFromCommand(""), "empty");
        }

        private static void Timeouts()
        {
            Check(PluginOptions.ClampTimeout(-5) == 10000, "min");
            Check(PluginOptions.ClampTimeout(3000) == 10000, "1.0.x default (3 s) is raised to 10 s");
            Check(PluginOptions.ClampTimeout(long.MaxValue) == 60000, "max (old version: cast to int could become negative = wait forever)");
            Check(PluginOptions.ClampTimeout(25000) == 25000, "normal");
        }

        private static void TitleFilter()
        {
            // Same matching as KeePass' AutoType.IsMatchWindow for "//regex//" filters.
            Func<string, string, bool> keePassMatches = (title, filter) =>
                System.Text.RegularExpressions.Regex.IsMatch(title.Trim(), filter.Substring(2, filter.Length - 4),
                    System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            foreach (string vc in new[] { "Enter password for \"C:\\Vol\\a b (1).hc\"", "Enter password for /home/me/x.hc", "Title [x]+?*^$.|(){}#" })
            {
                string filter = TypingSafeguards.BuildTitleFilter(vc);
                Check(filter.StartsWith("//") && filter.EndsWith("//"), "KeePass regex filter syntax");
                Check(!keePassMatches(vc, filter), "VeraCrypt window itself must not abort: " + vc);
                Check(!keePassMatches("  " + vc + " ", filter), "surrounding spaces are trimmed by KeePass");
                Check(keePassMatches("VICTIM", filter), "other window aborts");
                Check(keePassMatches(vc + " - Chat", filter), "title that only starts like VeraCrypt's aborts");
                Check(keePassMatches("", filter), "window without title aborts");
                Check(TypingSafeguards.IsOwnFilter(filter), "own filter is recognised (clean-up after a crash)");
            }

            Check(TypingSafeguards.BuildTitleFilter("  ") == null && TypingSafeguards.BuildTitleFilter(null) == null, "no filter for empty title");
            Check(!TypingSafeguards.IsOwnFilter("//^Chat.*$//") && !TypingSafeguards.IsOwnFilter("Untitled - Notepad")
                  && !TypingSafeguards.IsOwnFilter(null), "user's own abort filters are never removed");
        }

        private static void Argon2Version()
        {
            var s = new MountSettings();
            s.Kdf = "Argon2id";
            Throws<MountConfigurationException>(() => VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, new Version(1, 26, 29)), "1.26.29");
            Throws<MountConfigurationException>(() => VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, new Version(1, 25, 9)), "1.25.9");
            List<string> ok = VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, new Version(1, 26, 30));
            Check(ok.Contains("/hash") && ok.Contains("argon2id"), "1.26.30: " + Show(ok));
            Check(VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, null).Contains("argon2id"), "unknown version: VeraCrypt decides");
            Check(VeraCryptArguments.Build(s, "/v", null, TargetPlatform.Unix, null).Contains("--hash=argon2id"), "Linux");

            try
            {
                VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, new Version(1, 26, 29));
            }
            catch (MountConfigurationException ex)
            {
                Check(ex.Message.Contains("Argon2") && ex.Message.Contains("1.26.30") && ex.Message.Contains("1.26.29"), "message: " + ex.Message);
            }

            s.Kdf = string.Empty; // Autodetection (also finds Argon2 volumes) works with every version
            Check(!VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, new Version(1, 26, 29)).Contains("/hash"), "auto");
        }

        private static void KdfListMatchesVeraCrypt()
        {
            // VeraCrypt 1.26.31 password dialog, PRF list (Windows: Mount.c / Linux: VolumePasswordPanel).
            string[] veraCrypt = { "Autodetection", "SHA512-PBKDF2", "Whirlpool-PBKDF2", "SHA256-PBKDF2", "BLAKE2S-PBKDF2", "STREEBOG-PBKDF2", "Argon2" };
            string[] plugin = KdfAlgorithms.All.Where(k => !k.RemovedInVeraCrypt126).Select(k => k.ToString()).ToArray();
            SameList(veraCrypt, plugin, "names and order");

            // Values stored by 1.0.7 are still read (field veracrypt.option.pkcs5).
            foreach (string old in new[] { "", "HMAC-SHA-512", "HMAC-Whirlpool", "HMAC-SHA-256", "HMAC-RIPEMD-160", "HMAC-Streebog" })
            {
                Check(KdfAlgorithms.FromStoredValue(old) != null, "1.0.7 value: " + old);
            }

            Check(KdfAlgorithms.FromStoredValue("HMAC-RIPEMD-160").RemovedInVeraCrypt126, "RIPEMD marked as legacy");
            Check(KdfAlgorithms.All.Select(k => k.StoredValue.ToUpperInvariant()).Distinct().Count() == KdfAlgorithms.All.Length, "stored values unique");
        }

        private static void SecureDesktopArgument()
        {
            var s = new MountSettings();
            Func<List<string>, bool> asksNormalDesktop = a =>
            {
                int i = a.IndexOf("/secureDesktop");
                return i >= 0 && i + 1 < a.Count && a[i + 1] == "n" && a.IndexOf("/secureDesktop", i + 1) < 0;
            };

            List<string> at = VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, new Version(1, 26, 31), true);
            Check(asksNormalDesktop(at), "Auto-Type: /secureDesktop n: " + Show(at));
            Check(asksNormalDesktop(VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, new Version(1, 20, 0), true)), "1.20");
            Check(!VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, new Version(1, 26, 31), false).Contains("/secureDesktop"),
                "no Auto-Type: VeraCrypt decides");
            Check(!VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, new Version(1, 19, 0), true).Contains("/secureDesktop"),
                "1.19 does not know the option");
            Check(!VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, null, true).Contains("/secureDesktop"),
                "unknown version: not passed (an unknown option stops VeraCrypt)");
            Check(VeraCryptArguments.Build(s, "/v", null, TargetPlatform.Unix, null, true).All(a => a.IndexOf("secure", StringComparison.OrdinalIgnoreCase) < 0),
                "Linux has no Secure Desktop");
        }

        private static void LabelRules()
        {
            Check(VeraCryptArguments.IsValidLabel("") && VeraCryptArguments.IsValidLabel(null), "no label");
            Check(VeraCryptArguments.IsValidLabel("My Data") && VeraCryptArguments.IsValidLabel(new string('x', 32)), "normal / 32 characters");
            Check(!VeraCryptArguments.IsValidLabel(new string('x', 33)), "33 characters");
            Check(!VeraCryptArguments.IsValidLabel("a\"b") && !VeraCryptArguments.IsValidLabel("a\tb"), "quote / tab");

            // Linux has no labels: a label (even one Windows would refuse) is ignored, not an error,
            // and stays stored in the entry for Windows (the dialog keeps the hidden value).
            var s = new MountSettings();
            s.Label = new string('x', 40);
            List<string> unix = VeraCryptArguments.Build(s, "/v", null, TargetPlatform.Unix, null);
            Check(unix.All(a => !a.Contains("label")), "no label on Linux: " + Show(unix));
            Throws<MountConfigurationException>(() => VeraCryptArguments.Build(s, "C:\\v", null, TargetPlatform.Windows, null), "too long on Windows");
        }

        // ------------------------------------------------------------------ test database for the end-to-end test

        private static int MakeTestDatabase(string[] args)
        {
            string file = args[1];
            string master = args[2];
            string entryPassword = args[3];
            string volume = args[4];
            bool autoType = args.Length < 6 || args[5] != "0";

            var db = new PwDatabase();
            var key = new CompositeKey();
            key.AddUserKey(new KcpPassword(master));
            db.New(IOConnectionInfo.FromPath(file), key);
            db.KdfParameters = new KeePassLib.Cryptography.KeyDerivation.AesKdf().GetDefaultParameters();

            var e = new PwEntry(true, true);
            e.Strings.Set(PwDefs.TitleField, new ProtectedString(false, "VeraCrypt test volume"));
            e.Strings.Set(PwDefs.UserNameField, new ProtectedString(false, "not-typed-user"));
            e.Strings.Set(PwDefs.PasswordField, new ProtectedString(true, entryPassword));
            var s = new MountSettings();
            s.Volume = volume;
            s.HideDialog = true;
            s.AutoType = true;
            s.ReadOnly = true;
            s.Kdf = "HMAC-SHA-512";
            s.Pim = "485";
            s.WriteTo(e);
            e.AutoType.Enabled = autoType;
            db.RootGroup.AddEntry(e, true);
            db.Save(null);
            db.Close();
            Console.WriteLine("created " + file);
            return 0;
        }
    }
}
