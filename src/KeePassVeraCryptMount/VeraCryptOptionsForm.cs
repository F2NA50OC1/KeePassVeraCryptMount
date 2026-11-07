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
// VeraCryptOptionsForm.cs - Tools > "VeraCrypt Mount Plugin Options..."
// -------------------------------------------------------------------------------------------------
// Two global settings, stored in KeePass' configuration file (not in the database), see
// PluginOptions: the VeraCrypt program path and the maximum wait for VeraCrypt's password window.
// OK is only enabled while the path points to an existing program. The "find" button looks in the
// usual places (VeraCryptInfo.FindExecutable).
// The dialog contains no links and opens no web pages.
//
// HISTORY
//   1.0.x: the "find" button only looked in the registry (Windows); "Donate" and "Help" links to
//          the original author's pages; a picture of the menu.
//   2.0.0 (2026-11): wait time 10-60 s, search on Linux too, no links, no picture, no icon in
//          the banner.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.IO;
    using System.Windows.Forms;

    using KeePass.Plugins;
    using KeePass.UI;

    internal partial class VeraCryptOptionsForm : Form
    {
        private readonly IPluginHost pluginHost;

        public VeraCryptOptionsForm(IPluginHost host)
        {
            this.pluginHost = host;

            InitializeComponent();

            // Image buttons: short text if a KeePass icon is missing (see Resources).
            this.btnOpenFileDialog.Text = Resources.FallbackText(this.btnOpenFileDialog.Image, "...");
            this.btnRegistryResolve.Text = Resources.FallbackText(this.btnRegistryResolve.Image, "?");
        }

        protected override void OnLoad(EventArgs e)
        {
            if (Resources.FormIcon != null)
            {
                this.Icon = Resources.FormIcon;
            }

            this.tipRegistryButton.SetToolTip(this.btnRegistryResolve, Platform.IsUnix
                ? "Find the VeraCrypt program in the usual folders and the PATH"
                : "Find the installed VeraCrypt program");

            BannerFactory.CreateBannerEx(this,
                this.picBanner,
                null, // no icon: the plugin has none of its own
                LanguageTexts.BannerPluginTitleText,
                LanguageTexts.BannerOptionsDescriptionText);

            this.txtVeraCryptExecutable.Text = PluginOptions.GetExecutable(this.pluginHost);

            int timeout = PluginOptions.GetAutoTypeTimeout(this.pluginHost);
            timeout = Math.Max((int)this.numericAutoTypeTimeout.Minimum, Math.Min((int)this.numericAutoTypeTimeout.Maximum, timeout));
            this.numericAutoTypeTimeout.Value = timeout;

            if (Platform.IsUnix)
            {
                this.ofdVeraCryptExecutable.FileName = "veracrypt";
                this.ofdVeraCryptExecutable.Filter = "VeraCrypt program|veracrypt";
            }

            this.OnVeraCryptPathTextChanged(this, e);

            GlobalWindowManager.AddWindow(this);

            base.OnLoad(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            GlobalWindowManager.RemoveWindow(this);

            base.OnClosing(e);
        }

        private void OnButtonOpenFileDialogClick(object sender, EventArgs e)
        {
            string current = this.txtVeraCryptExecutable.Text.Trim();
            if (current.Length > 0)
            {
                try
                {
                    this.ofdVeraCryptExecutable.FileName = Path.GetFileName(current);
                    this.ofdVeraCryptExecutable.InitialDirectory = Path.GetDirectoryName(current);
                }
                catch (ArgumentException)
                {
                }
                catch (PathTooLongException)
                {
                }
            }

            if (this.ofdVeraCryptExecutable.ShowDialog(this) == DialogResult.OK)
            {
                this.txtVeraCryptExecutable.Text = this.ofdVeraCryptExecutable.FileName;
            }
        }

        private void OnOkButtonClicked(object sender, EventArgs e)
        {
            PluginOptions.SetExecutable(this.pluginHost, this.txtVeraCryptExecutable.Text);
            PluginOptions.SetAutoTypeTimeout(this.pluginHost, (int)this.numericAutoTypeTimeout.Value);
        }

        private void OnVeraCryptPathTextChanged(object sender, EventArgs e)
        {
            bool executableOk = VeraCryptInfo.ExecutableExists(this.txtVeraCryptExecutable.Text.Trim());

            this.txtVeraCryptExecutable.BackColor = executableOk
                ? SystemColors.Window
                : Color.Coral;

            this.btnOk.Enabled = executableOk;
        }

        private void OnRegistryResolveButtonClicked(object sender, EventArgs e)
        {
            string found = VeraCryptInfo.FindExecutable();
            if (found.Length > 0)
            {
                this.txtVeraCryptExecutable.Text = found;
            }
            else
            {
                KeePassLib.Utility.MessageService.ShowInfo(LanguageTexts.ExecutableMissing);
            }
        }
    }
}
