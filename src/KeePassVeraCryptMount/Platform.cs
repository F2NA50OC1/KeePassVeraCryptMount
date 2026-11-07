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
// Platform.cs - Windows, or Linux/macOS (KeePass on Mono)
// -------------------------------------------------------------------------------------------------
// HISTORY
//   1.0.x: Tools.Unix (from Environment.OSVersion).
//   2.0.0 (2026-11): KeePass' own NativeLib.IsUnix().
// =================================================================================================

namespace KeePassVeraCryptMount
{
    using KeePassLib.Native;

    internal static class Platform
    {
        /// <summary>True on Linux/macOS/BSD (KeePass running on Mono).</summary>
        public static readonly bool IsUnix = NativeLib.IsUnix();

        public static TargetPlatform Current
        {
            get { return IsUnix ? TargetPlatform.Unix : TargetPlatform.Windows; }
        }
    }
}
