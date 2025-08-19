# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a professional clipboard manager application built with WinUI 3 and C# targeting .NET 8.0 and Windows 10/11. The application replaces the default Windows clipboard manager with an enhanced, persistent clipboard history system.

### Project Goals

The application provides a comprehensive clipboard management solution with:
- **Persistent Storage**: All clipboard items (text, images, colors, code, links) are stored in SQLite database for minimal space usage and permanent history
- **Professional UI**: Modern, glass-effect transparent design with intuitive navigation
- **Global Hotkeys**: Customizable keyboard shortcuts (default: Ctrl+Shift+V) to quickly access clipboard history
- **Advanced Search**: Real-time filtering and search capabilities for easy item retrieval
- **Multi-format Support**: Handles all clipboard data types including text, images, colors, code snippets, and URLs

### Architecture

The project follows **MVVM (Model-View-ViewModel)** pattern for clean separation of concerns and maintainable code structure.

### UI Design

- **Left Panel**: Vertical list/drawer showing clipboard history items with search/filter input at top
- **Right Panel**: Detailed view showing complete content of selected clipboard item
- **Visual Effects**: Glass/transparent background effect for modern appearance
- **Responsive Layout**: Adaptive design for different window sizes

## Build Commands

### Build the application
```bash
dotnet build
```

### Build for specific platform
```bash
dotnet build -c Release -r win-x64
dotnet build -c Release -r win-x86
dotnet build -c Release -r win-arm64
```

### Publish the application
```bash
dotnet publish -c Release -r win-x64
dotnet publish -c Release -r win-x86
dotnet publish -c Release -r win-arm64
```

### Run the application
```bash
dotnet run
```

## Development Environment

- **Target Framework**: .NET 8.0 with Windows 10.0.19041.0
- **Minimum Platform Version**: Windows 10.0.17763.0
- **UI Framework**: WinUI 3 (Microsoft.WindowsAppSDK)
- **Packaging**: MSIX with EnableMsixTooling
- **Supported Platforms**: x86, x64, ARM64

## Project Structure

- `App.xaml`/`App.xaml.cs` - Application entry point and lifecycle management
- `MainWindow.xaml`/`MainWindow.xaml.cs` - Main application window
- `Package.appxmanifest` - MSIX package manifest with app metadata and capabilities
- `app.manifest` - Application manifest for Win32 compatibility
- `Assets/` - Application icons and images for different scales
- `Properties/PublishProfiles/` - Platform-specific publish profiles

## Configuration Notes

- The application has `runFullTrust` capability, allowing full system access
- Nullable reference types are enabled
- Publishing is configured for ReadyToRun and trimming in Release builds
- The project uses single-project MSIX packaging tools