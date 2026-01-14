# HyZaap - Hytale Server Manager

<div align="center">

![HyZaap Logo](https://via.placeholder.com/200x200/1a1a2e/ffffff?text=HyZaap)

**A powerful, user-friendly desktop application for managing Hytale game servers**

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Windows](https://img.shields.io/badge/Platform-Windows-0078D6?logo=windows)](https://www.microsoft.com/windows)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE.txt)

[Features](#-features) • [Installation](#-installation) • [Usage](#-usage) • [Building](#-building) • [Contributing](#-contributing)

</div>

---

## 📖 Overview

HyZaap is a comprehensive Windows desktop application designed to simplify the setup, configuration, and management of Hytale game servers. Built with WPF and .NET 10.0, it provides an intuitive interface for server administrators to handle multiple server instances with ease.

### Why HyZaap?

- 🚀 **Quick Setup**: Automated server installation wizard guides you through the entire process
- ☕ **Java Management**: Automatic Java 25 download and installation
- 🎮 **Multi-Server Support**: Manage multiple Hytale server instances from one interface
- 🖥️ **Embedded Console**: Real-time server console output and command execution
- ⚙️ **Easy Configuration**: User-friendly settings editor with advanced JSON config support
- 🔐 **Authentication**: Streamlined OAuth2 device authentication flow
- 🎨 **Modern UI**: Beautiful dark theme inspired by Hytale's design language

---

## ✨ Features

### Server Management
- **Multi-Server Support**: Create and manage multiple Hytale server instances
- **Start/Stop/Restart**: Control server lifecycle with a single click
- **Process Reattachment**: Automatically reconnects to servers if the manager is closed and reopened
- **Server Deletion**: Safely remove server instances with confirmation

### Setup Wizard
- **Step-by-Step Guide**: Intuitive wizard walks you through server setup
- **Java Detection**: Automatically checks for Java 25 installation
- **Java Download**: One-click download and extraction of Java 25 from Adoptium
- **Server Download**: Supports both Hytale Downloader CLI and manual server file copy
- **Configuration**: Set port, bind address, memory limits, and more during setup

### Console & Monitoring
- **Real-Time Output**: Live console output with auto-scrolling
- **Command Execution**: Send commands directly to the server console
- **Log Persistence**: Console logs persist across sessions
- **Clear Console**: Clear console output while keeping server running

### Configuration
- **Easy Mode**: User-friendly settings editor for common configurations
  - Server name, port, bind address
  - Memory allocation (min/max)
  - PvP, fall damage, and gameplay settings
  - Backup configuration
  - Authentication mode
- **Advanced Mode**: Direct JSON editor for `config.json` with validation
- **Save & Restart**: Apply configuration changes and restart server automatically

### Authentication
- **OAuth2 Device Flow**: Streamlined authentication process
- **Auto-Open URL**: Automatically opens authentication URL in browser
- **Profile Selection**: Automatically selects the first available profile after authentication
- **Session Management**: Handles authentication tokens and session state

### Additional Features
- **Dark Theme**: Beautiful Hytale-inspired dark color scheme
- **Custom Scrollbars**: Styled scrollbars matching the application theme
- **Tab Navigation**: Easy switching between console and settings views
- **Status Indicators**: Visual indicators for server running status
- **Auto-Scroll**: Console automatically scrolls to show latest output

---

## 📋 Requirements

- **Operating System**: Windows 10 or later
- **.NET Runtime**: .NET 10.0 Desktop Runtime
- **Java**: Java 25 (automatically downloaded if not found)
- **Disk Space**: ~500MB for application + Java, additional space for server files
- **Internet Connection**: Required for initial setup (Java download, server download)

---

## 🚀 Installation

### Option 1: Download Pre-built Release (Recommended)

1. Go to the [Releases](https://github.com/yourusername/HyZaap/releases) page
2. Download the latest `HyZaap-vX.X.X.zip` file
3. Extract the ZIP file to your desired location
4. Run `HyZaap.exe`

### Option 2: Build from Source

See the [Building](#-building) section below.

---

## 💻 Usage

### First Time Setup

1. **Launch HyZaap**: Run `HyZaap.exe`
2. **Add a Server**: Click "Add Server" button
3. **Follow the Wizard**:
   - **Step 1**: Check Java installation (or download if needed)
   - **Step 2**: Configure server name and directory
   - **Step 3**: Download server files (using Hytale Downloader or manual copy)
   - **Step 4**: Configure server settings (port, memory, etc.)
   - **Step 5**: Review and create server

### Managing Servers

#### Starting a Server
1. Select a server from the list
2. Click "Start Server" button
3. Monitor console output in real-time

#### Stopping a Server
1. Navigate to the server management view
2. Click "Stop Server" button
   - Or use the console command: `/stop`

#### Configuring a Server

**Easy Mode:**
1. Select your server
2. Go to "Easy Settings" tab
3. Modify settings using the user-friendly interface
4. Click "Save" to apply changes

**Advanced Mode:**
1. Select your server
2. Click "Edit Config" button
3. Edit the JSON configuration directly
4. Click "Save" or "Save & Restart" to apply changes

#### Authentication

1. Start your server
2. Click "Auth Login" button
3. Complete authentication in the opened browser window
4. Profile will be automatically selected
5. You can now join your server!

#### Sending Commands

1. Navigate to server management view
2. Type your command in the command input field
3. Press Enter or click "Send"
4. View output in the console

---

## 🔨 Building

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Visual Studio 2022 or later (recommended) or VS Code with C# extension

### Build Steps

1. **Clone the repository**:
   ```bash
   git clone https://github.com/yourusername/HyZaap.git
   cd HyZaap
   ```

2. **Restore dependencies**:
   ```bash
   dotnet restore
   ```

3. **Build the project**:
   ```bash
   dotnet build --configuration Release
   ```

4. **Run the application**:
   ```bash
   dotnet run --configuration Release
   ```

### Creating a Release Build

#### Automated Release (Recommended)

Use the included PowerShell script for automated builds and releases:

```powershell
# Basic release (bumps patch version: 1.0.0 → 1.0.1)
.\build-release.ps1

# Minor version bump (1.0.0 → 1.1.0)
.\build-release.ps1 -BumpType minor

# Major version bump (1.0.0 → 2.0.0)
.\build-release.ps1 -BumpType major
```

The script automatically:
- Bumps version in `version.txt` and `HyZaap.csproj`
- Builds and publishes the project
- Creates a ZIP archive
- Creates and pushes a git tag
- Creates a GitHub release (if GitHub CLI is configured)

See [RELEASE.md](RELEASE.md) for detailed usage instructions.

#### Manual Release Build

For manual builds, create a self-contained release:

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The output will be in `bin/Release/net10.0-windows/win-x64/publish/`

---

## 📁 Project Structure

```
HyZaap/
├── Models/              # Data models (ServerInstance, etc.)
├── ViewModels/         # MVVM ViewModels
│   ├── MainViewModel.cs
│   ├── ServerSetupViewModel.cs
│   ├── ServerManagementViewModel.cs
│   ├── ServerConfigEditorViewModel.cs
│   └── EasyConfigEditorViewModel.cs
├── Views/              # XAML views
│   ├── ServerList.xaml
│   ├── ServerSetup.xaml
│   ├── ServerManagement.xaml
│   └── ServerConfigEditor.xaml
├── Services/           # Business logic services
│   ├── JavaService.cs
│   ├── JavaDownloadService.cs
│   ├── ServerDownloadService.cs
│   ├── ServerProcessService.cs
│   └── ConfigService.cs
├── Converters/         # XAML value converters
├── App.xaml            # Application resources and styling
├── MainWindow.xaml     # Main window
└── HyZaap.csproj       # Project file
```

---

## 🛠️ Configuration

### Server Configuration Files

Server configurations are stored in `servers.json` in the application directory.

Each server instance stores:
- Basic settings (name, port, memory, etc.)
- Paths (server directory, assets, Java)
- Runtime state (process ID, running status)

### Server Files Location

By default, servers are stored in:
```
<Application Directory>/servers/<Server Name>/
```

### Java Location

Java 25 is downloaded to:
```
<Application Directory>/java/jdk-25.0.1+8/
```

---

## 🐛 Troubleshooting

### Java Not Detected
- Ensure Java 25 is installed or use the download button in the setup wizard
- Check that the Java path is correct in server settings
- Try restarting the application

### Server Won't Start
- Verify Java 25 is installed and detected
- Check that server files are present in the server directory
- Review console output for error messages
- Ensure the port is not already in use

### Authentication Issues
- Make sure you complete the browser authentication flow
- Check that your internet connection is stable
- Verify the server is running before attempting authentication

### Console Not Showing Output
- If you closed and reopened the manager, logs may not be available for reattached processes
- Try restarting the server to restore full console functionality
- Check that the server process is still running

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

### Development Guidelines

- Follow C# coding conventions
- Use meaningful variable and method names
- Add comments for complex logic
- Test your changes before submitting
- Update documentation if needed

---

## 📝 License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

---

## 🙏 Acknowledgments

- Built for the Hytale community
- Inspired by Hytale's design language
- Uses [Adoptium](https://adoptium.net/) for Java distribution

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/yourusername/HyZaap/issues)
- **Discussions**: [GitHub Discussions](https://github.com/yourusername/HyZaap/discussions)

---

<div align="center">

**Made with ❤️ for the Hytale community**

[⬆ Back to Top](#hyzaap---hytale-server-manager)

</div>

