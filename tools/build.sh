#!/bin/bash
# Builds KeePassVeraCryptMount.dll and KeePassVeraCryptMount.plgx (Linux, with Mono + .NET SDK).
# Run it from anywhere:  bash tools/build.sh   (all paths below are relative to the repository root)
#
# Needs: dotnet SDK 8, mono, xvfb-run (KeePass needs a display, even for --plgx-create),
# python3 (header check, PLGX path check), and a KeePass 2.x folder in ./lib/KeePass
# (KeePass.exe from the portable ZIP of keepass.info). Output goes to ./dist.
# Steps and the release checklist: docs/MAINTAINING.md.
set -euo pipefail
cd "$(dirname "$0")/.."

KEEPASS_DIR="${KEEPASS_DIR:-$PWD/lib/KeePass}"
PLGX_MIN_KEEPASS="${PLGX_MIN_KEEPASS:-2.36}"   # first KeePass with Integration.AutoTypeAbortOnWindows (TypingSafeguards)
SRC=src/KeePassVeraCryptMount
OUT=dist

rm -rf "$OUT" build/plgx
mkdir -p "$OUT" build/plgx

echo "== 0. Source files: license header, UTF-8 with BOM, CRLF (fix with: python3 tools/apply-headers.py)"
python3 -I tools/apply-headers.py --check

echo "== 1. DLL (C# 5 language level enforced, same as KeePass' PLGX compiler)"
dotnet build "$SRC/KeePassVeraCryptMount.csproj" -c Release -p:KeePassExe="$KEEPASS_DIR/KeePass.exe" -nologo -v:minimal
cp build/bin/KeePassVeraCryptMount/Release/KeePassVeraCryptMount.dll "$OUT/"

echo "== 2. PLGX (packed by KeePass itself, from a clean copy of the source folder)"
ROOT="$PWD"
STAGE="$ROOT/build/plgx/KeePassVeraCryptMount"
mkdir -p "$STAGE"
# The plugin consists of .cs files and the .csproj only (no resources); extend this if that changes.
(cd "$SRC" && find . -type f \( -name '*.cs' -o -name '*.csproj' \) -exec cp --parents {} "$STAGE/" \;)
cp LICENSE "$STAGE/LICENSE"   # GPL v3 text, as in the repository
xvfb-run -a mono "$KEEPASS_DIR/KeePass.exe" --plgx-create "$STAGE" \
    --plgx-prereq-kp:"$PLGX_MIN_KEEPASS" --plgx-prereq-net:4.0 2>/dev/null || true
test -f build/plgx/KeePassVeraCryptMount.plgx || { echo "PLGX was not created"; exit 1; }
# KeePass writes the build time into the PLGX and ignores it when loading: remove it.
python3 -I tools/plgx.py strip-time build/plgx/KeePassVeraCryptMount.plgx
cp build/plgx/KeePassVeraCryptMount.plgx "$OUT/"

echo "== 3. Check: no file in the PLGX may point outside the plugin folder"
python3 -I tools/plgx.py list "$OUT/KeePassVeraCryptMount.plgx" | tee build/plgx/contents.txt
if grep -E '^  file: (\.\.|/|\\|.*[/\\]\.\.[/\\])' build/plgx/contents.txt; then
    echo "ERROR: suspicious path in PLGX"; exit 1
fi

echo "== 4. Checksums"
(cd "$OUT" && sha256sum KeePassVeraCryptMount.dll KeePassVeraCryptMount.plgx | tee SHA256SUMS.txt)
