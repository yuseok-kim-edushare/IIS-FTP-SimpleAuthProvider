# IIS FTP Simple Authentication Provider

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net48)
[![IIS](https://img.shields.io/badge/IIS-10.0%2B-green.svg)](https://www.iis.net/)

A secure, lightweight authentication and authorization provider for IIS FTP that doesn't require Windows/Active Directory accounts.

## Features

- ✅ **Zero dependency on Windows/AD accounts** - Manage FTP users independently
- ✅ **Native IIS integration** - Works seamlessly with IIS FTP Server
- ✅ **Security first** - BCrypt (default) password hashing, Argon2 support, encryption at rest, audit logging
- ✅ **Hot-reload support** - Update users without restarting IIS
- ✅ **Flexible permissions** - Per-path read/write access control
- ✅ **CLI management tool** - Easy user and encryption key management
- ✅ **Web management interface** - User-friendly web UI for user and permission management
- ✅ **Multiple encryption options** - DPAPI or AES-256-GCM with environment variable keys

## Quick Start (5 Steps)

> **📖 For detailed installation and setup instructions, see [Installation & Setup Guide](docs/installation-and-setup-guide.md)**
> **🏗️ For architecture and design details, see [Architecture Diagrams](docs/architecture%20diagrams.md) and [Codebase Summary](docs/codebase-summary.md)**
> **📁 For project structure and organization, see [Project Structure](docs/project-structure.md)**
> **🔧 For technical implementation details, see [Technical Overview](docs/technical-overview.md)**
> **🖥️ For Windows 11 Pro client setup, see [Windows 11 Pro Client Setup Guide](docs/windows-11-pro-client-setup.md)**
> **🚀 For automated deployment, see [Deployment Guide](deploy/README.md)**

1. **Build the solution**
   ```powershell
   # Build all projects
   .\build-all.ps1
   
   # Or build only specific projects
   .\build-sdk-projects.ps1
   ```

2. **Deploy to IIS (automated)**
   ```powershell
   # Navigate to deploy directory
   cd deploy
   
   # Full system deployment (recommended)
   .\integrated-deploy.ps1 -CreateAppPool -CreateSite
   
   # Or web service only
   .\deploy-web-service-local.ps1
   ```

3. **Create your first user**
   ```powershell
   # Using the CLI tool
   .\src\ManagementCli\bin\Release\net48\ftpauth.exe create-user `
     -f "C:\inetpub\ftpusers\users.json" `
     -u "john.doe" `
     -p "SecurePassword123!" `
     -n "John Doe" `
     -h "/files/john" `
     --read --write
   ```

4. **Access the web management interface**
   - Open your browser and navigate to: `http://localhost:8080`
   - Login with admin credentials: `admin` / `admin123` (if you want use temp-users.json, password123 is pw)
   - Manage users and permissions through the web UI

5. **Test your FTP connection**
   ```powershell
   # Using Windows FTP client
   ftp ftp://john.doe:SecurePassword123!@localhost
   ```

## Configuration

Create `ftpauth.config.json` in your IIS directory:

```json
{
  "UserStore": {
    "Type": "Json",
    "Path": "C:\\inetpub\\ftpusers\\users.json",
    "EncryptionKeyEnv": "FTP_USERS_KEY",
    "EnableHotReload": true
  },
  "Hashing": {
    "Algorithm": "BCrypt", // or "PBKDF2" or "Argon2"
    "Iterations": 100000,
    "SaltSize": 16
  },
  "Logging": {
    "EnableEventLog": true,
    "EventLogSource": "IIS-FTP-SimpleAuth",
    "LogFailures": true,
    "LogSuccesses": false
  }
}
```

### User Store Types

This provider supports multiple user store backends:

- **Json** (default): Simple file-based JSON storage with optional encryption
- **SQLite**: Embedded SQLite database for better performance
- **Esent**: Windows native ESENT database (no external dependencies)

To use ESENT storage, update your configuration:

```json
{
  "UserStore": {
    "Type": "Esent",
    "Path": "C:\\inetpub\\ftpusers\\users.edb"
  }
}
```

## Management Tools

### Web Management Interface

Access the web-based user management console at `http://localhost:8080`:

- **User Management**: Create, edit, and delete FTP users
- **Permission Control**: Set read/write access for specific directories
- **System Monitoring**: View authentication logs and system health
- **Real-time Updates**: Changes take effect immediately without restarting IIS

### CLI Management Tool

The `ftpauth.exe` CLI tool provides comprehensive user and encryption management:

```powershell
# User management
ftpauth create-user -f users.json -u jane.doe -p "Pass123!" -n "Jane Doe" -h "/home/jane"
ftpauth change-password -f users.json -u jane.doe -p "NewPass456!"
ftpauth list-users -f users.json

# Permission management
ftpauth add-permission -f users.json -u jane.doe -p "/shared/documents" --read --write
ftpauth remove-user -f users.json -u jane.doe

# Access the web interface
# Navigate to /ftpauth/ on your IIS server
```

### Encryption Management

Manage encryption keys and encrypted user stores:

```powershell
# Generate new encryption key
ftpauth generate-key
ftpauth generate-key -o encryption.key

# Encrypt/decrypt user files
ftpauth encrypt-file -i users.json -o users.enc -k FTP_USERS_KEY
ftpauth decrypt-file -i users.enc -o users.json -k FTP_USERS_KEY

# Rotate encryption keys
ftpauth rotate-key -f users.enc -o OLD_KEY -n NEW_KEY
```

## Deployment Options

### Automated Deployment (Recommended)

```powershell
# Full system deployment
cd deploy
.\integrated-deploy.ps1 -CreateAppPool -CreateSite

# Web service only
.\deploy-web-service-local.ps1

# Quick deployment
.\quick-deploy.ps1
```

### Manual Deployment

```powershell
# Build the solution
.\build-all.ps1

# Deploy to IIS
.\deploy\deploy-to-iis.ps1 -CreateAppPool -CreateSite

# Check deployment status
.\deploy\check-deployment-status.ps1
```

### Troubleshooting

```powershell
# Diagnose issues
.\deploy\diagnose-ftp-issues.ps1

# Auto-fix common problems
.\deploy\diagnose-ftp-issues.ps1 -FixIssues

# Check deployment status
.\deploy\check-deployment-status.ps1
```

## Project Structure

```
IIS-FTP-SimpleAuthProvider/
├── src/
│   ├── AuthProvider/          # IIS FTP provider implementations
│   ├── Core/                  # Core business logic and security
│   ├── ManagementCli/         # Command-line management tool
│   └── ManagementWeb/         # Web management interface
├── tests/                     # Test projects
├── deploy/                    # Deployment scripts and guides
├── docs/                      # Documentation
├── build-all.ps1             # Master build script
└── IIS-FTP-SimpleAuthProvider.slnx
```

## Build and Development

### Prerequisites

- **Visual Studio 2022** or **Build Tools** (for MSBuild)
- **IIS** with FTP Server role
- **PowerShell 5.1+**

### Build Commands

```powershell
# Build everything
.\build-all.ps1

# Build only SDK-style projects
.\build-sdk-projects.ps1

# Build only SystemWeb project
.\build-systemweb.ps1

# Build with custom configuration
.\build-all.ps1 -Configuration Debug
```

### Development Workflow

```powershell
# Daily development
.\build-all.ps1 -Configuration Debug

# Quick iteration (SDK projects only)
.\build-sdk-projects.ps1 -Configuration Debug

# Full build including web project
.\build-all.ps1 -Configuration Debug

# Run tests
msbuild IIS-FTP-SimpleAuthProvider.slnx /t:Test /p:Configuration=Debug
```

## Security Features

### Password Hashing

- **BCrypt** (default): Industry-standard password hashing with configurable work factor
- **PBKDF2**: Legacy support with configurable iterations
- **Argon2**: Future support for memory-hard hashing algorithms

### Encryption at Rest

- **DPAPI**: Windows Data Protection API (default, no key management required)
- **AES-256-GCM**: Strong encryption with environment variable keys
- **Key Rotation**: Seamless encryption key updates

### Audit and Monitoring

- **Windows Event Log**: Native Windows logging integration
- **Authentication Metrics**: Success/failure rate monitoring
- **Prometheus Export**: Metrics for monitoring systems
- **Real-time Logging**: Immediate visibility into authentication events

## Troubleshooting

### Common Issues

1. **Build failures**
   ```powershell
   # Check MSBuild availability
   msbuild /version
   
   # Clean and rebuild
   msbuild IIS-FTP-SimpleAuthProvider.slnx /t:Clean
   msbuild IIS-FTP-SimpleAuthProvider.slnx
   ```

2. **IIS configuration issues**
   ```powershell
   # Check IIS status
   Get-Service -Name "W3SVC", "FTPSVC"
   
   # Restart IIS
   iisreset /restart
   ```

3. **Permission errors**
   ```powershell
   # Check deployment status
   .\deploy\check-deployment-status.ps1
   
   # Diagnose issues
   .\deploy\diagnose-ftp-issues.ps1
   ```

### Getting Help

- **Check deployment status**: `.\deploy\check-deployment-status.ps1`
- **Diagnose issues**: `.\deploy\diagnose-ftp-issues.ps1`
- **Review logs**: Windows Event Viewer → Applications → IIS-FTP-SimpleAuth
- **Documentation**: See [docs/](docs/) folder for detailed guides

## Contributing

See [CONTRIBUTING](CONTRIBUTING) for development guidelines and contribution information.

## License

This project is licensed under the MIT License - see the [license](license) file for details.

## Support

- **GitHub Issues**: Report bugs and request features
- **Documentation**: Comprehensive guides in the [docs/](docs/) folder
- **Deployment**: Automated scripts in the [deploy/](deploy/) folder