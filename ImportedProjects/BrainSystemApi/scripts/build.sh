#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DIST="$ROOT/dist"
STAGE="$DIST/BrainSystem"

rm -rf "$DIST"
mkdir -p "$STAGE"
dotnet restore "$ROOT/BrainSystem.sln"
dotnet test "$ROOT/BrainSystem.sln" --configuration Release --no-restore
dotnet publish "$ROOT/src/BrainSystem.Api/BrainSystem.Api.csproj" \
  --configuration Release \
  --output "$STAGE/bin" \
  --no-restore
cp -R "$ROOT/src" "$ROOT/tests" "$ROOT/config" "$ROOT/docs" "$ROOT/models" "$ROOT/scripts" "$STAGE/"
cp "$ROOT/README.md" "$ROOT/BrainSystem.sln" "$ROOT/Directory.Build.props" "$ROOT/Directory.Packages.props" "$STAGE/"
rm -rf "$STAGE"/**/bin "$STAGE"/**/obj "$STAGE"/tests/**/bin "$STAGE"/tests/**/obj 2>/dev/null || true
(cd "$DIST" && zip -r "BrainSystem.zip" "BrainSystem" -x "*/.git/*")
cp "$ROOT/download/index.html" "$DIST/index.html"
sha256sum "$DIST/BrainSystem.zip" > "$DIST/BrainSystem.zip.sha256"
echo "Created $DIST/BrainSystem.zip"