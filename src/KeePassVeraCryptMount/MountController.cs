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
// MountController.cs - one "mount" from the saved entry settings to the started VeraCrypt process
// -------------------------------------------------------------------------------------------------
// FLOW (Mount)
//   1. VeraCrypt program configured and present?               (PluginOptions, VeraCryptInfo)
//   2. Read the entry's settings; Auto-Type for this mount = the entry's mount setting AND
//      Auto-Type enabled for the entry in KeePass              (MountSettings)
//   3. Volume and key file paths -> absolute paths, files exist (PathResolver)
//   4. Validated argument list for this platform/VeraCrypt; with Auto-Type it asks VeraCrypt
//      for the normal desktop                                   (VeraCryptArguments)
//      Any problem in 3-4: a message names it and VeraCrypt is not started.
//   5. Auto-Type wanted but disabled for the entry in KeePass? Tell the user BEFORE VeraCrypt
//      opens (so the message does not cover VeraCrypt's window), then continue without typing.
//   6. Start VeraCrypt (no shell; working folder = VeraCrypt's folder).
//   7. Auto-Type: hand over to PendingAutoType. Exception: if Windows had to ask for administrator
//      rights (VeraCrypt set to "Run as administrator"), KeePass cannot type into VeraCrypt
//      (Windows UIPI blocks input to elevated windows), so the user is told to type the password.
//
// POLICIES
//   * At most one operation waits at a time; a new mount cancels a waiting one (it types nothing),
//     but never interrupts typing in progress.
//   * Placeholders: only path placeholders and %ENV% variables are expanded (ExpandPlaceholders),
//     never field references like {PASSWORD} - no secret can reach the command line.
//   * The password is never passed on the command line (VeraCrypt's /p would expose it to other
//     programs and to logs).
//
// HISTORY
//   1.0.x: this logic lived in KeepassVeraCryptMountExt.OnVeraCryptMenuItemClicked and
//          PwEntryExtension.ToVeraCryptArguments (string concatenation, ShellExecute,
//          WaitForInputIdle + blind auto-type, {DB_DIR} broken because no database was passed).
//   2.0.0 (2026-11): rewritten as this class.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;

    using KeePass.Plugins;
    using KeePass.Util;
    using KeePass.Util.Spr;

    using KeePassLib;
    using KeePassLib.Utility;

    internal sealed class MountController : IDisposable
    {
        /// <summary>Windows: ERROR_ELEVATION_REQUIRED (VeraCrypt.exe set to "Run as administrator").</summary>
        private const int ErrorElevationRequired = 740;

        private readonly IPluginHost host;
        private PendingAutoType pending;

        public MountController(IPluginHost host)
        {
            this.host = host;
        }

        public void Mount(PwEntry entry, PwDatabase database)
        {
            if (entry == null || database == null || !database.IsOpen)
            {
                return;
            }

            if (this.pending != null)
            {
                if (this.pending.IsTyping)
                {
                    return; // never interrupt typing in progress
                }

                this.pending.Cancel(); // the waiting operation types nothing
            }

            string executable = PluginOptions.GetExecutable(this.host);
            if (!VeraCryptInfo.ExecutableExists(executable))
            {
                ShowWarning(LanguageTexts.ExecutableMissing);
                return;
            }

            MountSettings settings = MountSettings.ReadFrom(entry);
            bool autoType = settings.AutoType && entry.GetAutoTypeEnabled();
            List<string> arguments;
            try
            {
                var resolver = new PathResolver(Platform.Current, GetAppDirectory(),
                    text => ExpandPlaceholders(text, entry, database));
                string volume = resolver.ResolveVolume(settings.Volume);
                List<string> keyFiles = resolver.ResolveKeyFiles(settings.KeyFiles);
                arguments = VeraCryptArguments.Build(settings, volume, keyFiles, Platform.Current,
                    VeraCryptInfo.GetVersion(executable), autoType);
            }
            catch (MountConfigurationException ex)
            {
                ShowWarning(ex.Message);
                return;
            }

            if (settings.AutoType && !autoType)
            {
                ShowWarning(LanguageTexts.AutoTypeDisabledForEntry); // VeraCrypt still starts
            }

            bool elevated;
            Process process = StartVeraCrypt(executable, arguments, autoType, out elevated);
            if (process == null)
            {
                return;
            }

            if (autoType && elevated)
            {
                SetStatus(this.host, LanguageTexts.AutoTypeElevated);
                autoType = false;
            }

            if (!autoType)
            {
                process.Dispose(); // only releases the handle; VeraCrypt keeps running
                return;
            }

            var operation = new PendingAutoType(this.host, entry, database, process,
                PasswordWindowProbe.ForCurrentPlatform(), PluginOptions.GetAutoTypeTimeout(this.host));
            operation.Finished += this.OnPendingFinished;
            this.pending = operation;
            operation.Start();
        }

        /// <summary>Called when the plugin is unloaded (KeePass exits).</summary>
        public void Dispose()
        {
            PendingAutoType operation = this.pending;
            this.pending = null;
            if (operation != null)
            {
                operation.Shutdown();
            }
        }

        internal static string GetAppDirectory()
        {
            return UrlUtil.GetFileDirectory(WinUtil.GetExecutable(), false, false);
        }

        /// <summary>
        /// Expands only path placeholders ({APPDIR}, {DB_DIR}, {DB_PATH}, ...) and %ENVIRONMENT%
        /// variables (KeePass source: Util/Spr/SprEngine.cs). The database must be passed for the
        /// {DB_*} placeholders (1.0.x passed null, so they never worked).
        /// </summary>
        internal static string ExpandPlaceholders(string text, PwEntry entry, PwDatabase database)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return SprEngine.Compile(text, new SprContext(entry, database,
                SprCompileFlags.Paths | SprCompileFlags.EnvVars));
        }

        private static Process StartVeraCrypt(string executable, List<string> arguments, bool autoType, out bool elevated)
        {
            elevated = false;
            var psi = new ProcessStartInfo(executable, CommandLine.Join(arguments, Platform.Current));
            psi.UseShellExecute = false; // start VeraCrypt directly, no shell, and get its process ID
            try
            {
                psi.WorkingDirectory = Path.GetDirectoryName(executable); // not KeePass' current folder
            }
            catch (ArgumentException)
            {
            }

            if (Platform.IsUnix && autoType &&
                !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) &&
                string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GDK_BACKEND")))
            {
                // KeePass (xdotool) can only type into X11 windows: let VeraCrypt (GTK) use XWayland.
                psi.EnvironmentVariables["GDK_BACKEND"] = "x11";
            }

            try
            {
                return Process.Start(psi);
            }
            catch (Win32Exception ex)
            {
                if (ex.NativeErrorCode != ErrorElevationRequired)
                {
                    ShowWarning(LanguageTexts.StartFailed + ex.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                ShowWarning(LanguageTexts.StartFailed + ex.Message);
                return null;
            }

            // VeraCrypt.exe requires administrator rights: let Windows ask (UAC), as 1.0.x did.
            try
            {
                psi.UseShellExecute = true;
                elevated = true;
                return Process.Start(psi);
            }
            catch (Exception ex)
            {
                ShowWarning(LanguageTexts.StartFailed + ex.Message); // includes "cancelled by the user"
                return null;
            }
        }

        private static void ShowWarning(string message)
        {
            MessageService.ShowWarning(LanguageTexts.MountErrorTitle, message);
        }

        private static void SetStatus(IPluginHost host, string text)
        {
            try
            {
                host.MainWindow.SetStatusEx(text);
            }
            catch (Exception)
            {
            }
        }

        private void OnPendingFinished(object sender, EventArgs e)
        {
            var operation = sender as PendingAutoType;
            if (operation != null)
            {
                operation.Finished -= this.OnPendingFinished;
            }

            if (ReferenceEquals(this.pending, operation))
            {
                this.pending = null;
            }
        }
    }
}
