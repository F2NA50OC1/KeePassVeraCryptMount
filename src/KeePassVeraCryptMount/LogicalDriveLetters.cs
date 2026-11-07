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
// LogicalDriveLetters.cs - drive letters for the drive list of the mount dialog
// -------------------------------------------------------------------------------------------------
// FreeDriveLetters = the letters not in use right now (Windows). The dialog adds the entry's own
// letter even if it is in use at that moment (VeraCryptMountForm.FillDriveLetters).
// On Linux the "drive letter" means a VeraCrypt slot: A..Z = slot 1..26 (VeraCryptArguments).
//
// HISTORY
//   1.0.x: this file.
//   2.0.0 (2026-11): unused helper (EnsureIsDrive) removed.
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    internal static class LogicalDriveLetters
    {
        /// <summary>"A:\" ... "Z:\" (format of DriveInfo.Name on Windows).</summary>
        public static readonly string[] AllDriveLetters =
        {
            "A:\\", "B:\\", "C:\\", "D:\\", "E:\\", "F:\\", "G:\\", "H:\\", "I:\\",
            "J:\\", "K:\\", "L:\\", "M:\\", "N:\\", "O:\\", "P:\\", "Q:\\", "R:\\",
            "S:\\", "T:\\", "U:\\", "V:\\", "W:\\", "X:\\", "Y:\\", "Z:\\",
        };

        /// <summary>The drive letters not in use right now.</summary>
        public static IEnumerable<string> FreeDriveLetters
        {
            get
            {
                string[] usedDriveLetters;
                try
                {
                    usedDriveLetters = DriveInfo.GetDrives().Select(di => di.Name).ToArray();
                }
                catch (IOException)
                {
                    return AllDriveLetters;
                }
                catch (UnauthorizedAccessException)
                {
                    return AllDriveLetters;
                }

                return AllDriveLetters.Except(usedDriveLetters, StringComparer.OrdinalIgnoreCase);
            }
        }
    }
}
