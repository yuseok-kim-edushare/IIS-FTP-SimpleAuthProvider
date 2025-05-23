# IIS-FTP-SimpleAuthProvider

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework)

A secure, standalone authentication provider for IIS FTP that doesn't require Active Directory or Windows accounts. Features strong password hashing, encrypted user stores, audit logging, and hot-reload capabilities.

## 🎯 Features

- **Standalone Authentication**: No dependency on Active Directory or Windows accounts
- **Strong Security**: PBKDF2 password hashing with 100,000 iterations and constant-time comparison
- **Encrypted Storage**: AES-GCM encryption for user stores with DPAPI fallback
- **Hot Reload**: Zero-downtime user store updates with file watching
- **Audit Logging**: Windows Event Log integration for security monitoring  
- **Granular Permissions**: Per-path read/write permissions for each user
- **Thread-Safe**: Immutable data structures and concurrent access handling

## 🔧 Requirements

### Runtime
- **IIS 10.0** or later with FTP service installed
- **.NET Framework 4.8** or later
- **Windows Server 2016** or later (for BCrypt AES-GCM support)

### Development
- **Visual Studio 2019** or later, OR Visual Studio Code with C# extension
- **.NET Framework 4.8 SDK**
- **IIS FTP service** installed (required for `Microsoft.Web.FtpServer` assembly)

> ⚠️ **Note**: The `Microsoft.Web.FtpServer` assembly is only available when IIS FTP service is installed and is not redistributable.

## 🚀 Quick Start

### 1. Generate Encryption Key
```bash
# Generate a new 256-bit AES key
set FTP_USERS_KEY=<generated-base64-key>
```

### 2. Create Configuration File
Create `ftpauth.config.json`:
```json
{
  "UserStore": {
    "Type": "Json",
    "Path": "C:\\inetpub\\ftpusers\\users.enc",
    "EncryptionKeyEnv": "FTP_USERS_KEY",
    "EnableHotReload": true
  },
  "Hashing": {
    "Algorithm": "PBKDF2",
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

### 3. Create Users
```csharp
// Example: Create a new user
UserManager.CreateUser(
    @"C:\inetpub\ftpusers\users.json", 
    "ftpuser1", 
    "SecurePassword123!",
    "FTP User 1",
    "/home/ftpuser1",
    new List<Permission> {
        new Permission { Path = "/", CanRead = true, CanWrite = false },
        new Permission { Path = "/upload", CanRead = true, CanWrite = true }
    }
);

// Encrypt the user file
UserManager.EncryptUserFile(
    @"C:\inetpub\ftpusers\users.json",
    @"C:\inetpub\ftpusers\users.enc",
    "FTP_USERS_KEY"
);
```

### 4. Configure IIS FTP
Add to your FTP site's `web.config`:
```xml
<configuration>
  <system.ftpServer>
    <security>
      <authentication>
        <customAuthentication>
          <providers>
            <add name="SimpleAuth" 
                 type="IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthenticationProvider, AuthProvider" />
          </providers>
        </customAuthentication>
      </authentication>
      <authorization>
        <providers>
          <add name="SimpleAuth" 
               type="IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthorizationProvider, AuthProvider" />
        </providers>
      </authorization>
    </security>
  </system.ftpServer>
  <appSettings>
    <add key="UserStorePath" value="C:\inetpub\ftpusers\users.enc" />
  </appSettings>
</configuration>
```

## 🔐 Security Features

### Password Hashing
- **Algorithm**: PBKDF2 with SHA-256
- **Iterations**: 100,000 (configurable)
- **Salt**: 16-byte cryptographically secure random salt per password
- **Timing Attack Protection**: Constant-time comparison for password verification

### File Encryption
- **Primary**: AES-GCM with 256-bit keys via Windows BCrypt API
- **Fallback**: DPAPI (Data Protection API) for local machine scope
- **Key Management**: Environment variable or secure key storage
- **Authenticated Encryption**: Provides both confidentiality and integrity

### Audit Logging
- **Windows Event Log**: Integration with Application log
- **Event Types**: Success audits, failure audits, errors, configuration changes
- **Event IDs**: 
  - `1001`: Authentication Success
  - `1002`: Authentication Failure  
  - `1003`: UserStore Error
  - `1004`: Configuration Change

## 📖 User Management

### Creating Users
```csharp
// Basic user with default permissions
UserManager.CreateUser("users.json", "john", "password123", "John Doe");

// User with custom permissions
UserManager.CreateUser("users.json", "admin", "admin123", "Administrator", "/", 
    new List<Permission> {
        new Permission { Path = "/", CanRead = true, CanWrite = true }
    });
```

### Managing Permissions
```csharp
// Add/update permissions
UserManager.AddPermission("users.json", "john", "/uploads", true, true);
UserManager.AddPermission("users.json", "john", "/readonly", true, false);

// List all users
UserManager.ListUsers("users.json");
```

### Password Management
```csharp
// Change password
UserManager.ChangePassword("users.json", "john", "newPassword456");
```

### Encryption Operations
```csharp
// Generate new encryption key
string key = UserManager.GenerateEncryptionKey();
Console.WriteLine($"Set environment variable: FTP_USERS_KEY={key}");

// Encrypt existing user file
UserManager.EncryptUserFile("users.json", "users.enc", "FTP_USERS_KEY");

// Decrypt for maintenance
UserManager.DecryptUserFile("users.enc", "users.json", "FTP_USERS_KEY");
```

## 🏗️ Architecture

```
┌──────────────┐         ┌───────────────┐          ┌───────────────┐
│ Configuration│ ←json┐  │  User Store   │ ←JSON/DB │   CryptoSvc    │
└──────┬───────┘       │  └───────────────┘          └──────┬────────┘
       │               │          ▲                          │
       ▼               │          │                          ▼
  Provider classes ────┴──────────┴────────────────────────>Hash / Salt
```

### Components
- **AuthProvider**: IIS FTP integration layer
- **Core**: Business logic, domain models, security
- **Configuration**: JSON-based configuration system
- **Stores**: Pluggable user storage backends
- **Security**: Password hashing and file encryption
- **Logging**: Audit trail and monitoring

## 🔧 Configuration

### User Store Types
- **Json** (default): File-based JSON storage
- **SqlServer**: SQL Server database (future)
- **SQLite**: SQLite database (future)

### Hashing Algorithms
- **PBKDF2** (default): RFC 2898 with SHA-256
- **BCrypt**: bcrypt algorithm (future)
- **Argon2**: Argon2id algorithm (future)

## 🐛 Troubleshooting

### Common Issues

**Authentication Fails**
- Check Event Log for error details
- Verify user file encryption/decryption
- Ensure correct password and case sensitivity

**Hot Reload Not Working**
- Check file permissions on user store directory
- Verify FileSystemWatcher is enabled
- Look for file locking issues

**Encryption Errors**
- Verify environment variable is set correctly
- Check key format (must be valid Base64)
- Ensure BCrypt API availability on Windows

### Event Log Monitoring
```powershell
# View authentication events
Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='IIS-FTP-SimpleAuth'}

# Monitor failures only
Get-WinEvent -FilterHashtable @{LogName='Application'; ProviderName='IIS-FTP-SimpleAuth'; ID=1002}
```

## 🔄 Migration & Backup

### Backup Strategy
```bash
# Backup encrypted user store
copy C:\inetpub\ftpusers\users.enc C:\backup\users.enc.backup

# Backup configuration
copy ftpauth.config.json C:\backup\ftpauth.config.json.backup
```

### Key Rotation
```csharp
// 1. Generate new key
string newKey = UserManager.GenerateEncryptionKey();

// 2. Decrypt with old key
UserManager.DecryptUserFile("users.enc", "users.json", "OLD_KEY_ENV");

// 3. Encrypt with new key  
UserManager.EncryptUserFile("users.json", "users.enc", "NEW_KEY_ENV");
```

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Microsoft IIS FTP Extensibility API
- Windows BCrypt Cryptographic APIs
- .NET Framework Security Libraries