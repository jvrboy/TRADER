#!/bin/bash
# Java Sandbox System - Quick Run Script
set -e

cd "$(dirname "$0")/.."
BASE_DIR=$(pwd)
JAR_FILE="$BASE_DIR/target/java-sandbox-all.jar"

# Build if needed
if [ ! -f "$JAR_FILE" ]; then
    echo "[BUILD] Building sandbox..."
    mvn package -q -DskipTests
fi

# Run with args or interactive
if [ $# -eq 0 ]; then
    echo "Starting Java Sandbox (interactive mode)..."
    echo "Press Ctrl+C or type 'exit' to quit."
    echo ""
    java -jar "$JAR_FILE" "$@"
else
    java -jar "$JAR_FILE" "$@"
fi
