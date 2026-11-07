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
// VeraCryptMountForm.AutoDriveLetterComboBoxItem.cs - the "<Auto Select>" drive list item
// -------------------------------------------------------------------------------------------------
// Selected = no drive letter is passed: VeraCrypt takes the first free letter (Linux: first free
// slot).
//
// HISTORY
//   1.0.x: this file, unchanged.
// =================================================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KeePassVeraCryptMount
{
    internal partial class VeraCryptMountForm
    {
        internal class AutoDriveLetterComboBoxItem
        {
            public override string ToString()
            {
                return "<Auto Select>";
            }
        }
    }
}
