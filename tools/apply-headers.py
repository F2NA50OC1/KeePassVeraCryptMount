#!/usr/bin/env python3
"""Puts the project's license header at the top of every C# source file and saves the files
as UTF-8 with BOM and CRLF line endings.

Why: KeePass compiles a .plgx with the C# compiler of the .NET Framework on Windows. Without a
BOM that compiler may read non-ASCII characters (e.g. the "e" with accent in the authors' names
in AssemblyInfo.cs) in the Windows code page instead of UTF-8. CRLF is what Visual Studio and
the original project use.

Run from the repository root:   python3 tools/apply-headers.py
tools/build.sh runs it with --check, which only reports files that would change (exit code 1).

A file may start with:
  * "// @@LICENSE@@"  - placeholder for new files; replaced by the header;
  * an older license comment ("/* ... */" at the very top, mentioning the GPL) - replaced;
  * the header itself - left as it is (the script is idempotent).
"""
import os
import sys

HEADER = """/*
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
"""

PLACEHOLDER = "// @@LICENSE@@"
FOLDERS = ["src", "tests"]


def with_header(text):
    body = text.replace("\r\n", "\n").lstrip("﻿")
    stripped = body.lstrip()
    if stripped.startswith(HEADER):
        return stripped
    if stripped.startswith(PLACEHOLDER):
        rest = stripped[len(PLACEHOLDER):]
    elif stripped.startswith("/*") and "*/" in stripped:
        end = stripped.index("*/") + 2
        comment = stripped[:end]
        if "General Public License" not in comment and "GNU GPL" not in comment:
            rest = "\n" + stripped  # some other comment: keep it
        else:
            rest = stripped[end:]
    else:
        rest = "\n" + stripped
    return HEADER + "\n" + rest.lstrip("\n")


def normalised(text):
    text = text.replace("\r\n", "\n").rstrip("\n") + "\n"
    return "﻿" + text.replace("\n", "\r\n")


def main():
    check = "--check" in sys.argv[1:]
    root = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
    changed = []
    for folder in FOLDERS:
        for directory, _, files in os.walk(os.path.join(root, folder)):
            if os.sep + "obj" in directory or os.sep + "bin" in directory:
                continue
            for name in sorted(files):
                if not name.endswith(".cs"):
                    continue
                path = os.path.join(directory, name)
                with open(path, "rb") as f:
                    raw = f.read()
                new = normalised(with_header(raw.decode("utf-8-sig"))).encode("utf-8")
                if new != raw:
                    changed.append(os.path.relpath(path, root))
                    if not check:
                        with open(path, "wb") as f:
                            f.write(new)
    for path in changed:
        print(("needs header/format: " if check else "updated: ") + path)
    if check and changed:
        print("Run: python3 tools/apply-headers.py")
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
