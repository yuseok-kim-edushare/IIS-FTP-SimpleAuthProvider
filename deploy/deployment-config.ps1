# IIS FTP SimpleAuthProvider - Standardized Deployment Configuration
# This file provides consistent configuration values for all deployment scripts

# Standard naming convention
$script:DeploymentConfig = @{
    # IIS Site Configuration
    SiteName = "ftpauth"
    AppPoolName = "ftpauth-pool"
    Port = 8080
    
    # File Paths
    IISPath = "C:\inetpub\wwwroot\ftpauth"
    BackupPath = "C:\inetpub\backup\ftpauth"
    UserDataPath = "C:\inetpub\ftpusers"
    IISSystemPath = "C:\Windows\System32\inetsrv"
    
    # Configuration Files
    ConfigFileName = "ftpauth.config.json"
    UsersFileName = "users.json"
    
    # Build Paths
    SourcePath = "src\ManagementWeb\bin\Release\net48"
    AuthProviderPath = "src\AuthProvider\bin\Release\net48"
    
    # Provider Types
    AuthProviderType = "IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthenticationProvider"
    AuthzProviderType = "IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthorizationProvider"
    
    # Default User
    DefaultAdminUser = "admin"
    DefaultAdminPassword = "admin123"
    
    # File Permissions
    IISUserGroup = "IIS_IUSRS"
    RequiredPermissions = "Modify"
    
    # Service Names
    WebServiceName = "W3SVC"
    FTPServiceName = "FTPSVC"
    
    # Event Log
    EventLogSource = "IIS-FTP-SimpleAuth"
}

# Function to get configuration value
function Get-DeploymentConfig {
    param([string]$Key)
    
    if ($script:DeploymentConfig.ContainsKey($Key)) {
        return $script:DeploymentConfig[$Key]
    }
    
    throw "Configuration key '$Key' not found in deployment configuration"
}

# Function to get full path for configuration files
function Get-ConfigPath {
    param([string]$ConfigType)
    
    switch ($ConfigType) {
        "MainConfig" { return Join-Path $script:DeploymentConfig.IISSystemPath $script:DeploymentConfig.ConfigFileName }
        "UsersFile" { return Join-Path $script:DeploymentConfig.UserDataPath $script:DeploymentConfig.UsersFileName }
        "DeploymentInfo" { return Join-Path $script:DeploymentConfig.IISPath "deployment-info.json" }
        default { throw "Unknown config type: $ConfigType" }
    }
}

# Function to validate configuration
function Test-DeploymentConfig {
    $errors = @()
    
    # Check required directories
    $requiredPaths = @(
        $script:DeploymentConfig.IISPath,
        $script:DeploymentConfig.BackupPath,
        $script:DeploymentConfig.UserDataPath
    )
    
    foreach ($path in $requiredPaths) {
        if (-not (Test-Path $path)) {
            $errors += "Required path does not exist: $path"
        }
    }
    
    # Check IIS system path
    if (-not (Test-Path $script:DeploymentConfig.IISSystemPath)) {
        $errors += "IIS system path does not exist: $($script:DeploymentConfig.IISSystemPath)"
    }
    
    if ($errors.Count -gt 0) {
        return $false, $errors
    }
    
    return $true, @()
}

# Export configuration for use in other scripts
Export-ModuleMember -Variable DeploymentConfig -Function Get-DeploymentConfig, Get-ConfigPath, Test-DeploymentConfig
