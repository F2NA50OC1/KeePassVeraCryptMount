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
// Resources.cs - the button icons of the dialogs: KeePass' own icons
// -------------------------------------------------------------------------------------------------
// The plugin ships no images and has no icon of its own: its menu items and the banners of its
// dialogs show none, and KeePassVeraCryptMountExt does not override SmallIcon (KeePass' Plugins
// dialog then shows its standard plugin icon). The few icons below are taken at run time from
// KeePass' resource manager (IPluginHost.Resources; names from KeePass' Properties/Resources.resx,
// present since at least KeePass 2.36):
//   FolderIcon B16x16_Folder_Yellow_Open  "browse" buttons
//   ReloadIcon B16x16_Reload_Page         "reload drive letters" button
//   FindIcon   B16x16_XMag                "find VeraCrypt" button
//   FormIcon   KeePass' own window icon   title bar of both dialogs
// If a future KeePass renames an icon, it is simply missing: the button shows a short text
// instead (FallbackText). Nothing fails.
// Icons are fetched once and kept for the plugin's lifetime (KeePass loads a plugin once; its
// resource manager would create a new bitmap on every call).
//
// HISTORY
//   1.0.x: Resources.resx with VeraCrypt logos, a picture of the menu, a copy of a KeePass icon and
//          two icons under non-commercial licenses (see LicenseInfo.txt).
//   2.0.0 (2026-11): no images and no icon of its own; KeePass' icons on the small buttons only
//          (neutral look, no licensing questions, nothing to keep up to date).
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System.Collections.Generic;
    using System.Drawing;

    using KeePass.Plugins;

    internal static class Resources
    {
        private static readonly Dictionary<string, Image> Cache = new Dictionary<string, Image>();
        private static IPluginHost host;

        /// <summary>Called by the plugin's Initialize (before any menu item or dialog is created).</summary>
        public static void Initialize(IPluginHost pluginHost)
        {
            host = pluginHost;
        }

        public static Image FolderIcon
        {
            get { return FromKeePass("B16x16_Folder_Yellow_Open"); }
        }

        public static Image ReloadIcon
        {
            get { return FromKeePass("B16x16_Reload_Page"); }
        }

        public static Image FindIcon
        {
            get { return FromKeePass("B16x16_XMag"); }
        }

        /// <summary>KeePass' own window icon (null if not available).</summary>
        public static Icon FormIcon
        {
            get
            {
                try
                {
                    return (host != null && host.MainWindow != null) ? host.MainWindow.Icon : null;
                }
                catch (System.Exception)
                {
                    return null;
                }
            }
        }

        /// <summary>Text for an image button whose KeePass icon is missing (future KeePass versions).</summary>
        public static string FallbackText(Image image, string text)
        {
            return image == null ? text : string.Empty;
        }

        private static Image FromKeePass(string name)
        {
            lock (Cache)
            {
                Image image;
                if (Cache.TryGetValue(name, out image))
                {
                    return image;
                }

                try
                {
                    image = host != null ? host.Resources.GetObject(name) as Image : null;
                }
                catch (System.Exception)
                {
                    image = null;
                }

                if (image != null)
                {
                    Cache[name] = image;
                }

                return image;
            }
        }
    }
}
