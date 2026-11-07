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
// PasswordWindowProbe.cs - "is VeraCrypt's password window in front and ready for typing?"
// -------------------------------------------------------------------------------------------------
// Used by PendingAutoType (layers 1 and 3 of its safety layers). Every check starts from the
// process ID of the VeraCrypt process the plugin started itself, so a window of another program
// can never be taken for VeraCrypt's, whatever its title.
//
// WINDOWS (WindowsProbe; VeraCrypt source: src/Mount/Mount.rc IDD_PASSWORD_DLG, PasswordDlgProc)
//   VeraCrypt's password window = a visible dialog box (class "#32770") of that process that has
//   VeraCrypt's password field (control ID IDC_PASSWORD = 1035, class "Edit") and its KDF list
//   (IDC_PKCS5_PRF_ID = 5127, class "ComboBox"). Both IDs are the same in every VeraCrypt from
//   1.19 to 1.26.31 (src/Mount/Resource.h, src/Common/Resource.h).
//   Ready = it is the foreground window and the keyboard focus (GetGUIThreadInfo) is in exactly
//   that password field. VeraCrypt puts the focus there itself (PasswordDlgProc, WM_INITDIALOG).
//   Ignored on purpose (nothing is typed while one of them has the focus):
//     * message boxes, e.g. "The volume is already mounted";
//     * the security token PIN dialog (IDD_TOKEN_PASSWORD). With token key files and VeraCrypt's
//       password cache, VeraCrypt can ask for the PIN before the password; typing the volume
//       password there would also count as a wrong PIN on the token;
//     * the PIM field and every other control of the password dialog;
//     * VeraCrypt's other windows (main window with its volume path field, Mount Options).
//   Control IDs and class names are not translated, so this works for VeraCrypt in every language.
//   VeraCrypt's Secure Desktop: mounts started by the plugin show the password window on the
//   normal desktop (VeraCryptArguments: /q, and /secureDesktop n with Auto-Type). Should a window
//   ever be on another desktop, it is not found and nothing is typed.
//
// LINUX (XDoToolProbe; xdotool is required by KeePass' Auto-Type on Linux anyway)
//   A visible window whose _NET_WM_PID is that process must be both the active window (what the
//   window manager reports) and the window with the keyboard focus (where keys really go; the
//   two differ for a moment while windows are switched). X11 cannot tell which control has the
//   focus, so the Linux version cannot tell the password window from VeraCrypt's other dialogs.
//   Wayland: KeePass/xdotool see only XWayland windows; MountController starts VeraCrypt with
//   GDK_BACKEND=x11 for that reason.
//
// HISTORY
//   1.0.x: no check at all (typed into whatever window was in front after WaitForInputIdle).
//   2.0.0 (2026-11): this file. Linux compares the active and the focus window (a stress test
//          showed one leak with the active window alone); Windows identifies VeraCrypt's password
//          field and KDF list by their control IDs and requires the focus in the password field,
//          so the token PIN dialog, the PIM field and VeraCrypt's other windows are never typed
//          into.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Runtime.InteropServices;

    using KeePassLib.Native;

    internal enum ProbeState
    {
        /// <summary>The VeraCrypt process has no window yet.</summary>
        NotFound,

        /// <summary>VeraCrypt shows a window, but it is not (yet) the active window ready for typing.</summary>
        FoundNotReady,

        /// <summary>VeraCrypt's window is the active window and the cursor is in an input field.</summary>
        Ready,

        /// <summary>This system offers no way to check the active window.</summary>
        Unsupported,
    }

    /// <summary>Recognises the password window of the VeraCrypt process the plugin started.</summary>
    internal interface IPasswordWindowProbe
    {
        ProbeState Probe(int processId, out IntPtr window);

        /// <summary>Asks the system once to bring the window to the front.</summary>
        void Activate(IntPtr window);

        /// <summary>Checked again immediately before KeePass types the first key.</summary>
        bool IsStillReady(int processId, IntPtr window);

        /// <summary>The window title as KeePass sees it (used to abort typing if another window gets in front).</summary>
        string GetTitle(IntPtr window);
    }

    internal static class PasswordWindowProbe
    {
        public static IPasswordWindowProbe ForCurrentPlatform()
        {
            return Platform.IsUnix ? (IPasswordWindowProbe)new XDoToolProbe() : new WindowsProbe();
        }
    }

    /// <summary>Windows: see the file header.</summary>
    internal sealed class WindowsProbe : IPasswordWindowProbe
    {
        /// <summary>VeraCrypt's password field: IDC_PASSWORD in src/Mount/Resource.h.</summary>
        internal const int PasswordFieldId = 1035;

        /// <summary>KDF list of VeraCrypt's password dialog: IDC_PKCS5_PRF_ID in src/Common/Resource.h.</summary>
        internal const int KdfListId = 5127;

        public ProbeState Probe(int processId, out IntPtr window)
        {
            window = IntPtr.Zero;
            List<IntPtr> dialogs = FindDialogs(processId);
            if (dialogs.Count == 0)
            {
                return ProbeState.NotFound;
            }

            foreach (IntPtr dialog in dialogs)
            {
                if (IsReady(processId, dialog))
                {
                    window = dialog;
                    return ProbeState.Ready;
                }
            }

            window = dialogs[0];
            return ProbeState.FoundNotReady;
        }

        public void Activate(IntPtr window)
        {
            if (window != IntPtr.Zero)
            {
                NativeMethods.SetForegroundWindow(window);
            }
        }

        public bool IsStillReady(int processId, IntPtr window)
        {
            return window != IntPtr.Zero && IsReady(processId, window);
        }

        public string GetTitle(IntPtr window)
        {
            return window == IntPtr.Zero ? string.Empty : NativeMethods.GetWindowTextOf(window);
        }

        private static bool IsReady(int processId, IntPtr dialog)
        {
            if (NativeMethods.GetForegroundWindow() != dialog)
            {
                return false;
            }

            int owner;
            int threadId = NativeMethods.GetWindowThreadProcessId(dialog, out owner);
            if (threadId == 0 || owner != processId)
            {
                return false;
            }

            if (!NativeMethods.IsWindowVisible(dialog) || !NativeMethods.IsWindowEnabled(dialog))
            {
                return false;
            }

            IntPtr passwordField = GetControl(dialog, PasswordFieldId, "Edit");
            if (passwordField == IntPtr.Zero ||
                !NativeMethods.IsWindowVisible(passwordField) || !NativeMethods.IsWindowEnabled(passwordField))
            {
                return false;
            }

            var info = new NativeMethods.GUITHREADINFO();
            info.cbSize = Marshal.SizeOf(typeof(NativeMethods.GUITHREADINFO));
            if (!NativeMethods.GetGUIThreadInfo(threadId, ref info))
            {
                return false;
            }

            return info.hwndFocus == passwordField;
        }

        /// <summary>
        /// Visible dialog boxes of the process that have VeraCrypt's password field and KDF list
        /// (message boxes, the token PIN dialog and VeraCrypt's other windows are ignored).
        /// </summary>
        private static List<IntPtr> FindDialogs(int processId)
        {
            var result = new List<IntPtr>();
            NativeMethods.EnumWindowsProc callback = delegate(IntPtr hWnd, IntPtr lParam)
            {
                int owner;
                NativeMethods.GetWindowThreadProcessId(hWnd, out owner);
                if (owner == processId && NativeMethods.IsWindowVisible(hWnd) &&
                    NativeMethods.GetClassNameOf(hWnd) == "#32770" &&
                    GetControl(hWnd, PasswordFieldId, "Edit") != IntPtr.Zero &&
                    GetControl(hWnd, KdfListId, "ComboBox") != IntPtr.Zero)
                {
                    result.Add(hWnd);
                }

                return true;
            };

            NativeMethods.EnumWindows(callback, IntPtr.Zero);
            GC.KeepAlive(callback);
            return result;
        }

        /// <summary>The dialog's control with this ID if it has the expected class, otherwise IntPtr.Zero.</summary>
        private static IntPtr GetControl(IntPtr dialog, int id, string className)
        {
            IntPtr control = NativeMethods.GetDlgItem(dialog, id);
            return control != IntPtr.Zero &&
                   string.Equals(NativeMethods.GetClassNameOf(control), className, StringComparison.OrdinalIgnoreCase)
                ? control
                : IntPtr.Zero;
        }
    }

    /// <summary>Linux (X11, xdotool): see the file header.</summary>
    internal sealed class XDoToolProbe : IPasswordWindowProbe
    {
        public ProbeState Probe(int processId, out IntPtr window)
        {
            window = IntPtr.Zero;
            string found = Run("search --onlyvisible --pid " + processId.ToString(CultureInfo.InvariantCulture));
            if (found == null)
            {
                return ProbeState.Unsupported;
            }

            List<long> windows = ParseIds(found);
            if (windows.Count == 0)
            {
                return ProbeState.NotFound;
            }

            long active = ActiveWindow();
            if (active == -1)
            {
                return ProbeState.Unsupported;
            }

            if (windows.Contains(active) && FocusWindow() == active)
            {
                window = new IntPtr(active);
                return ProbeState.Ready;
            }

            window = new IntPtr(windows[windows.Count - 1]);
            return ProbeState.FoundNotReady;
        }

        public void Activate(IntPtr window)
        {
            if (window != IntPtr.Zero)
            {
                Run("windowactivate " + window.ToInt64().ToString(CultureInfo.InvariantCulture));
            }
        }

        public bool IsStillReady(int processId, IntPtr window)
        {
            if (window == IntPtr.Zero || ActiveWindow() != window.ToInt64() || FocusWindow() != window.ToInt64())
            {
                return false;
            }

            string pid = Run("getwindowpid " + window.ToInt64().ToString(CultureInfo.InvariantCulture));
            int owner;
            return pid != null && int.TryParse(pid.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out owner) &&
                   owner == processId;
        }

        public string GetTitle(IntPtr window)
        {
            if (window == IntPtr.Zero)
            {
                return string.Empty;
            }

            string name = Run("getwindowname " + window.ToInt64().ToString(CultureInfo.InvariantCulture));
            return (name ?? string.Empty).Trim('\r', '\n');
        }

        private static long ActiveWindow()
        {
            return ParseId(Run("getactivewindow"));
        }

        /// <summary>The top-level window that has the keyboard focus.</summary>
        private static long FocusWindow()
        {
            return ParseId(Run("getwindowfocus"));
        }

        private static long ParseId(string text)
        {
            long id;
            if (text != null && long.TryParse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out id))
            {
                return id;
            }

            return -1;
        }

        private static List<long> ParseIds(string text)
        {
            var ids = new List<long>();
            foreach (string line in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                long id;
                if (long.TryParse(line.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out id))
                {
                    ids.Add(id);
                }
            }

            return ids;
        }

        /// <summary>Returns xdotool's output, or null if xdotool could not be run.</summary>
        private static string Run(string arguments)
        {
            try
            {
                return NativeLib.RunConsoleApp("xdotool", arguments);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
