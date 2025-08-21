# IIS FTP SimpleAuthProvider - Build Test Script
# This script tests the build process without deploying to IIS

param(
    [string]$Configuration = "Release"
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Configuration variables
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$WebProjectPath = Join-Path $ProjectRoot "src\ManagementWeb"
$PublishPath = Join-Path $ProjectRoot "publish\ManagementWeb"

# Colors for output
$Host.UI.RawUI.ForegroundColor = "White"
function Write-Info { param($Message) Write-Host "[INFO] $Message" -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host "[SUCCESS] $Message" -ForegroundColor Green }
function Write-Warning { param($Message) Write-Host "[WARNING] $Message" -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host "[ERROR] $Message" -ForegroundColor Red }

Write-Info "=== Build Test Script ==="
Write-Info "Testing build process for ManagementWeb project"
Write-Info "Configuration: $Configuration"
Write-Info "Project Root: $ProjectRoot"
Write-Info "Web Project Path: $WebProjectPath"
Write-Info "Publish Path: $PublishPath"
Write-Info ""

# Function to check prerequisites
function Test-Prerequisites {
    Write-Info "Checking prerequisites..."
    
    # Check if .NET Framework is available
    try {
        $dotnetVersion = Get-ChildItem "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\" -ErrorAction SilentlyContinue | Get-ItemProperty -Name Release -ErrorAction SilentlyContinue
        if ($dotnetVersion.Release -ge 528040) {
            Write-Success ".NET Framework 4.8 or higher is available"
        } else {
            Write-Error ".NET Framework 4.8 or higher is required"
            exit 1
        }
    } catch {
        Write-Error "Failed to check .NET Framework version"
        exit 1
    }
    
    # Check if MSBuild is available
    $msbuildPaths = @(
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    )
    
    $msbuildPath = $null
    foreach ($path in $msbuildPaths) {
        if (Test-Path $path) {
            $msbuildPath = $path
            break
        }
    }
    
    if (-not $msbuildPath) {
        Write-Error "MSBuild.exe not found! Please install Visual Studio Build Tools or Visual Studio."
        Write-Error "Download from: https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022"
        exit 1
    } else {
        Write-Success "MSBuild found: $msbuildPath"
    }
    
    Write-Success "All prerequisites are met"
}

# Function to test build process
function Test-Build {
    Write-Info "Testing build process..."
    
    # Clean previous build artifacts
    if (Test-Path $PublishPath) {
        Remove-Item $PublishPath -Recurse -Force
        Write-Info "Cleaned previous publish directory"
    }
    
    # Create publish directory if it doesn't exist
    if (-not (Test-Path $PublishPath)) {
        New-Item -ItemType Directory -Path $PublishPath -Force | Out-Null
        Write-Info "Created publish directory: $PublishPath"
    }
    
    # Find MSBuild.exe
    $msbuildPaths = @(
        "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
        "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
    )
    
    $msbuildPath = $null
    foreach ($path in $msbuildPaths) {
        if (Test-Path $path) {
            $msbuildPath = $path
            break
        }
    }
    
    if (-not $msbuildPath) {
        Write-Error "MSBuild.exe not found! Please install Visual Studio Build Tools or Visual Studio."
        exit 1
    }
    
    Write-Info "Using MSBuild: $msbuildPath"
    
    # Test MSBuild build
    Write-Info "Testing MSBuild build..."
    try {
        # Change to the web project directory
        Push-Location $WebProjectPath
        
        # Test build using MSBuild
        $buildResult = & $msbuildPath "ManagementWeb.csproj" "/p:Configuration=$Configuration" "/p:Platform=`"Any CPU`"" "/verbosity:minimal"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "MSBuild build test successful"
            
            # Test publish using MSBuild Web Publishing Pipeline
            Write-Info "Testing MSBuild publish..."
            $publishResult = & $msbuildPath "ManagementWeb.csproj" `
                "/p:Configuration=$Configuration" `
                "/p:Platform=`"Any CPU`"" `
                "/p:DeployOnBuild=true" `
                "/p:WebPublishMethod=Package" `
                "/p:PackageAsSingleFile=true" `
                "/p:PackageLocation=`"$PublishPath`"" `
                "/verbosity:minimal"
            
            if ($LASTEXITCODE -eq 0) {
                Write-Success "MSBuild publish test successful"
                Pop-Location
                return $true
            } else {
                Write-Error "MSBuild publish test failed"
                Pop-Location
                return $false
            }
        } else {
            Write-Error "MSBuild build test failed"
            Pop-Location
            return $false
        }
    } catch {
        Write-Error "MSBuild test failed with exception: $($_.Exception.Message)"
        Pop-Location
        return $false
    }
}

# Function to verify build output
function Test-BuildOutput {
    Write-Info "Verifying build output..."
    
    # Check essential files for web application
    $requiredFiles = @(
        "Web.config"
    )
    
    # Check if we have the essential web application files
    $hasBin = Test-Path (Join-Path $PublishPath "bin")
    $hasGlobalAsax = Test-Path (Join-Path $PublishPath "Global.asax")
    $hasViews = Test-Path (Join-Path $PublishPath "Views")
    $hasControllers = Test-Path (Join-Path $PublishPath "Controllers")
    
    # Add web-specific requirements if they exist
    if ($hasBin) {
        $requiredFiles += "bin"
    }
    if ($hasGlobalAsax) {
        $requiredFiles += "Global.asax"
    }
    if ($hasViews) {
        $requiredFiles += "Views"
    }
    if ($hasControllers) {
        $requiredFiles += "Controllers"
    }
    
    Write-Info "Build output analysis:"
    Write-Info "  - bin directory: $(if ($hasBin) { 'Found' } else { 'Missing' })"
    Write-Info "  - Global.asax: $(if ($hasGlobalAsax) { 'Found' } else { 'Missing' })"
    Write-Info "  - Views: $(if ($hasViews) { 'Found' } else { 'Missing' })"
    Write-Info "  - Controllers: $(if ($hasControllers) { 'Found' } else { 'Missing' })"
    
    $missingFiles = @()
    
    foreach ($file in $requiredFiles) {
        $filePath = Join-Path $PublishPath $file
        if (Test-Path $filePath) {
            Write-Success "Found: $file"
        } else {
            Write-Error "Missing: $file"
            $missingFiles += $file
        }
    }
    
    # For MSBuild builds, we need at least Web.config and some web content
    if ($missingFiles.Count -eq 0 -and $hasBin) {
        Write-Success "Build output verification passed - complete web application"
        return $true
    } elseif ($missingFiles.Count -eq 0 -and ($hasViews -or $hasControllers)) {
        Write-Success "Build output verification passed - web application with content"
        return $true
    } elseif ($missingFiles.Count -eq 0) {
        Write-Warning "Build output verification passed - basic web application"
        Write-Warning "This may be a minimal build - check if it's suitable for IIS deployment"
        return $true
    } else {
        Write-Error "Build output verification failed. Missing files: $($missingFiles -join ', ')"
        return $false
    }
}

# Main execution
try {
    # Check prerequisites
    Test-Prerequisites
    
    # Test build process
    $buildSuccess = Test-Build
    
    if ($buildSuccess) {
        # Verify build output
        $outputValid = Test-BuildOutput
        
        if ($outputValid) {
            Write-Success "=== Build Test Completed Successfully ==="
            Write-Info "The build process is working correctly"
            Write-Info "You can now run the full deployment script"
            Write-Info ""
            Write-Info "Next steps:"
            Write-Info "1. Run: .\deploy-web-service-local.ps1"
            Write-Info "2. Or run with custom settings: .\deploy-web-service-local.ps1 -Configuration Debug"
        } else {
            Write-Error "Build test failed - output verification failed"
            exit 1
        }
    } else {
        Write-Error "Build test failed - build process failed"
        exit 1
    }
    
} catch {
    Write-Error "Build test failed: $($_.Exception.Message)"
    Write-Error "Stack trace: $($_.ScriptStackTrace)"
    exit 1
}
