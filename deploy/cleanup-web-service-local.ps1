# IIS FTP SimpleAuthProvider - Web Service Local Cleanup Script
# This script removes the locally deployed Management Web Service from IIS
# Target: Web Service only (ManagementWeb project built with MSBuild)

param(
    [string]$IISSiteName = "ftpauth",
    [string]$IISAppPoolName = "ftpauth-pool",
    [switch]$Force
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Colors for output
$Host.UI.RawUI.ForegroundColor = "White"
function Write-Info { param($Message) Write-Host "[INFO] $Message" -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host "[SUCCESS] $Message" -ForegroundColor Green }
function Write-Warning { param($Message) Write-Host "[WARNING] $Message" -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host "[ERROR] $Message" -ForegroundColor Red }

Write-Info "Starting Web Service local cleanup process..."

# Function to check if running as Administrator
function Test-Administrator {
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    if (-not $isAdmin) {
        Write-Error "This script must be run as Administrator for IIS operations"
        exit 1
    }
    Write-Success "Running as Administrator"
}

# Function to cleanup IIS resources
function Remove-IISResources {
    Write-Info "Removing IIS resources..."
    
    # Import IIS module
    Import-Module WebAdministration -ErrorAction Stop
    
    # Stop and remove website if it exists
    if (Test-Path "IIS:\Sites\$IISSiteName") {
        Write-Info "Stopping website: $IISSiteName"
        try {
            Stop-Website -Name $IISSiteName -ErrorAction Stop
            Write-Success "Website stopped: $IISSiteName"
        } catch {
            Write-Warning "Failed to stop website: $($_.Exception.Message)"
        }
        
        Write-Info "Removing website: $IISSiteName"
        try {
            Remove-Website -Name $IISSiteName -ErrorAction Stop
            Write-Success "Website removed: $IISSiteName"
        } catch {
            Write-Error "Failed to remove website: $($_.Exception.Message)"
            exit 1
        }
    } else {
        Write-Info "Website does not exist: $IISSiteName"
    }
    
    # Remove application pool if it exists
    if (Test-Path "IIS:\AppPools\$IISAppPoolName") {
        Write-Info "Removing application pool: $IISAppPoolName"
        try {
            Remove-WebAppPool -Name $IISAppPoolName -ErrorAction Stop
            Write-Success "Application pool removed: $IISAppPoolName"
        } catch {
            Write-Error "Failed to remove application pool: $($_.Exception.Message)"
            exit 1
        }
    } else {
        Write-Info "Application pool does not exist: $IISAppPoolName"
    }
    
    Write-Success "IIS resources cleanup completed"
}

# Function to cleanup publish directory
function Remove-PublishDirectory {
    $ProjectRoot = Split-Path -Parent $PSScriptRoot
    $PublishPath = Join-Path $ProjectRoot "publish\ManagementWeb"
    
    if (Test-Path $PublishPath) {
        Write-Info "Removing publish directory: $PublishPath"
        try {
            Remove-Item $PublishPath -Recurse -Force -ErrorAction Stop
            Write-Success "Publish directory removed: $PublishPath"
        } catch {
            Write-Warning "Failed to remove publish directory: $($_.Exception.Message)"
        }
    } else {
        Write-Info "Publish directory does not exist: $PublishPath"
    }
}

# Main execution
try {
    Write-Info "=== Web Service Local Cleanup Script ==="
    Write-Info "IIS Site Name: $IISSiteName"
    Write-Info "IIS App Pool Name: $IISAppPoolName"
    Write-Info ""
    
    if (-not $Force) {
        Write-Warning "This will remove the following resources:"
        Write-Warning "- Website: $IISSiteName"
        Write-Warning "- Application Pool: $IISAppPoolName"
        Write-Warning "- Publish directory contents"
        Write-Warning ""
        $confirmation = Read-Host "Are you sure you want to continue? (y/N)"
        if ($confirmation -ne "y" -and $confirmation -ne "Y") {
            Write-Info "Cleanup cancelled by user"
            exit 0
        }
    }
    
    # Check Administrator privileges
    Test-Administrator
    
    # Remove IIS resources
    Remove-IISResources
    
    # Remove publish directory
    Remove-PublishDirectory
    
    Write-Success "Web Service local cleanup completed successfully!"
    Write-Info "All resources have been removed from IIS and local filesystem"
    
} catch {
    Write-Error "Cleanup failed: $($_.Exception.Message)"
    Write-Error "Stack trace: $($_.ScriptStackTrace)"
    exit 1
}
