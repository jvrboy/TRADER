#!/bin/bash
# Java Sandbox System - Test Runner
set -e

cd "$(dirname "$0")/.."
BASE_DIR=$(pwd)


echo "========================================"
echo "  Java Sandbox System - Build & Test"
echo "========================================"

# Check Maven
if ! command -v mvn &> /dev/null; then
    echo "[ERROR] Maven not found. Please install Maven 3.6+"
    exit 1
fi

echo ""
echo "[1/4] Compiling..."
mvn compile -q
if [ $? -ne 0 ]; then
    echo "[FAIL] Compilation failed"
    exit 1
fi
echo "[OK]   Compilation successful"

echo ""
echo "[2/4] Running tests..."
mvn test -q 2>&1
if [ $? -ne 0 ]; then
    echo "[FAIL] Tests failed"
    exit 1
fi
echo "[OK]   All tests passed"

echo ""
echo "[3/4] Building fat JAR..."
mvn package -q -DskipTests
if [ $? -ne 0 ]; then
    echo "[FAIL] Package failed"
    exit 1
fi
echo "[OK]   Build successful"

echo ""
echo "[4/4] Running built-in tests..."
java -jar target/java-sandbox-all.jar --test --no-color
if [ $? -ne 0 ]; then
    echo "[FAIL] Built-in tests failed"
    exit 1
fi
echo "[OK]   Built-in tests passed"

echo ""
echo "========================================"
echo "  ALL TESTS PASSED"
echo "========================================"
