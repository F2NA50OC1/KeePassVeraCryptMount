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
// KeePassVeraCryptMountExt.cs - the plugin class KeePass loads (menus, shortcut, dialogs)
// -------------------------------------------------------------------------------------------------
// KEEPASS PLUGIN RULES (do not change the names)
//   KeePass looks for the type "<AssemblyName>.<AssemblyName>Ext" deriving from KeePass.Plugins.
//   Plugin: assembly/file KeePassVeraCryptMount(.dll/.plgx), namespace KeePassVeraCryptMount,
//   class KeePassVeraCryptMountExt.
//
// USER INTERFACE (menu items and Ctrl+M as since 1.0.6)
//   * Tools > "VeraCrypt Mount Plugin Options...": VeraCryptOptionsForm.
//   * Entry context menu, first item "Mount volume" (with "..." when the dialog will be shown):
//     visible for one selected entry that has mount settings, see MountMenuItemState. If the
//     VeraCrypt program is not found, clicking it says where to set the path (MountController).
//   * No icon of its own: the menu items show none, and SmallIcon is not overridden, so KeePass'
//     Plugins dialog shows its standard plugin icon, as for every plugin without an icon.
//   * Ctrl+M: always opens the mount dialog for the selected entry, also for entries without
//     settings. Implemented as a second, invisible context menu item that only carries the
//     shortcut (WinForms processes shortcuts of invisible items of the list's context menu).
//
// SAVING
//   Mount settings are saved through SaveIfChanged only: nothing is written if nothing changed;
//   otherwise a history backup is created first (like KeePass' own entry editor), then the entry is
//   updated and its database marked as modified. The user saves the database as usual.
//
// HISTORY
//   1.0.x: dialog changes were only saved with "Apply"; "Mount" used the old saved settings; the
//          active database was assumed; the Ctrl+M item's Click handler was not removed on exit;
//          an UpdateUrl pointed to the original project's version file; the mount item was
//          disabled with an error icon while VeraCrypt was not found (without saying why); menus
//          and the Plugins dialog showed a VeraCrypt logo.
//   2.0.0 (2026-11): "Mount" saves and mounts what the dialog shows, "Apply" saves immediately;
//          the database that contains the entry is used; all handlers removed on exit; no
//          UpdateUrl (no server is contacted); unexpected exceptions are shown as a message
//          instead of reaching KeePass; no icon of its own; the mount item always enabled.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;
    using System.Windows.Forms;

    using KeePass.Plugins;

    using KeePassLib;

    public sealed class KeePassVeraCryptMountExt : Plugin
    {
        private IPluginHost pluginHost;
        private ToolStripMenuItem mnuEntryVeraCryptMount;
        private ToolStripMenuItem mnuEntryVeraCryptEdit;
        private ToolStripMenuItem mnuToolsVeraCryptOptions;
        private MountMenuItemState mountMenuItemState;
        private MountController mountController;

        // No SmallIcon and no UpdateUrl override: see the header above.

        public override bool Initialize(IPluginHost host)
        {
            if (host == null)
            {
                return false;
            }

            this.pluginHost = host;
            Resources.Initialize(host);
            TypingSafeguards.RecoverAfterUnexpectedExit();
            this.mountController = new MountController(host);

            this.mnuToolsVeraCryptOptions = new ToolStripMenuItem(LanguageTexts.TCOptionsMenuItemText + LanguageTexts.MenuItemOpenDialogSuffix);
            this.mnuToolsVeraCryptOptions.Click += this.OnVeraCryptOptionsMenuItemClicked;
            this.pluginHost.MainWindow.ToolsMenu.DropDownItems.Add(this.mnuToolsVeraCryptOptions);

            this.pluginHost.MainWindow.EntryContextMenu.Opened += this.OnEntryContextMenuOpened;
            this.mnuEntryVeraCryptMount = new ToolStripMenuItem();
            this.mnuEntryVeraCryptMount.ShowShortcutKeys = true;
            this.mnuEntryVeraCryptMount.ShortcutKeyDisplayString = "Ctrl+M";
            this.mnuEntryVeraCryptMount.Click += this.OnVeraCryptMenuItemClicked;
            this.pluginHost.MainWindow.EntryContextMenu.Items.Insert(0, this.mnuEntryVeraCryptMount);

            this.mnuEntryVeraCryptEdit = new ToolStripMenuItem();
            this.mnuEntryVeraCryptEdit.ShortcutKeys = Keys.Control | Keys.M;
            this.mnuEntryVeraCryptEdit.ShowShortcutKeys = false;
            this.mnuEntryVeraCryptEdit.Click += this.OnVeraCryptEditItemClicked;
            this.pluginHost.MainWindow.EntryContextMenu.Items.Insert(1, this.mnuEntryVeraCryptEdit);

            this.mountMenuItemState = new MountMenuItemState(this.mnuEntryVeraCryptMount);
            this.mountMenuItemState.ChangeState(MountMenuItemStates.Invisible);
            this.mnuEntryVeraCryptEdit.Visible = false;

            return true;
        }

        public override void Terminate()
        {
            // KeePass unloads plugins before it saves its configuration: undo temporary settings first.
            TypingSafeguards.Restore();

            if (this.pluginHost == null)
            {
                return;
            }

            if (this.mountController != null)
            {
                this.mountController.Dispose();
                this.mountController = null;
            }

            this.pluginHost.MainWindow.ToolsMenu.DropDownItems.Remove(this.mnuToolsVeraCryptOptions);
            this.mnuToolsVeraCryptOptions.Click -= this.OnVeraCryptOptionsMenuItemClicked;
            this.mnuToolsVeraCryptOptions.Dispose();

            this.pluginHost.MainWindow.EntryContextMenu.Opened -= this.OnEntryContextMenuOpened;
            this.pluginHost.MainWindow.EntryContextMenu.Items.Remove(this.mnuEntryVeraCryptMount);
            this.mnuEntryVeraCryptMount.Click -= this.OnVeraCryptMenuItemClicked;
            this.mnuEntryVeraCryptMount.Dispose();

            this.pluginHost.MainWindow.EntryContextMenu.Items.Remove(this.mnuEntryVeraCryptEdit);
            this.mnuEntryVeraCryptEdit.Click -= this.OnVeraCryptEditItemClicked;
            this.mnuEntryVeraCryptEdit.Dispose();

            base.Terminate();
        }

        private void OnEntryContextMenuOpened(object sender, EventArgs e)
        {
            var state = MountMenuItemState.GetState(
                this.pluginHost.MainWindow.GetSelectedEntriesCount(),
                this.pluginHost.MainWindow.GetSelectedEntry(false));

            this.mountMenuItemState.ChangeState(state);
        }

        /// <summary>Ctrl+M: force the dialog, then continue like a click on "Mount volume".</summary>
        private void OnVeraCryptEditItemClicked(object sender, EventArgs e)
        {
            this.mountMenuItemState.ChangeState(MountMenuItemStates.VisibleWithMountDialog);
            this.OnVeraCryptMenuItemClicked(sender, e);
        }

        private void OnVeraCryptMenuItemClicked(object sender, EventArgs e)
        {
            try
            {
                this.MountSelectedEntry();
            }
            catch (Exception ex)
            {
                // Never let an unexpected error reach KeePass.
                KeePassLib.Utility.MessageService.ShowWarning(LanguageTexts.MountErrorTitle, ex);
            }
        }

        private void MountSelectedEntry()
        {
            if (this.pluginHost.MainWindow.GetSelectedEntriesCount() != 1)
            {
                return;
            }

            PwEntry entry = this.pluginHost.MainWindow.GetSelectedEntry(false);
            if (entry == null)
            {
                return;
            }

            // The database that contains the entry (normally the active one).
            PwDatabase database = this.pluginHost.MainWindow.DocumentManager.SafeFindContainerOf(entry);
            if (database == null || !database.IsOpen)
            {
                return;
            }

            if (this.mountMenuItemState.ShouldMountDialogShown)
            {
                using (var dialog = new VeraCryptMountForm(settings => this.SaveIfChanged(entry, database, settings)))
                {
                    dialog.ReadFrom(entry);
                    if (dialog.ShowDialog(this.pluginHost.MainWindow) != DialogResult.OK)
                    {
                        return; // "Cancel": nothing more is saved ("Apply" may have saved before)
                    }

                    this.SaveIfChanged(entry, database, dialog.CurrentSettings);
                }
            }

            this.mountController.Mount(entry, database);
        }

        private void SaveIfChanged(PwEntry entry, PwDatabase database, MountSettings settings)
        {
            if (settings == null)
            {
                return;
            }

            // Try on a copy first, so that a history backup is only created for a real change.
            PwEntry probe = entry.CloneDeep();
            if (!settings.WriteTo(probe))
            {
                return;
            }

            entry.CreateBackup(database);
            settings.WriteTo(entry);
            entry.Touch(true, false);
            database.Modified = true;
            this.pluginHost.MainWindow.UpdateUI(false, null, false, null, true, null, false);
        }

        private void OnVeraCryptOptionsMenuItemClicked(object sender, EventArgs e)
        {
            using (var optionsDialog = new VeraCryptOptionsForm(this.pluginHost))
            {
                optionsDialog.ShowDialog(this.pluginHost.MainWindow);
            }
        }
    }
}
