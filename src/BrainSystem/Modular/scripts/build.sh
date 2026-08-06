#!/bin/bash
# BrainSystem v1.0 - Build Script (Bash)
# Restores NuGet packages, builds the solution, and runs tests.

set -e

CONFIGURATION="${1:-Release}"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SOLUTION_PATH="$SCRIPT_DIR/../BrainSystem.sln"

echo "========================================"
echo "  BrainSystem Build Script"
echo "========================================"
echo ""

# Step 1: Restore
echo "[1/4] Restoring NuGet packages..."
dotnet restore "$SOLUTION_PATH"
echo "Restore complete."
echo ""

# Step 2: Build
echo "[2/4] Building solution ($CONFIGURATION)..."
dotnet build "$SOLUTION_PATH" -c "$CONFIGURATION" --no-restore
echo "Build complete."
echo ""

# Step 3: Test
echo "[3/4] Running tests..."
dotnet test "$SOLUTION_PATH" -c "$CONFIGURATION" --no-build --logger "console;verbosity=normal"
echo "All tests passed."
echo ""

# Step 4: Package
if [ "$2" == "--package" ]; then
    echo "[4/4] Creating ZIP package..."
    OUTPUT_DIR="$SCRIPT_DIR/../dist"
    rm -rf "$OUTPUT_DIR"
    mkdir -p "$OUTPUT_DIR"

    dotnet publish "$SCRIPT_DIR/../src/Brain.API/Brain.API.csproj" -c "$CONFIGURATION" -o "$OUTPUT_DIR/publish/api" --no-build
    dotnet publish "$SCRIPT_DIR/../src/Brain.Launcher/Brain.Launcher.csproj" -c "$CONFIGURATION" -o "$OUTPUT_DIR/publish/launcher" --no-build

    cd "$OUTPUT_DIR"
    zip -r "BrainSystem.zip" .
    echo "ZIP created: $OUTPUT_DIR/BrainSystem.zip"
    echo "SHA-256: $(sha256sum BrainSystem.zip | cut -d' ' -f1)"
else
    echo "[4/4] Packaging skipped (use --package to create ZIP)"
fi
echo ""

echo "========================================"
echo "  Build Complete!"
echo "========================================"
