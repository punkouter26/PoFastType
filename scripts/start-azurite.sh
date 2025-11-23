#!/bin/bash
# Start Azurite for local Azure Storage emulation

echo "Starting Azurite (Azure Storage Emulator)..."

# Check if Azurite is installed
if ! command -v azurite &> /dev/null
then
    echo "Azurite is not installed. Installing via npm..."
    npm install -g azurite
fi

# Start Azurite with default settings
# - Blob storage on port 10000
# - Queue storage on port 10001
# - Table storage on port 10002
azurite --silent --location azurite-data --debug azurite-debug.log

echo "Azurite started successfully!"
echo "Blob endpoint: http://127.0.0.1:10000"
echo "Queue endpoint: http://127.0.0.1:10001"
echo "Table endpoint: http://127.0.0.1:10002"
