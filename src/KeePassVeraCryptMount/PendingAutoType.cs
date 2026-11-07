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
// PendingAutoType.cs - waits for VeraCrypt's password window, then types the password once
// -------------------------------------------------------------------------------------------------
// WHAT
//   Created by MountController after it started VeraCrypt (only if "Auto-Type" is checked for the
//   entry). A WinForms timer polls every 150 ms, so KeePass stays responsive:
//
//     Start --> [tick] --+-- database closed/locked, entry deleted ----------> Finish (nothing typed)
//                        +-- probe Unsupported (Linux without xdotool) ------> Finish + message
//                        +-- no VeraCrypt window, VeraCrypt process exited --> Finish (nothing typed)
//                        +-- window found, not ready --> ask once to bring it to the front
//                        +-- window Ready on 2 checks in a row (3 after the plugin activated it)
//                        |       --> TypePassword() --> Finish
//                        +-- timeout (plugin option, 10-60 s, default 10 s) --> Finish (nothing typed;
//                                message only if a VeraCrypt window is still waiting)
//
// SAFETY LAYERS (why the password cannot end up in another program)
//   1. PasswordWindowProbe: the window belongs to the VeraCrypt process the plugin started, is the
//      foreground window and (Windows) has the keyboard focus in an input field.
//   2. Stable for 2-3 consecutive checks (150 ms apart).
//   3. OnFilterSend: KeePass raises AutoType.FilterSend right before sending the keys (the password
//      is already in the sequence then); the probe runs again and the sequence is emptied if the
//      window is no longer ready.
//   4. TypingSafeguards: KeePass checks before every key that the active window did not change and
//      still has the VeraCrypt window's title.
//   5. Exactly one typing attempt per mount; nothing is retried, so a wrong password never loops.
//
// WHAT IS TYPED
//   The entry's own default Auto-Type sequence if it has one, otherwise "{PASSWORD}{ENTER}".
//   Version 1.0.x wrote "{PASSWORD}{ENTER}" into every entry it configured (and switched on
//   two-channel obfuscation); 2.0.0 no longer changes the entry's Auto-Type settings, but uses them
//   (obfuscation is used if it is switched on in the entry's Auto-Type tab). KeePass window
//   associations are not used: the sequence is passed explicitly (AutoType.PerformIntoCurrentWindow
//   with a sequence, KeePass source Util/AutoType.cs).
//
// HISTORY
//   1.0.x: Process.WaitForInputIdle(timeout), then auto-type into whatever window was in front,
//          then Thread.Sleep(1000) on the UI thread. WaitForInputIdle returns immediately on Mono
//          and does not mean that the password window is shown or focused; the password could go
//          to another program (reproduced: it was typed into KeePass itself).
//   2.0.0 (2026-11): this class.
//
// NOTES FOR MAINTAINERS
//   * Everything runs on the UI thread. PerformIntoCurrentWindow pumps messages (DoEvents), so other
//     plugin code can run while IsTyping is true; MountController refuses a new mount then.
//   * Finish() is idempotent and the only place that ends an operation.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;
    using System.Diagnostics;

    using KeePass.Plugins;
    using KeePass.Util;

    using KeePassLib;
    using KeePassLib.Utility;

    internal sealed class PendingAutoType
    {
        public const string DefaultSequence = "{PASSWORD}{ENTER}";

        private const int PollIntervalMs = 150;

        /// <summary>The window must be ready on this many consecutive checks before typing.</summary>
        private const int RequiredReadyChecks = 2;

        /// <summary>After the plugin had to bring VeraCrypt to the front, wait one check longer.</summary>
        private const int RequiredReadyChecksAfterActivation = 3;

        private readonly IPluginHost host;
        private readonly PwEntry entry;
        private readonly PwDatabase database;
        private readonly Process process;
        private readonly int processId;
        private readonly IPasswordWindowProbe probe;
        private readonly int timeoutMs;
        private readonly Stopwatch elapsed = new Stopwatch();

        private System.Windows.Forms.Timer timer;
        private bool finished;
        private bool activationTried;
        private int readyChecks;
        private IntPtr expectedWindow = IntPtr.Zero;
        private bool blockedByFilter;
        private bool quiet;

        public PendingAutoType(IPluginHost host, PwEntry entry, PwDatabase database, Process process,
            IPasswordWindowProbe probe, int timeoutMs)
        {
            this.host = host;
            this.entry = entry;
            this.database = database;
            this.process = process;
            this.probe = probe;
            this.timeoutMs = timeoutMs;

            try
            {
                this.processId = process.Id;
            }
            catch (InvalidOperationException)
            {
                this.processId = 0;
            }
        }

        public event EventHandler Finished;

        public bool IsTyping { get; private set; }

        public void Start()
        {
            if (this.processId == 0)
            {
                this.Finish(LanguageTexts.AutoTypeNoWindow, null);
                return;
            }

            this.SetStatus(LanguageTexts.WaitingForVeraCrypt);
            this.elapsed.Start();
            this.timer = new System.Windows.Forms.Timer();
            this.timer.Interval = PollIntervalMs;
            this.timer.Tick += this.OnTick;
            this.timer.Start();
        }

        /// <summary>Stops waiting; nothing will be typed. Ignored while typing.</summary>
        public void Cancel()
        {
            if (!this.IsTyping)
            {
                this.Finish(LanguageTexts.AutoTypeCancelled, null);
            }
        }

        /// <summary>KeePass is closing: undo the temporary KeePass settings, show no more messages.</summary>
        public void Shutdown()
        {
            this.quiet = true;
            TypingSafeguards.Restore();
            this.Cancel();
        }

        private void OnTick(object sender, EventArgs e)
        {
            if (this.finished || this.IsTyping)
            {
                return;
            }

            try
            {
                this.Check();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("KeePassVeraCryptMount: " + ex);
                this.Finish(LanguageTexts.AutoTypeCancelled, null);
            }
        }

        private void Check()
        {
            if (!this.IsEntryStillAvailable())
            {
                this.Finish(LanguageTexts.AutoTypeCancelled, null);
                return;
            }

            IntPtr window;
            ProbeState state = this.probe.Probe(this.processId, out window);

            if (state == ProbeState.Unsupported)
            {
                this.Finish(LanguageTexts.AutoTypeCancelled, LanguageTexts.AutoTypeUnsupported);
                return;
            }

            if (state == ProbeState.Ready)
            {
                if (window == this.expectedWindow)
                {
                    this.readyChecks++;
                }
                else
                {
                    this.expectedWindow = window;
                    this.readyChecks = 1;
                }

                int required = this.activationTried ? RequiredReadyChecksAfterActivation : RequiredReadyChecks;
                if (this.readyChecks >= required)
                {
                    this.TypePassword();
                    return;
                }
            }
            else
            {
                this.readyChecks = 0;
                this.expectedWindow = IntPtr.Zero;

                if (state == ProbeState.FoundNotReady)
                {
                    if (!this.activationTried)
                    {
                        this.activationTried = true;
                        this.probe.Activate(window);
                    }
                }
                else if (this.HasProcessExited())
                {
                    // e.g. mounted with a cached password, or VeraCrypt showed an error and quit.
                    this.Finish(LanguageTexts.VeraCryptExited, null);
                    return;
                }
            }

            if (this.elapsed.ElapsedMilliseconds > this.timeoutMs)
            {
                // Explain only if a VeraCrypt window is still open and waiting; if it is gone
                // (e.g. the user typed the password already), the status bar text is enough.
                this.Finish(LanguageTexts.AutoTypeNoWindow,
                    state != ProbeState.NotFound ? LanguageTexts.AutoTypeNotVerified : null);
            }
        }

        private void TypePassword()
        {
            this.timer.Stop();
            this.IsTyping = true;

            string sequence = this.entry.AutoType.DefaultSequence;
            if (string.IsNullOrEmpty(sequence) || sequence.Trim().Length == 0)
            {
                sequence = DefaultSequence;
            }

            bool typed = false;
            Exception error = null;
            string titleFilter = TypingSafeguards.BuildTitleFilter(this.probe.GetTitle(this.expectedWindow));
            AutoType.FilterSend += this.OnFilterSend;
            try
            {
                TypingSafeguards.Apply(titleFilter);
                typed = AutoType.PerformIntoCurrentWindow(this.entry, this.database, sequence);
            }
            catch (Exception ex)
            {
                error = ex;
            }
            finally
            {
                AutoType.FilterSend -= this.OnFilterSend;
                TypingSafeguards.Restore();
                this.IsTyping = false;
            }

            if (error != null)
            {
                this.Finish(LanguageTexts.AutoTypeCancelled, LanguageTexts.AutoTypeFailed + error.Message);
            }
            else if (this.blockedByFilter)
            {
                this.Finish(LanguageTexts.AutoTypeCancelled, LanguageTexts.AutoTypeNotVerified);
            }
            else if (!typed)
            {
                this.Finish(LanguageTexts.AutoTypeCancelled, null);
            }
            else
            {
                this.Finish(LanguageTexts.AutoTypeDone, null);
            }
        }

        /// <summary>
        /// Raised by KeePass right before it sends the keys. Last check that VeraCrypt's window is
        /// still ready; otherwise the sequence is emptied and nothing is typed. Only reacts to the
        /// plugin's own entry, so KeePass' normal Auto-Type of other entries is never affected.
        /// </summary>
        private void OnFilterSend(object sender, AutoTypeEventArgs e)
        {
            if (e == null || !ReferenceEquals(e.Entry, this.entry))
            {
                return;
            }

            bool ok;
            try
            {
                ok = this.probe.IsStillReady(this.processId, this.expectedWindow);
            }
            catch (Exception)
            {
                ok = false;
            }

            if (!ok)
            {
                e.Sequence = string.Empty;
                this.blockedByFilter = true;
            }
        }

        private bool IsEntryStillAvailable()
        {
            if (!this.database.IsOpen || this.entry.ParentGroup == null)
            {
                return false;
            }

            foreach (PwDatabase open in this.host.MainWindow.DocumentManager.GetOpenDatabases())
            {
                if (ReferenceEquals(open, this.database))
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasProcessExited()
        {
            try
            {
                return this.process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return true;
            }
            catch (System.ComponentModel.Win32Exception)
            {
                return false;
            }
        }

        private void Finish(string status, string message)
        {
            if (this.finished)
            {
                return;
            }

            this.finished = true;
            if (this.timer != null)
            {
                this.timer.Stop();
                this.timer.Tick -= this.OnTick;
                this.timer.Dispose();
                this.timer = null;
            }

            try
            {
                this.process.Dispose(); // only releases the handle; VeraCrypt keeps running
            }
            catch (Exception)
            {
            }

            if (!this.quiet)
            {
                this.SetStatus(status);
                if (message != null)
                {
                    MessageService.ShowWarning(LanguageTexts.MountErrorTitle, message);
                }
            }

            EventHandler handler = this.Finished;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void SetStatus(string text)
        {
            try
            {
                this.host.MainWindow.SetStatusEx(text);
            }
            catch (Exception)
            {
            }
        }
    }
}
