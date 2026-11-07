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
// TypingSafeguards.cs - KeePass options switched on only while the plugin types into VeraCrypt
// -------------------------------------------------------------------------------------------------
// WHAT
//   While PendingAutoType types the password, two KeePass options are switched on temporarily, so
//   that KeePass itself checks the active window before EVERY key it sends:
//   * Integration.AutoTypeCancelOnWindowChange = true: KeePass stops silently if the foreground
//     window is no longer the window that was in front when typing started.
//   * Integration.AutoTypeAbortOnWindows += "//^(?!<VeraCrypt window title>$).*$//": a KeePass
//     regex filter that matches every window title EXCEPT the title of the verified VeraCrypt
//     window; KeePass aborts (with its message "Auto-Type has been aborted, because the target
//     window is disallowed by the application policy ...") as soon as another window is in front.
//   Both are KeePass 2.x options (KeePass source: App/Configuration/AceIntegration.cs), checked in
//   Util/SendInputExt/SiEngineStd.cs ValidateState(), which runs before every key event.
//
// WHY
//   The plugin's own checks (PasswordWindowProbe, PendingAutoType.OnFilterSend) run before KeePass
//   starts typing. KeePass records its target window a few milliseconds later, when its typing
//   engine starts. Without the title filter, a window switch exactly in that gap made KeePass type
//   into the other window: in 1 of 13 stress-test runs on Linux (where each xdotool call takes
//   ~10 ms) the whole password went to the other window. With the filter: 0 of 18 runs, not one
//   character. The filter also stops typing if the focus changes in the middle of the password.
//
// RESTORING (important)
//   The user's own settings must never stay changed:
//   * Restore() runs in the finally block right after typing (PendingAutoType.TypePassword).
//   * KeePass processes window messages while typing (Application.DoEvents), so the user could
//     close KeePass in that moment. On a normal exit KeePass unloads the plugins BEFORE it saves
//     its configuration (KeePass source: Forms/MainForm_Functions.cs, CleanUpEx: "UnloadAllPlugins
//     ... Before saving the configuration"), and the plugin's Terminate() calls Restore().
//   * If KeePass saves its configuration while the options are switched on anyway (Windows shuts
//     down during the ~1 second of typing, or a crash right after a save), a marker in the plugin's
//     own configuration (MarkerKey) holds the user's original value; RecoverAfterUnexpectedExit()
//     runs at the next start (plugin Initialize) and undoes both changes.
//   Restore() is idempotent; Apply() restores a previous state first.
//   There is no KeePass dialog for AutoTypeAbortOnWindows; it only exists in KeePass.config.xml
//   (<Integration><AutoTypeAbortOnWindows><Window>...</Window>).
//
// HISTORY
//   2.0.0 (2026-11): added.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    internal static class TypingSafeguards
    {
        /// <summary>Present in the KeePass configuration only while typing; value = user's original setting.</summary>
        public const string MarkerKey = "veracrypt.autotype.restore.cancelonwindowchange";

        private const string FilterPrefix = "//^(?!";
        private const string FilterSuffix = "$).*$//";

        private static bool active;
        private static bool savedCancelOnWindowChange;
        private static string addedFilter;

        /// <param name="titleFilter">Result of <see cref="BuildTitleFilter"/>; may be null (no title known).</param>
        public static void Apply(string titleFilter)
        {
            Restore();

            var config = KeePass.Program.Config;
            savedCancelOnWindowChange = config.Integration.AutoTypeCancelOnWindowChange;
            config.CustomConfig.SetBool(MarkerKey, savedCancelOnWindowChange);
            config.Integration.AutoTypeCancelOnWindowChange = true;

            if (!string.IsNullOrEmpty(titleFilter))
            {
                config.Integration.AutoTypeAbortOnWindows.Add(titleFilter);
                addedFilter = titleFilter;
            }

            active = true;
        }

        public static void Restore()
        {
            if (!active)
            {
                return;
            }

            active = false;
            var config = KeePass.Program.Config;
            config.Integration.AutoTypeCancelOnWindowChange = savedCancelOnWindowChange;

            if (addedFilter != null)
            {
                config.Integration.AutoTypeAbortOnWindows.Remove(addedFilter);
                addedFilter = null;
            }

            config.CustomConfig.SetString(MarkerKey, null); // removes the marker
        }

        /// <summary>
        /// Called once at plugin start: if KeePass saved its configuration while the safeguards
        /// were on, put the user's settings back.
        /// </summary>
        public static void RecoverAfterUnexpectedExit()
        {
            var config = KeePass.Program.Config;
            string marker = config.CustomConfig.GetString(MarkerKey);
            if (marker == null)
            {
                return;
            }

            config.Integration.AutoTypeCancelOnWindowChange = config.CustomConfig.GetBool(MarkerKey, false);

            List<string> filters = config.Integration.AutoTypeAbortOnWindows;
            filters.RemoveAll(IsOwnFilter);

            config.CustomConfig.SetString(MarkerKey, null);
        }

        /// <summary>
        /// KeePass window filter ("//regex//", matched case-insensitively against the trimmed title,
        /// see KeePass Util/AutoType.cs IsMatchWindow) that matches every title except this one.
        /// Returns null for an empty title.
        /// </summary>
        public static string BuildTitleFilter(string title)
        {
            string t = (title ?? string.Empty).Trim();
            if (t.Length == 0)
            {
                return null;
            }

            return FilterPrefix + Regex.Escape(t) + FilterSuffix;
        }

        /// <summary>True for filters created by <see cref="BuildTitleFilter"/>.</summary>
        internal static bool IsOwnFilter(string filter)
        {
            return filter != null &&
                   filter.StartsWith(FilterPrefix, StringComparison.Ordinal) &&
                   filter.EndsWith(FilterSuffix, StringComparison.Ordinal);
        }
    }
}
