# IIS FTP Simple Authentication Provider

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net48)
[![IIS](https://img.shields.io/badge/IIS-10.0%2B-green.svg)](https://www.iis.net/)

A secure, lightweight authentication and authorization provider for IIS FTP that doesn't require Windows/Active Directory accounts.

## Features

- ✅ **Zero dependency on Windows/AD accounts** - Manage FTP users independently
- ✅ **Native IIS integration** - Works seamlessly with IIS FTP Server
- ✅ **Security first** - BCrypt password hashing, encryption at rest, audit logging
- ✅ **Hot-reload support** - Update users without restarting IIS
- ✅ **Flexible permissions** - Per-path read/write access control
- ✅ **CLI management tool** - Easy user and encryption key management
- ✅ **Multiple encryption options** - DPAPI or AES-GCM with environment variable keys

## Quick Start (5 Steps)

1. **Build the solution**
   ```powershell
   dotnet build
   ```

2. **Install the providers to IIS**
   ```powershell
   # Copy the DLLs to IIS directory
   Copy-Item "src\AuthProvider\bin\Debug\net48\*.dll" "C:\Windows\System32\inetsrv\"
   ```

3. **Create your first user**
   ```powershell
   # Using the CLI tool
   .\src\ManagementCli\bin\Debug\net48\ftpauth.exe create-user `
     -f "C:\inetpub\ftpusers\users.json" `
     -u "john.doe" `
     -p "SecurePassword123!" `
     -n "John Doe" `
     -h "/files/john" `
     --read --write
   ```

4. **Configure IIS FTP Site**
   - Open IIS Manager
   - Select your FTP site → FTP Authentication
   - Enable "IIS Manager Authentication"
   - Select "Custom Providers" and add:
     - Authentication Provider: `IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthenticationProvider`
     - Authorization Provider: `IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthorizationProvider`

5. **Test your connection**
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
    "Algorithm": "BCrypt",
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
    "Path": "C:\\inetpub\\ftpusers\\database",
    "EnableHotReload": false
  }
}
```

### Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `UserStore.Type` | Type of user store (Json, SQLite, Esent) | `Json` |
| `UserStore.Path` | Path to the users JSON file | `C:\inetpub\ftpusers\users.json` |
| `UserStore.EncryptionKeyEnv` | Environment variable with encryption key | `null` (uses DPAPI) |
| `UserStore.EnableHotReload` | Auto-reload users when file changes | `true` |
| `Hashing.Algorithm` | Password hashing algorithm (BCrypt, PBKDF2) | `BCrypt` |
| `Hashing.Iterations` | PBKDF2 iterations for password hashing | `100000` |
| `Logging.EnableEventLog` | Write to Windows Event Log | `true` |

## CLI Usage Examples

The `ftpauth.exe` CLI tool provides comprehensive user and encryption management:

### User Management

```powershell
# Create a new user
ftpauth create-user -f users.json -u jane.doe -p "Pass123!" -n "Jane Doe" -h "/home/jane"

# Change password
ftpauth change-password -f users.json -u jane.doe -p "NewPass456!"

# List all users
ftpauth list-users -f users.json

# Add permission to additional path
ftpauth add-permission -f users.json -u jane.doe -p "/shared/documents" --read --write

# Remove a user
ftpauth remove-user -f users.json -u jane.doe
```

### Encryption Management

```powershell
# Generate a new encryption key
ftpauth generate-key
# Or save to file
ftpauth generate-key -o encryption.key

# Encrypt user file with AES-GCM
$env:FTP_USERS_KEY = "your-base64-key-here"
ftpauth encrypt-file -i users.json -o users.enc -k FTP_USERS_KEY

# Decrypt for maintenance
ftpauth decrypt-file -i users.enc -o users.json -k FTP_USERS_KEY

# Rotate encryption key
$env:OLD_KEY = "old-base64-key"
$env:NEW_KEY = "new-base64-key"
ftpauth rotate-key -f users.enc -o OLD_KEY -n NEW_KEY
```

## Security Notes

### Password Hashing
- Uses BCrypt with work factor 12 by default for battle-tested security
- Auto-detects algorithm for backward compatibility with existing PBKDF2 hashes
- Legacy PBKDF2-SHA256 support maintained for existing users
- Constant-time comparison prevents timing attacks

### Encryption at Rest
- **Option 1: DPAPI** (default) - Windows Data Protection API, machine-specific
- **Option 2: AES-GCM** - 256-bit key from environment variable

### Key Rotation
Regular key rotation is recommended:
```powershell
# 1. Generate new key
ftpauth generate-key -o new-key.txt

# 2. Set environment variables
$env:OLD_FTP_KEY = "current-key"
$env:NEW_FTP_KEY = Get-Content new-key.txt

# 3. Rotate
ftpauth rotate-key -f users.enc -o OLD_FTP_KEY -n NEW_FTP_KEY

# 4. Update configuration to use NEW_FTP_KEY
```

### Audit Logging
Authentication events are logged to Windows Event Log:
- Source: `IIS-FTP-SimpleAuth`
- Success/failure events with session ID and client IP
- Configure verbosity in `ftpauth.config.json`

## Development

### Building from Source
```powershell
# Clone repository
git clone https://github.com/yourusername/IIS-FTP-SimpleAuthProvider.git
cd IIS-FTP-SimpleAuthProvider

# Restore packages and build
dotnet restore
dotnet build

# Run tests
dotnet test
```

### Project Structure
```
/src
  /AuthProvider     → IIS-facing provider classes
  /Core            → Domain logic, crypto, user stores
  /ManagementCli   → Command-line management tool
/tests
  /AuthProvider.Tests
  /Core.Tests
```

## Troubleshooting

### Event Log Access Denied
If you see "Failed to initialize EventLog", run the CLI as administrator once to create the event source:
```powershell
# Run as Administrator
ftpauth list-users -f dummy.json
```

### Authentication Failures
1. Check Windows Event Log: `Event Viewer → Applications → IIS-FTP-SimpleAuth`
2. Verify user exists: `ftpauth list-users -f users.json`
3. Test password locally: Create a test user and verify

### Hot-Reload Not Working
- Ensure the IIS app pool identity has read access to the user file directory
- Check that `EnableHotReload` is `true` in configuration

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

See [CONTRIBUTING](CONTRIBUTING) for detailed guidelines.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Third-Party Components

### WelsonJS.Esent

This project includes the WelsonJS.Esent library for ESENT database support:

- **Project**: [WelsonJS.Esent](https://github.com/gnh1201/welsonjs/tree/master/WelsonJS.Toolkit/WelsonJS.Esent)
- **License**: MIT License
- **Copyright**: 2025 Namhyeon Go, Catswords OSS and WelsonJS Contributors
- **Description**: Enable ESENT database engine functionality

The WelsonJS.Esent library is included as a git submodule and is used to provide native Windows ESENT database support without requiring external database dependencies.

## Acknowledgments

- Built for IIS FTP Server extensibility model
- Uses BCrypt P/Invoke for AES-GCM on .NET Framework
- ESENT database support provided by WelsonJS.Esent (MIT License)
- Inspired by the need for simple, secure FTP authentication without AD