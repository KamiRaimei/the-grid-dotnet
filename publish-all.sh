#!/bin/bash
echo "Building Grid Simulation for all platforms..."

# Clean previous builds
rm -rf ./publish

# Windows
echo "Building for Windows..."
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o ./publish/windows

# Linux
echo "Building for Linux..."
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishSingleFile=true -o ./publish/linux

# macOS
echo "Building for macOS..."
dotnet publish -c Release -r osx-x64 --self-contained true -p:PublishSingleFile=true -o ./publish/macos
dotnet publish -c Release -r osx-arm64 --self-contained true -p:PublishSingleFile=true -o ./publish/macos-arm64

echo "All builds complete!"
echo "Windows: ./publish/windows/GridSimulation.exe"
echo "Linux: ./publish/linux/GridSimulation"
echo "macOS Intel: ./publish/macos/GridSimulation"
echo "macOS Apple Silicon: ./publish/macos-arm64/GridSimulation"./publish/macos-arm64/GridSimulation