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
// PluginOptions.cs - the plugin's global settings (KeePass.config.xml, section <Custom>)
// -------------------------------------------------------------------------------------------------
// KEYS (unchanged since 1.0; do not rename, existing configurations use them)
//   veracrypt.exe.location          - full path of VeraCrypt.exe / veracrypt
//   veracrypt.autotype.wait.timeout - milliseconds
// Mount settings of a volume are NOT stored here but in the entry (EntryStrings).
//
// THE WAIT TIME
//   1.0.x: how long Process.WaitForInputIdle waited before typing blindly (range 3-60 s, default
//          3 s; the dialog saved the value with every OK, so most configurations contain 3000).
//   2.0.0:   only the maximum wait for VeraCrypt's password window: the password is typed as soon
//          as the window is ready, so a longer wait costs nothing. Range 10-60 s, default 10 s;
//          older, smaller values are raised to 10 s, so that a slow VeraCrypt start (first start
//          after boot, virus scanner) does not end with "nothing was typed".
//   The value from the configuration file is always clamped (ClampTimeout), also if edited by hand.
//
// HISTORY
//   1.0.x: CustomConfigKeys.cs + CustomConfigAccessor.cs (extension methods on IPluginHost).
//   2.0.0 (2026-11): this class; same keys.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using KeePass.Plugins;

    internal static class PluginOptions
    {
        public const string KeyExecutable = "veracrypt.exe.location";

        /// <summary>Maximum time (ms) to wait for VeraCrypt's password window.</summary>
        public const string KeyAutoTypeTimeout = "veracrypt.autotype.wait.timeout";

        public const int MinTimeout = 10000;

        public const int MaxTimeout = 60000;

        public const int DefaultTimeout = 10000;

        public static string GetExecutable(IPluginHost host)
        {
            if (host == null)
            {
                return string.Empty;
            }

            return (host.CustomConfig.GetString(KeyExecutable) ?? string.Empty).Trim();
        }

        public static void SetExecutable(IPluginHost host, string path)
        {
            if (host != null)
            {
                host.CustomConfig.SetString(KeyExecutable, (path ?? string.Empty).Trim());
            }
        }

        /// <summary>Always within [<see cref="MinTimeout"/>, <see cref="MaxTimeout"/>], whatever the config file says.</summary>
        public static int GetAutoTypeTimeout(IPluginHost host)
        {
            long value = host != null ? host.CustomConfig.GetLong(KeyAutoTypeTimeout, DefaultTimeout) : DefaultTimeout;
            return ClampTimeout(value);
        }

        public static void SetAutoTypeTimeout(IPluginHost host, int value)
        {
            if (host != null)
            {
                host.CustomConfig.SetLong(KeyAutoTypeTimeout, ClampTimeout(value));
            }
        }

        public static int ClampTimeout(long value)
        {
            if (value < MinTimeout)
            {
                return MinTimeout;
            }

            if (value > MaxTimeout)
            {
                return MaxTimeout;
            }

            return (int)value;
        }
    }
}
