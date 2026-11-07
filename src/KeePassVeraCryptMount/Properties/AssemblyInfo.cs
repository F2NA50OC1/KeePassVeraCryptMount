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
// AssemblyInfo.cs - name, author, version shown in KeePass' Plugins dialog
// -------------------------------------------------------------------------------------------------
// GUID: only used as a COM type library ID (KeePass does not use it). Derived from the project
//   name, so it is the same for everybody and says nothing about who built the plugin:
//   python3 -c 'import uuid; print(uuid.uuid5(uuid.NAMESPACE_DNS, "KeePassVeraCryptMount"))'
//
// HISTORY
//   1.0.x: version "1.0.*" (a different number on every build); a random GUID from the original
//          Visual Studio project.
//   2.0.0 (2026-11): fixed version 2.0.0.0; deterministic builds (tools/build.sh); GUID derived
//          from the project name; author field "KeePassVeraCryptMount contributors" (the
//          copyright line keeps every author, as the GPL requires); license GPL v3 or later
//          (1.0.x: GPL v2 or later).
// =================================================================================================

using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// KeePass' Plugins dialog shows AssemblyTitle as the name, AssemblyDescription, AssemblyCompany
// as the author and AssemblyFileVersion as the version.
[assembly: AssemblyTitle("KeePassVeraCryptMount")]
[assembly: AssemblyDescription("Mounts VeraCrypt volumes with the password of a KeePass entry")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("KeePassVeraCryptMount contributors")]
[assembly: AssemblyProduct("KeePass Plugin")]
[assembly: AssemblyCopyright("Copyright © 2015-2017 Frédéric Bourqui, 2010-2013 Patrick Schaller, 2026 KeePassVeraCryptMount contributors; GPL v3 or later")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

[assembly: ComVisible(false)]
[assembly: Guid("214032c3-f875-5b79-8b05-926c2a6fa33d")]

// Fixed version numbers (the old "1.0.*" produced a different version on every build).
// Release checklist: docs/MAINTAINING.md.
[assembly: AssemblyVersion("2.0.0.0")]
[assembly: AssemblyFileVersion("2.0.0.0")]

// Lets the separate test program check the internal logic. Has no effect in KeePass.
[assembly: InternalsVisibleTo("KeePassVeraCryptMount.Tests")]
