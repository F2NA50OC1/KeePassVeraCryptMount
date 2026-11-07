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
// MountMenuItemState.cs - text and visibility of the entry context menu item "Mount volume"
// -------------------------------------------------------------------------------------------------
//   Invisible                  unless exactly one entry with mount settings is selected
//                              (MountSettings.IsEnabledFor).
//   "Mount volume..."          the mount dialog opens first (VisibleWithMountDialog).
//   "Mount volume"             the entry is set to "Hide dialog next time": mounts directly with
//                              the saved settings (VisibleWithoutMountDialog).
//   Ctrl+M always shows the dialog (KeePassVeraCryptMountExt switches to VisibleWithMountDialog).
//
// HISTORY
//   1.0.x: a SHIFT key variant and an "always visible" option (both dropped in 1.0.6; their
//          leftovers stayed in the code).
//   2.0.0 (2026-11): only the three states above; the leftovers removed.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System.Windows.Forms;

    using KeePassLib;

    internal sealed class MountMenuItemState
    {
        private readonly ToolStripMenuItem menuItem;

        public MountMenuItemState(ToolStripMenuItem menuItem)
        {
            this.menuItem = menuItem;
        }

        /// <summary>True if the mount dialog is shown before mounting.</summary>
        public bool ShouldMountDialogShown
        {
            get { return this.Current == MountMenuItemStates.VisibleWithMountDialog; }
        }

        public MountMenuItemStates Current { get; private set; }

        /// <summary>The state for the current selection of the entry list.</summary>
        public static MountMenuItemStates GetState(uint selectedItemCount, PwEntry entry)
        {
            if (selectedItemCount != 1 || !MountSettings.IsEnabledFor(entry))
            {
                return MountMenuItemStates.Invisible;
            }

            return MountSettings.ShouldHideDialogFor(entry)
                ? MountMenuItemStates.VisibleWithoutMountDialog
                : MountMenuItemStates.VisibleWithMountDialog;
        }

        public void ChangeState(MountMenuItemStates state)
        {
            switch (state)
            {
                case MountMenuItemStates.VisibleWithMountDialog:
                    this.menuItem.Visible = true;
                    this.menuItem.Text = LanguageTexts.TCMountMenuItemText + LanguageTexts.MenuItemOpenDialogSuffix;
                    break;

                case MountMenuItemStates.VisibleWithoutMountDialog:
                    this.menuItem.Visible = true;
                    this.menuItem.Text = LanguageTexts.TCMountMenuItemText;
                    break;

                default:
                    this.menuItem.Visible = false;
                    break;
            }

            this.Current = state;
        }
    }

    internal enum MountMenuItemStates
    {
        Invisible,

        VisibleWithMountDialog,

        VisibleWithoutMountDialog,
    }
}
