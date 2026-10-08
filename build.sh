#!/usr/bin/env bash
# ---------------------------------------------------------------------------
#  LIKAsys - full build: WPF app (win-x64, self-contained) + NSIS installer
#  Works on Linux and in CI. Usage:  ./build.sh [version]
# ---------------------------------------------------------------------------
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VERSION="${1:-$(cat "$ROOT/VERSION" | tr -d '[:space:]')}"
DOTNET="${DOTNET:-$(command -v dotnet || echo "$HOME/.dotnet-sdk/dotnet")}"
APPOUT="$ROOT/build/app"
DIST="$ROOT/dist"

export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1

echo "=============================================="
echo " LIKAsys $VERSION - build"
echo "=============================================="

# 0. language gate ---------------------------------------------------------
# A repeated key in Lang.cs throws inside a static constructor, which kills the
# app before it draws anything. That shipped once; this stops it shipping twice.
echo "[0/4] kontrolli i gjuhes"
python3 "$(dirname "$0")/scripts/check-lang.py" || exit 1

# 1. branding assets -------------------------------------------------------

if command -v python3 >/dev/null && python3 -c "import PIL" 2>/dev/null; then
  echo "[1/4] ikona + bitmaps"
  python3 "$ROOT/tools/make_branding.py"
else
  echo "[1/4] (kalohet: pillow mungon, perdoren asetet ekzistuese)"
fi

# 2. publish ---------------------------------------------------------------
echo "[2/4] publish .NET (win-x64, self-contained)"
rm -rf "$APPOUT"
"$DOTNET" publish "$ROOT/src/LIKAsys/LIKAsys.csproj" \
  -c Release -r win-x64 --self-contained true \
  -p:Version="$VERSION" -p:AssemblyVersion="$VERSION.0" -p:FileVersion="$VERSION.0" \
  -o "$APPOUT" -v q --nologo

# 3. prune files we never load at runtime ----------------------------------
echo "[3/4] pastrim"
rm -f  "$APPOUT"/*.pdb
rm -f  "$APPOUT"/Dia2Lib.dll
rm -f  "$APPOUT"/Microsoft.DiaSymReader.Native.amd64.dll
rm -f  "$APPOUT"/TraceReloggerLib.dll
rm -f  "$APPOUT"/Mono.Posix.NETStandard.dll
rm -f  "$APPOUT"/MonoPosixHelper.dll
rm -f  "$APPOUT"/libMonoPosixHelper.dll
rm -f  "$APPOUT"/amd64/msdia140.dll
rm -rf "$APPOUT"/x86
du -sh "$APPOUT"

# 4. installer -------------------------------------------------------------
echo "[4/4] installer NSIS"
mkdir -p "$DIST"
rm -f "$DIST/LIKAsys-Setup-$VERSION.exe"
# NSIS version resources must be exactly X.X.X.X
VERSION4="$VERSION"
while [ "$(printf '%s' "$VERSION4" | tr -cd '.' | wc -c)" -lt 3 ]; do VERSION4="$VERSION4.0"; done
( cd "$ROOT/installer" && makensis -V2 -DVERSION="$VERSION" -DVERSION4="$VERSION4" LIKAsys.nsi )

echo
echo "GATI ->  $DIST/LIKAsys-Setup-$VERSION.exe"
ls -lh "$DIST/LIKAsys-Setup-$VERSION.exe"
