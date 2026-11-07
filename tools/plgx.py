#!/usr/bin/env python3
"""Small PLGX helper (KeePass plugin package format, see KeePass/Plugins/PlgxPlugin.cs).

  plgx.py list       FILE.plgx                 -> header fields + stored file paths
  plgx.py add        IN.plgx OUT.plgx PATH SRC -> copy IN, adding SRC stored under PATH
                                                 (used to reproduce the 1.0.7 layout)
  plgx.py strip-time FILE.plgx                 -> remove the creation time KeePass writes
                                                 (KeePass ignores it when loading; tools/build.sh
                                                 removes it so a release does not record when
                                                 it was built)
"""
import gzip
import struct
import sys

SIG1, SIG2 = 0x65D90719, 0x3DDD0503
NAMES = {0: "EOF", 1: "FileUuid", 2: "BaseFileName", 3: "BeginContent", 4: "File",
         5: "EndContent", 6: "CreationTime", 7: "GeneratorName", 8: "GeneratorVersion",
         9: "PrereqKP", 10: "PrereqNet", 11: "PrereqOS", 12: "PrereqPtr",
         13: "BuildPre", 14: "BuildPost"}


def read_objects(buf, pos):
    while pos < len(buf):
        t, n = struct.unpack_from("<HI", buf, pos)
        pos += 6
        data = buf[pos:pos + n]
        pos += n
        yield t, data
        if t == 0:
            return


def parse_file_object(data):
    path, content = None, None
    for t, d in read_objects(data, 0):
        if t == 1:
            path = d.decode("utf-8")
        elif t == 2:
            content = gzip.decompress(d) if d else b""
    return path, content


def version_str(data):
    v = struct.unpack("<Q", data)[0]
    return ".".join(str((v >> s) & 0xFFFF) for s in (48, 32, 16, 0))


def cmd_list(fn):
    buf = open(fn, "rb").read()
    s1, s2, ver = struct.unpack_from("<III", buf, 0)
    assert (s1, s2) == (SIG1, SIG2), "not a PLGX file"
    print("PLGX format version 0x%08x" % ver)
    for t, d in read_objects(buf, 12):
        name = NAMES.get(t, str(t))
        if t == 4:
            p, c = parse_file_object(d)
            print("  file: %-60s %7d bytes" % (p, len(c)))
        elif t in (2, 6, 7, 11, 13, 14):
            print("%s: %s" % (name, d.decode("utf-8")))
        elif t in (8, 9, 10):
            print("%s: %s" % (name, version_str(d)))
        elif t not in (0, 3, 5):
            print("%s: %s" % (name, d.hex()))


def file_object(path, content):
    out = b""
    p = path.encode("utf-8")
    out += struct.pack("<HI", 1, len(p)) + p
    z = gzip.compress(content, mtime=0) if content else b""
    out += struct.pack("<HI", 2, len(z)) + z
    out += struct.pack("<HI", 0, 0)
    return out


def cmd_add(fin, fout, stored_path, src):
    buf = open(fin, "rb").read()
    out = bytearray(buf[:12])
    content = open(src, "rb").read()
    for t, d in read_objects(buf, 12):
        if t == 5:  # EndContent: insert the extra file just before it
            fo = file_object(stored_path, content)
            out += struct.pack("<HI", 4, len(fo)) + fo
        out += struct.pack("<HI", t, len(d)) + d
    open(fout, "wb").write(out)


def cmd_strip_time(fn):
    buf = open(fn, "rb").read()
    s1, s2, _ = struct.unpack_from("<III", buf, 0)
    assert (s1, s2) == (SIG1, SIG2), "not a PLGX file"
    out = bytearray(buf[:12])
    for t, d in read_objects(buf, 12):
        if t == 6:  # CreationTime (KeePass: "Ignore" when reading, Plugins/PlgxPlugin.cs)
            continue
        out += struct.pack("<HI", t, len(d)) + d
    open(fn, "wb").write(out)


if __name__ == "__main__":
    if sys.argv[1] == "list":
        cmd_list(sys.argv[2])
    elif sys.argv[1] == "add":
        cmd_add(*sys.argv[2:6])
    elif sys.argv[1] == "strip-time":
        cmd_strip_time(sys.argv[2])
    else:
        sys.exit(__doc__)
