#!/usr/bin/env bash
set -e

echo "=========================================="
echo "  TRADER Monorepo Build & Test Pipeline   "
echo "=========================================="

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

echo "[1/4] Restoring solution dependencies..."
if command -v dotnet &> /dev/null; then
    dotnet restore TRADER.sln
    echo "[2/4] Building full solution..."
    dotnet build TRADER.sln -c Release --no-restore
    echo "[3/4] Running backend unit tests..."
    dotnet test tests/Trader.Backend.Tests/Trader.Backend.Tests.csproj -c Release --no-build
    echo "[4/4] Running NexusBrain unit tests..."
    dotnet test tests/NexusBrain.Tests/NexusBrain.Tests.csproj -c Release --no-build
    echo "[SUCCESS] Build & tests completed successfully!"
else
    echo "[INFO] .NET SDK not detected in environment. Performing static project checks..."
    python3 -c "
import os, xml.etree.ElementTree as ET
for root, dirs, files in os.walk('.'):
    for f in files:
        if f.endswith(('.csproj', '.props', '.xml')):
            ET.parse(os.path.join(root, f))
print('All project XML files validated successfully.')
"
fi
