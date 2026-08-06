#!/usr/bin/env bash
# NexusBrain build & package script
set -euo pipefail
cd "$(dirname "$0")"

echo "=== Building NexusBrain (Release) ==="
dotnet build -c Release

echo ""
echo "=== Running test suite ==="
dotnet run --project tests/NexusBrain.Tests -c Release

echo ""
echo "=== Publishing CLI (self-contained) ==="
dotnet publish src/NexusBrain.Cli -c Release -r "$(dotnet --info | grep -i 'RID:' | head -1 | awk '{print $2}')" --self-contained true -o dist/cli

echo ""
echo "=== Done ==="
echo "Run the CLI:"
echo "  ./dist/cli/nexusbrain help"
