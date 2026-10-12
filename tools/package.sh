#!/usr/bin/env bash
# Build Good Hunting 2 and produce the distributable mod zip.
#
# Usage: tools/package.sh [version]
# Output: dist/<modid>_<version>.zip
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

DOTNET="${DOTNET:-$HOME/.dotnet/dotnet}"
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1

MODID="$(python3 -c "import json;print(json.load(open('modinfo.json'))['modid'])")"
VERSION="$(python3 -c "import json;print(json.load(open('modinfo.json'))['version'])")"
VERSION="${1:-$VERSION}"

echo "== build (Release) =="
"$DOTNET" build -c Release

STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT

cp modinfo.json "$STAGE/"
cp bin/Release/GoodHunting.dll "$STAGE/"
mkdir -p "$STAGE/assets"
cp -r assets/. "$STAGE/assets/"

mkdir -p dist
OUT="$ROOT/dist/${MODID}_${VERSION}.zip"
rm -f "$OUT"
( cd "$STAGE" && zip -r -X -q "$OUT" . )

echo "== packaged =="
unzip -l "$OUT"
echo "$OUT"
