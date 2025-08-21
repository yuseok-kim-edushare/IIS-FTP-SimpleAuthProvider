# IIS FTP Simple Authentication Provider - Installation & Setup Guide

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net48)
[![IIS](https://img.shields.io/badge/IIS-10.0%2B-green.svg)](https://www.iis.net/)

Complete step-by-step guide for installing, configuring, and managing the IIS FTP Simple Authentication Provider with both CLI and Web management interfaces.

## 📋 Table of Contents

- [Prerequisites](#prerequisites)
- [Phase 1: System Installation](#phase-1-system-installation)
- [Phase 2: Initial Configuration](#phase-2-initial-configuration)
- [Phase 3: Web Management Setup](#phase-3-web-management-setup)
- [Phase 4: Folder Permission Configuration](#phase-4-folder-permission-configuration)
- [Phase 5: Testing & Verification](#phase-5-testing--verification)
- [Troubleshooting](#troubleshooting)
- [Security Best Practices](#security-best-practices)
- [Maintenance & Updates](#maintenance--updates)

> **🖥️ Windows 11 Pro 클라이언트에서 실행하려면 [Windows 11 Pro Client Setup Guide](windows-11-pro-client-setup.md)를 참조하세요.**

## 🔧 Prerequisites

### System Requirements
- **Operating System**: Windows Server 2012 R2 or later, **Windows 11 Pro** (see [Windows 11 Pro Client Setup Guide](windows-11-pro-client-setup.md))
- **IIS Version**: IIS 8.0 or later
- **.NET Framework**: 4.8 or later (server 2012R2 requires upgrade .NET Fx)
- **PowerShell**: 5.1 or later (server 2012R2 requires upgrade Powershell)
- **Administrative Access**: Local administrator privileges required
#### Build Requirements choice one of lists
- **1) .NET SDK** : 9.x or later for slnx build
- **2) Latest Visual Studio** : to build slnx build

### Windows Features
Ensure these Windows features are enabled:
```powershell
# Check current status
Get-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole, IIS-FTPServer, IIS-FTPSvc

# Enable if not already enabled
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-FTPServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-FTPSvc
```

## 🚀 Phase 1: System Installation

### 1.1 Build the Solution
```powershell
# Clone or download the repository
cd C:\Users\kgong2\source\repos\Githubs\IIS-FTP-SimpleAuthProvider

# Build all projects in Release mode
msbuild IIS-FTP-SimpleAuthProvider.slnx /p:Configuration=Release

# Verify successful build
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Build successful!" -ForegroundColor Green
} else {
    Write-Host "❌ Build failed!" -ForegroundColor Red
    exit 1
}
```

### 1.2 Deploy Components
```powershell
# Create deployment directories
New-Item -ItemType Directory -Path "C:\Program Files\IIS-FTP-SimpleAuth" -Force
New-Item -ItemType Directory -Path "C:\inetpub\wwwroot\ftpauth" -Force
New-Item -ItemType Directory -Path "C:\inetpub\ftpusers" -Force

# Deploy ManagementWeb to IIS (complete web application)
# First, copy the web content (excluding bin and obj folders)
Get-ChildItem "src\ManagementWeb" -Exclude "bin", "obj" | Copy-Item -Destination "C:\inetpub\wwwroot\ftpauth\" -Recurse -Force

# Then copy the compiled assemblies to the bin folder
Copy-Item "src\ManagementWeb\bin\Release\net48\*" "C:\inetpub\wwwroot\ftpauth\bin\" -Recurse -Force

# Deploy CLI tool
Copy-Item "src\ManagementCli\bin\Release\net48\ftpauth.exe" "C:\Program Files\IIS-FTP-SimpleAuth\"

# Deploy Core libraries
Copy-Item "src\Core\bin\Release\net48\*.dll" "C:\Windows\System32\inetsrv\"
Copy-Item "src\AuthProvider\bin\Release\net48\*.dll" "C:\Windows\System32\inetsrv\"

Write-Host "✅ Components deployed successfully!" -ForegroundColor Green

# Verify web deployment
Write-Host "🔍 Verifying web deployment..." -ForegroundColor Yellow
$webFiles = @(
    "C:\inetpub\wwwroot\ftpauth\Web.config",
    "C:\inetpub\wwwroot\ftpauth\Global.asax",
    "C:\inetpub\wwwroot\ftpauth\bin\ManagementWeb.dll"
)

foreach ($file in $webFiles) {
    if (Test-Path $file) {
        Write-Host "✅ Found: $file" -ForegroundColor Green
    } else {
        Write-Host "❌ Missing: $file" -ForegroundColor Red
    }
}
```

### 1.3 Configure IIS Application
```powershell
# Create new IIS application
Import-Module WebAdministration

# Create application pool
New-WebAppPool -Name "FTP-SimpleAuth-Pool"
Set-ItemProperty -Path "IIS:\AppPools\FTP-SimpleAuth-Pool" -Name "managedRuntimeVersion" -Value "v4.0"
Set-ItemProperty -Path "IIS:\AppPools\FTP-SimpleAuth-Pool" -Name "managedPipelineMode" -Value "Integrated"

# Create website or application
New-WebApplication -Name "ftpauth" -Site "Default Web Site" -PhysicalPath "C:\inetpub\wwwroot\ftpauth" -ApplicationPool "FTP-SimpleAuth-Pool"

Write-Host "✅ IIS application configured!" -ForegroundColor Green
```

## ⚙️ Phase 2: Initial Configuration

### 2.1 Generate Encryption Key
```powershell
# Generate encryption key for user data
Write-Host "🔑 Generating encryption key..." -ForegroundColor Yellow
$encryptionKey = & "C:\Program Files\IIS-FTP-SimpleAuth\ftpauth.exe" generate-key

# Extract the key from output (remove the warning message)
$keyLines = $encryptionKey -split "`n"
$actualKey = $keyLines | Where-Object { $_ -match "^[A-Za-z0-9+/=]+$" } | Select-Object -First 1

if ($actualKey) {
    # Set as machine environment variable
    [Environment]::SetEnvironmentVariable("FTP_USERS_KEY", $actualKey, "Machine")
    Write-Host "✅ Encryption key set as environment variable: FTP_USERS_KEY" -ForegroundColor Green
    Write-Host "Key: $actualKey" -ForegroundColor Cyan
} else {
    Write-Host "❌ Failed to extract encryption key!" -ForegroundColor Red
    exit 1
}
```

### 2.2 Create Initial Admin Account
```powershell
# Create initial admin user
Write-Host "👤 Creating admin account..." -ForegroundColor Yellow

# Create empty users file
"[]" | Out-File -FilePath "C:\inetpub\ftpusers\users.json" -Encoding UTF8

# Create admin user
& "C:\Program Files\IIS-FTP-SimpleAuth\ftpauth.exe" create-user `
    -f "C:\inetpub\ftpusers\users.json" `
    -u "admin" `
    -p "SecureAdminPass123!" `
    -n "System Administrator" `
    -h "/" `
    -r true `
    -w true

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Admin account created successfully!" -ForegroundColor Green
} else {
    Write-Host "❌ Failed to create admin account!" -ForegroundColor Red
    exit 1
}
```

### 2.3 Configure Web Management Interface
```xml
<!-- Edit C:\inetpub\wwwroot\ftpauth\Web.config -->
<appSettings>
  <add key="UserStore:Type" value="Json" />
  <add key="UserStore:Path" value="C:\inetpub\ftpusers\users.enc" />
  <add key="AllowedAdmins" value="admin" />
  <add key="EncryptionKeyEnv" value="FTP_USERS_KEY" />
  <add key="EnableAuditLogging" value="true" />
  <add key="AuditLogPath" value="C:\inetpub\ftpusers\audit.log" />
</appSettings>
```

### 2.4 Create Configuration File
```powershell
# Create ftpauth.config.json
$config = @{
    UserStore = @{
        Type = "Json"
        Path = "C:\inetpub\ftpusers\users.enc"
        EncryptionKeyEnv = "FTP_USERS_KEY"
        EnableHotReload = $true
    }
    Hashing = @{
        Algorithm = "BCrypt"
        Iterations = 100000
        SaltSize = 16
    }
    Logging = @{
        EnableEventLog = $true
        EventLogSource = "IIS-FTP-SimpleAuth"
        LogFailures = $true
        LogSuccesses = $false
    }
    Security = @{
        RequireHttps = $true
        SessionTimeout = 30
        MaxLoginAttempts = 5
    }
}

$config | ConvertTo-Json -Depth 10 | Out-File -FilePath "C:\inetpub\ftpusers\ftpauth.config.json" -Encoding UTF8
Write-Host "✅ Configuration file created!" -ForegroundColor Green
```

## 🌐 Phase 3: Web Management Setup

### 3.1 Access Web Interface
1. **Open your browser** and navigate to: `https://your-server/ftpauth/`
2. **Login** with admin credentials:
   - Username: `admin`
   - Password: `SecureAdminPass123!`
3. **Dashboard** will show system status and user count

### 3.2 Create Additional Users via Web UI
1. **User Creation Flow:**
   - Click "Create User" button
   - Fill in user details:
     - User ID: `ftpuser1`
     - Display Name: `FTP User 1`
     - Password: `SecurePass123!`
     - Home Directory: `/ftpuser1`
   - Set initial permissions:
     - Read access to home directory
     - Write access to upload folder
   - Click "Create User"

2. **Permission Management Flow:**
   - Select user from user list
   - Click "Manage Permissions"
   - Add/remove directory access:
     - `/ftpuser1` - Read/Write (home directory)
     - `/shared` - Read only (shared resources)
     - `/archive` - Read only (archived files)

### 3.3 Web Interface Features
- **Dashboard**: System health, user statistics, recent activity
- **User Management**: Create, edit, delete users
- **Permission Management**: Granular folder access control
- **System Monitoring**: Health checks, metrics, audit logs
- **Bulk Operations**: Import/export users, bulk permission updates

## 📁 Phase 4: Folder Permission Configuration

### 4.1 Create Directory Structure
```powershell
# Create FTP directory structure
Write-Host "📁 Creating directory structure..." -ForegroundColor Yellow

$directories = @(
    "C:\inetpub\ftproot\ftpuser1",
    "C:\inetpub\ftproot\shared",
    "C:\inetpub\ftproot\upload",
    "C:\inetpub\ftproot\archive",
    "C:\inetpub\ftproot\temp"
)

foreach ($dir in $directories) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Write-Host "Created: $dir" -ForegroundColor Green
}
```

### 4.2 Set NTFS Permissions
```powershell
# Set permissions for user directories
Write-Host "🔐 Setting NTFS permissions..." -ForegroundColor Yellow

# Function to set directory permissions
function Set-DirectoryPermissions {
    param(
        [string]$Path,
        [string]$Identity,
        [string]$Rights,
        [string]$Inheritance = "ContainerInherit,ObjectInherit"
    )
    
    $acl = Get-Acl $Path
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule(
        $Identity, $Rights, $Inheritance, "None", "Allow"
    )
    $acl.SetAccessRule($accessRule)
    Set-Acl $Path $acl
    Write-Host "Set $Rights permissions for $Identity on $Path" -ForegroundColor Green
}

# Set permissions for different directory types
Set-DirectoryPermissions -Path "C:\inetpub\ftproot\ftpuser1" -Identity "IIS_IUSRS" -Rights "Modify"
Set-DirectoryPermissions -Path "C:\inetpub\ftproot\shared" -Identity "IIS_IUSRS" -Rights "ReadAndExecute"
Set-DirectoryPermissions -Path "C:\inetpub\ftproot\upload" -Identity "IIS_IUSRS" -Rights "Modify"
Set-DirectoryPermissions -Path "C:\inetpub\ftproot\archive" -Identity "IIS_IUSRS" -Rights "ReadAndExecute"
Set-DirectoryPermissions -Path "C:\inetpub\ftproot\temp" -Identity "IIS_IUSRS" -Rights "Modify"

Write-Host "✅ NTFS permissions configured!" -ForegroundColor Green
```

### 4.3 Configure IIS FTP Site
```powershell
# Create and configure FTP site
Write-Host "🌐 Configuring IIS FTP site..." -ForegroundColor Yellow

# Create FTP site
New-WebFtpSite -Name "FTP-SimpleAuth" -Port 21 -PhysicalPath "C:\inetpub\ftproot"

# Configure authentication provider
Set-WebConfigurationProperty -Filter "system.ftpServer/security/authentication/customAuthentication" -Name "enabled" -Value $true

# Add custom authentication provider
Add-WebConfigurationProperty -Filter "system.ftpServer/security/authentication/customAuthentication/providers" -Name "." -Value @{
    name = "SimpleAuth"
    enabled = $true
    type = "IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthenticationProvider"
}

# Add custom authorization provider
Add-WebConfigurationProperty -Filter "system.ftpServer/security/authorization/customAuthorization" -Name "." -Value @{
    name = "SimpleAuth"
    enabled = $true
    type = "IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthorizationProvider"
}

Write-Host "✅ IIS FTP site configured!" -ForegroundColor Green
```

## ✅ Phase 5: Testing & Verification

### 5.1 Test Web Interface
```powershell
# Test web interface accessibility
Write-Host "🧪 Testing web interface..." -ForegroundColor Yellow

try {
    $response = Invoke-WebRequest -Uri "http://localhost/ftpauth/" -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Web interface accessible!" -ForegroundColor Green
    } else {
        Write-Host "❌ Web interface returned status: $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ Web interface test failed: $($_.Exception.Message)" -ForegroundColor Red
}
```

### 5.2 Test User Creation via CLI
```powershell
# Test CLI functionality
Write-Host "🧪 Testing CLI functionality..." -ForegroundColor Yellow

# Test user listing
& "C:\Program Files\IIS-FTP-SimpleAuth\ftpauth.exe" list-users -f "C:\inetpub\ftpusers\users.json"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ CLI functionality verified!" -ForegroundColor Green
} else {
    Write-Host "❌ CLI test failed!" -ForegroundColor Red
}
```

### 5.3 Test FTP Authentication
```powershell
# Test FTP connection (requires FTP client)
Write-Host "🧪 Testing FTP authentication..." -ForegroundColor Yellow
Write-Host "Use an FTP client to test connection:" -ForegroundColor Cyan
Write-Host "Host: localhost" -ForegroundColor White
Write-Host "Port: 21" -ForegroundColor White
Write-Host "Username: admin" -ForegroundColor White
Write-Host "Password: SecureAdminPass123!" -ForegroundColor White
```

## 🔧 Troubleshooting

### Common Issues and Solutions

#### 1. Build Failures
```powershell
# Check .NET Framework version
Get-ChildItem "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP" -Recurse | Get-ItemProperty -Name version -EA 0 | Where-Object { $_.version } | Sort-Object { [System.Version]$_.version } -Descending

# Restore NuGet packages
dotnet restore --force
```

#### 2. IIS Configuration Issues
```powershell
# Check IIS configuration
Get-WebConfiguration "/system.ftpServer/security/authentication/customAuthentication"

# Reset IIS
iisreset /restart
```

#### 2.1 Web Application Deployment Issues
```powershell
# Check if web files are properly deployed
$webPath = "C:\inetpub\wwwroot\ftpauth"
Write-Host "Checking web deployment at: $webPath" -ForegroundColor Yellow

# Check essential web files
$essentialFiles = @("Web.config", "Global.asax", "bin\ManagementWeb.dll")
foreach ($file in $essentialFiles) {
    $fullPath = Join-Path $webPath $file
    if (Test-Path $fullPath) {
        Write-Host "✅ Found: $file" -ForegroundColor Green
    } else {
        Write-Host "❌ Missing: $file" -ForegroundColor Red
    }
}

# Check web directories
$webDirs = @("Controllers", "Views", "Content", "Scripts")
foreach ($dir in $webDirs) {
    $fullPath = Join-Path $webPath $dir
    if (Test-Path $fullPath) {
        Write-Host "✅ Found directory: $dir" -ForegroundColor Green
    } else {
        Write-Host "❌ Missing directory: $dir" -ForegroundColor Red
    }
}

# Redeploy if files are missing
if (-not (Test-Path "$webPath\Web.config")) {
    Write-Host "🔄 Redeploying web application..." -ForegroundColor Yellow
    
    # Stop application pool
    Stop-WebAppPool "FTP-SimpleAuth-Pool"
    
    # Clean and redeploy
    Remove-Item "$webPath\*" -Recurse -Force
    Get-ChildItem "src\ManagementWeb" -Exclude "bin", "obj" | Copy-Item -Destination $webPath -Recurse -Force
    Copy-Item "src\ManagementWeb\bin\Release\net48\*" "$webPath\bin\" -Recurse -Force
    
    # Start application pool
    Start-WebAppPool "FTP-SimpleAuth-Pool"
    
    Write-Host "✅ Web application redeployed!" -ForegroundColor Green
}
```

#### 3. Permission Denied Errors
```powershell
# Check file permissions
Get-Acl "C:\inetpub\ftpusers\users.json" | Format-List

# Grant IIS_IUSRS access
$acl = Get-Acl "C:\inetpub\ftpusers\users.json"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "Read", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl "C:\inetpub\ftpusers\users.json" $acl
```

#### 4. Encryption Key Issues
```powershell
# Verify environment variable
[Environment]::GetEnvironmentVariable("FTP_USERS_KEY", "Machine")

# Test encryption/decryption
& "C:\Program Files\IIS-FTP-SimpleAuth\ftpauth.exe" encrypt-file -i "C:\inetpub\ftpusers\users.json" -o "C:\inetpub\ftpusers\users.enc"
```

### Event Log Analysis
```powershell
# Check application logs
Get-EventLog -LogName Application -Source "IIS-FTP-SimpleAuth" -Newest 10 | Format-Table TimeGenerated, EntryType, Message -AutoSize

# Check system logs
Get-EventLog -LogName System -Newest 20 | Where-Object { $_.Message -like "*FTP*" } | Format-Table TimeGenerated, EntryType, Message -AutoSize
```

## 🛡️ Security Best Practices

### 1. Encryption
- **Always use environment variables** for encryption keys
- **Rotate keys regularly** (every 90 days recommended)
- **Use strong passwords** for admin accounts
- **Enable HTTPS** for web management interface

### 2. Access Control
- **Limit admin access** to specific IP ranges
- **Use strong authentication** for web interface
- **Implement session timeouts** (30 minutes recommended)
- **Enable audit logging** for all operations

### 3. Network Security
- **Use firewall rules** to restrict FTP access
- **Enable SSL/TLS** for FTP connections
- **Monitor connection attempts** and block suspicious IPs
- **Regular security updates** for Windows and IIS

### 4. User Management
- **Enforce password policies** (minimum 12 characters)
- **Regular user audits** and cleanup
- **Principle of least privilege** for folder access
- **Monitor failed login attempts**

## 🔄 Maintenance & Updates

### Regular Maintenance Tasks
```powershell
# Daily
- Check event logs for errors
- Monitor disk space usage
- Verify backup status

# Weekly
- Review user access logs
- Check system performance
- Update security patches

# Monthly
- Rotate encryption keys
- Audit user permissions
- Review and clean up old users
- Test disaster recovery procedures
```

### Backup Procedures
```powershell
# Create backup script
$backupPath = "C:\backups\ftp-users\$(Get-Date -Format 'yyyy-MM-dd-HHmm')"
New-Item -ItemType Directory -Path $backupPath -Force

# Backup user files
Copy-Item "C:\inetpub\ftpusers\*" $backupPath -Recurse

# Backup configuration
Copy-Item "C:\inetpub\wwwroot\ftpauth\Web.config" $backupPath

Write-Host "✅ Backup created: $backupPath" -ForegroundColor Green
```

### Update Procedures
```powershell
# 1. Create backup
# 2. Stop IIS application
Stop-WebAppPool "FTP-SimpleAuth-Pool"

# 3. Deploy new version
Copy-Item "new-version\*" "C:\inetpub\wwwroot\ftpauth\" -Recurse -Force

# 4. Start application
Start-WebAppPool "FTP-SimpleAuth-Pool"

# 5. Verify functionality
# 6. Test user authentication
```

## 📚 Additional Resources

### Documentation
- [Main README](../readme.md) - Project overview and quick start
- [Windows 11 Pro Client Setup Guide](windows-11-pro-client-setup.md) - Windows 11 Pro 클라이언트 환경 설정
- [Design Documentation](design.md) - Architecture and design decisions
- [Web Management Console](Web-Management-Console-Summary.md) - Web UI features
- [Improvement Roadmap](improvement-roadmap.md) - Future development plans

### Support
- **GitHub Issues**: Report bugs and request features
- **Event Logs**: Check Windows Event Viewer for detailed error information
- **Community**: Join discussions in GitHub Discussions

### Related Tools
- **IIS Manager**: Native IIS management interface
- **PowerShell**: Automation and bulk operations
- **Event Viewer**: System and application monitoring
- **Performance Monitor**: System performance analysis

---

## 🎯 Quick Reference Commands

### Essential Commands
```powershell
# Check system status
Get-Service -Name "W3SVC", "MSFTPSVC"

# Restart IIS
iisreset /restart

# Check web application
Get-WebApplication -Site "Default Web Site"

# List FTP users
& "C:\Program Files\IIS-FTP-SimpleAuth\ftpauth.exe" list-users -f "C:\inetpub\ftpusers\users.json"

# Generate new encryption key
& "C:\Program Files\IIS-FTP-SimpleAuth\ftpauth.exe" generate-key
```

### Configuration Files
- **Web.config**: `C:\inetpub\wwwroot\ftpauth\Web.config`
- **FTP Config**: `C:\inetpub\ftpusers\ftpauth.config.json`
- **User Store**: `C:\inetpub\ftpusers\users.enc`
- **Event Log**: Windows Event Viewer → Applications → IIS-FTP-SimpleAuth

---

*This guide covers the complete installation and setup process. For advanced configuration options, refer to the individual component documentation.*
