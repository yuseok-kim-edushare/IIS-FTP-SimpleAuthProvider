# IIS FTP SimpleAuthProvider - Web Service Local Deployment Script
# This script builds, deploys, and tests the Management Web Service locally on IIS
# Target: Web Service only (ManagementWeb project)

param(
    [string]$Configuration = "Release",
    [string]$IISSiteName = "ftpauth",
    [string]$IISAppPoolName = "ftpauth-pool",
    [string]$Port = "8080",
    [switch]$SkipBuild,
    [switch]$SkipDeploy,
    [switch]$SkipTest
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Configuration variables
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$WebProjectPath = Join-Path $ProjectRoot "src\ManagementWeb"
$PublishPath = Join-Path $ProjectRoot "publish\ManagementWeb"
$SolutionPath = Join-Path $ProjectRoot "IIS-FTP-SimpleAuthProvider.slnx"

# Colors for output
$Host.UI.RawUI.ForegroundColor = "White"
function Write-Info { param($Message) Write-Host "[INFO] $Message" -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host "[SUCCESS] $Message" -ForegroundColor Green }
function Write-Warning { param($Message) Write-Host "[WARNING] $Message" -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host "[ERROR] $Message" -ForegroundColor Red }

Write-Info "Starting Web Service local deployment process..."
Write-Info "Project Root: $ProjectRoot"
Write-Info "Web Project Path: $WebProjectPath"
Write-Info "Configuration: $Configuration"

# Function to check prerequisites
function Test-Prerequisites {
    Write-Info "Checking prerequisites..."
    
    # Check if running as Administrator
    $isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")
    if (-not $isAdmin) {
        Write-Error "This script must be run as Administrator for IIS operations"
        exit 1
    }
    
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
    
    # Check if IIS is available
    try {
        # Try multiple methods to detect IIS
        $iisDetected = $false
        
        # Method 1: Check Windows Features (Server editions)
        try {
            $iisFeature = Get-WindowsFeature -Name "Web-Server" -ErrorAction SilentlyContinue
            if ($iisFeature -and $iisFeature.InstallState -eq "Installed") {
                Write-Success "IIS is installed (detected via Windows Features)"
                $iisDetected = $true
            }
        } catch {
            Write-Info "Windows Features check not available (this is normal on Windows 10/11)"
        }
        
        # Method 2: Check if IIS service exists and is running
        try {
            $iisService = Get-Service -Name "W3SVC" -ErrorAction SilentlyContinue
            if ($iisService) {
                Write-Success "IIS service found: $($iisService.Name) (Status: $($iisService.Status))"
                $iisDetected = $true
            }
        } catch {
            Write-Info "IIS service check failed"
        }
        
        # Method 3: Check if IIS module can be imported
        try {
            $iisModule = Get-Module -ListAvailable -Name "WebAdministration" -ErrorAction SilentlyContinue
            if ($iisModule) {
                Write-Success "IIS WebAdministration module is available"
                $iisDetected = $true
            }
        } catch {
            Write-Info "IIS module check failed"
        }
        
        # Method 4: Check if IIS is listening on common ports
        try {
            $iisPorts = @(80, 443, 8080)
            foreach ($port in $iisPorts) {
                $connection = Get-NetTCPConnection -LocalPort $port -ErrorAction SilentlyContinue | Where-Object { $_.State -eq "Listen" }
                if ($connection) {
                    Write-Success "IIS appears to be listening on port $port"
                    $iisDetected = $true
                    break
                }
            }
        } catch {
            Write-Info "Port check failed"
        }
        
        if (-not $iisDetected) {
            Write-Warning "IIS installation could not be verified automatically"
            Write-Warning "This script will attempt to continue, but IIS operations may fail"
            Write-Warning "If you encounter issues, please ensure IIS is properly installed"
        } else {
            Write-Success "IIS installation verified"
        }
        
    } catch {
        Write-Warning "IIS detection encountered an error: $($_.Exception.Message)"
        Write-Warning "Script will continue, but IIS operations may fail"
    }
    
    Write-Success "All prerequisites are met"
    
    # Check if IIS WebAdministration module is available
    try {
        $iisModule = Get-Module -ListAvailable -Name "WebAdministration"
        if (-not $iisModule) {
            Write-Warning "IIS WebAdministration module not found"
            Write-Warning "This may prevent IIS deployment operations"
            Write-Info "To install IIS on Windows 10/11:"
            Write-Info "1. Open 'Turn Windows features on or off'"
            Write-Info "2. Check 'Internet Information Services'"
            Write-Info "3. Expand and check 'World Wide Web Services' > 'Application Development Features'"
            Write-Info "4. Check 'IIS 6 Management Compatibility' > 'IIS 6 Management Console'"
            Write-Info "5. Click OK and restart if prompted"
        }
    } catch {
        Write-Warning "Could not check IIS module availability"
    }
}

# Function to build and publish the web project
function Build-WebProject {
    if ($SkipBuild) {
        Write-Warning "Skipping build as requested"
        return
    }
    
    Write-Info "Building and publishing Web Service project..."
    
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
    
    # Build and publish using MSBuild
    Write-Info "Building and publishing using MSBuild..."
    try {
        # Change to the web project directory
        Push-Location $WebProjectPath
        
        # Clean any previous builds
        Write-Info "Cleaning previous build artifacts..."
        & $msbuildPath "ManagementWeb.csproj" "/t:Clean" "/p:Configuration=$Configuration" "/verbosity:minimal" | Out-Null
        
        # Build and publish the project
        Write-Info "Building and publishing project..."
        $buildResult = & $msbuildPath "ManagementWeb.csproj" `
            "/p:Configuration=$Configuration" `
            "/p:Platform=`"Any CPU`"" `
            "/p:DeployOnBuild=true" `
            "/p:WebPublishMethod=Package" `
            "/p:PackageAsSingleFile=true" `
            "/p:PackageLocation=`"$PublishPath`"" `
            "/verbosity:minimal"
        
        if ($LASTEXITCODE -eq 0) {
            Write-Success "Project built and published successfully using MSBuild"
            Pop-Location
        } else {
            Write-Error "MSBuild build failed with exit code: $LASTEXITCODE"
            Write-Error "Build output: $buildResult"
            Pop-Location
            exit 1
        }
    } catch {
        Write-Error "MSBuild build failed with exception: $($_.Exception.Message)"
        Pop-Location
        exit 1
    }
    
    # Verify published output
    Write-Info "Verifying published output..."
    
    # Check essential files for web application
    $requiredFiles = @("Web.config", "Global.asax")
    $missingFiles = @()
    
    foreach ($file in $requiredFiles) {
        $filePath = Join-Path $PublishPath $file
        if (-not (Test-Path $filePath)) {
            Write-Error "Missing required file: $file"
            $missingFiles += $file
        }
    }
    
    if ($missingFiles.Count -gt 0) {
        Write-Error "Published output verification failed. Missing files: $($missingFiles -join ', ')"
        exit 1
    }
    
    # Check for web application structure
    $hasBin = Test-Path (Join-Path $PublishPath "bin")
    $hasViews = Test-Path (Join-Path $PublishPath "Views")
    $hasControllers = Test-Path (Join-Path $PublishPath "Controllers")
    $hasContent = Test-Path (Join-Path $PublishPath "Content")
    $hasScripts = Test-Path (Join-Path $PublishPath "Scripts")
    $hasAppStart = Test-Path (Join-Path $PublishPath "App_Start")
    
    Write-Info "Published output structure:"
    Write-Info "  - bin directory: $(if ($hasBin) { 'Found' } else { 'Missing' })"
    Write-Info "  - Views: $(if ($hasViews) { 'Found' } else { 'Missing' })"
    Write-Info "  - Controllers: $(if ($hasControllers) { 'Found' } else { 'Missing' })"
    Write-Info "  - Content: $(if ($hasContent) { 'Found' } else { 'Missing' })"
    Write-Info "  - Scripts: $(if ($hasScripts) { 'Found' } else { 'Missing' })"
    Write-Info "  - App_Start: $(if ($hasAppStart) { 'Found' } else { 'Missing' })"
    
    # Count files in key directories
    if ($hasBin) {
        $binFiles = Get-ChildItem (Join-Path $PublishPath "bin") -Recurse -File | Measure-Object | Select-Object -ExpandProperty Count
        Write-Info "  - bin contains $binFiles files"
    }
    
    if ($hasViews) {
        $viewFiles = Get-ChildItem (Join-Path $PublishPath "Views") -Recurse -File | Measure-Object | Select-Object -ExpandProperty Count
        Write-Info "  - Views contains $viewFiles files"
    }
    
    if ($hasAppStart) {
        $appStartFiles = Get-ChildItem (Join-Path $PublishPath "App_Start") -Recurse -File | Measure-Object | Select-Object -ExpandProperty Count
        Write-Info "  - App_Start contains $appStartFiles files"
    }
    
    # Validate minimum requirements
    if (-not $hasBin) {
        Write-Error "bin directory is missing - this is required for .NET Framework web applications"
        exit 1
    }
    
    if (-not $hasViews) {
        Write-Warning "Views directory is missing - this may cause runtime errors"
    }
    
    if (-not $hasAppStart) {
        Write-Warning "App_Start directory is missing - this may cause runtime errors for MVC configuration"
    }
    
    Write-Success "Published output verification completed successfully"
    Write-Success "Build and publish completed successfully"
}

# Function to deploy to IIS
function Deploy-ToIIS {
    if ($SkipDeploy) {
        Write-Warning "Skipping deployment as requested"
        return
    }
    
    Write-Info "Deploying Web Service to IIS..."
    
    # Import IIS module
    try {
        Import-Module WebAdministration -ErrorAction Stop
        Write-Success "IIS WebAdministration module imported successfully"
    } catch {
        Write-Error "Failed to import IIS WebAdministration module: $($_.Exception.Message)"
        Write-Error "This usually means IIS is not properly installed or configured"
        Write-Error "Please ensure IIS is installed with the Web Administration Tools"
        exit 1
    }
    
    # Create application pool if it doesn't exist
    if (-not (Test-Path "IIS:\AppPools\$IISAppPoolName")) {
        Write-Info "Creating application pool: $IISAppPoolName"
        New-WebAppPool -Name $IISAppPoolName
        Set-ItemProperty "IIS:\AppPools\$IISAppPoolName" -Name "managedRuntimeVersion" -Value "v4.0"
        Set-ItemProperty "IIS:\AppPools\$IISAppPoolName" -Name "processModel" -Value @{identityType="ApplicationPoolIdentity"}
        Write-Success "Application pool created: $IISAppPoolName"
    } else {
        Write-Info "Application pool already exists: $IISAppPoolName"
    }
    
    # Create website if it doesn't exist
    if (-not (Test-Path "IIS:\Sites\$IISSiteName")) {
        Write-Info "Creating website: $IISSiteName"
        New-Website -Name $IISSiteName -Port $Port -PhysicalPath $PublishPath -ApplicationPool $IISAppPoolName
        Write-Success "Website created: $IISSiteName on port $Port"
    } else {
        Write-Info "Website already exists: $IISSiteName"
        # Update the physical path and application pool
        Set-ItemProperty "IIS:\Sites\$IISSiteName" -Name "physicalPath" -Value $PublishPath
        Set-ItemProperty "IIS:\Sites\$IISSiteName" -Name "applicationPool" -Value $IISAppPoolName
        Write-Info "Website updated with new physical path and application pool"
    }
    
    # Start the website if it's not running
    $site = Get-Website -Name $IISSiteName
    if ($site.State -ne "Started") {
        Write-Info "Starting website: $IISSiteName"
        Start-Website -Name $IISSiteName
        Write-Success "Website started: $IISSiteName"
    } else {
        Write-Info "Website is already running: $IISSiteName"
    }
    
    Write-Success "Deployment completed successfully"
}

# Function to test the web service
function Test-WebService {
    if ($SkipTest) {
        Write-Warning "Skipping test as requested"
        return
    }
    
    Write-Info "Testing Web Service..."
    
    $baseUrl = "http://localhost:$Port"
    $testUrls = @(
        "$baseUrl/",
        "$baseUrl/Account/Login",
        "$baseUrl/Home/Index"
    )
    
    foreach ($url in $testUrls) {
        try {
            Write-Info "Testing URL: $url"
            $response = Invoke-WebRequest -Uri $url -Method Get -TimeoutSec 30 -ErrorAction Stop
            
            if ($response.StatusCode -eq 200) {
                Write-Success "URL accessible: $url (Status: $($response.StatusCode))"
            } else {
                Write-Warning "URL returned unexpected status: $url (Status: $($response.StatusCode))"
            }
        } catch {
            Write-Error "Failed to access URL: $url - $($_.Exception.Message)"
        }
    }
    
    # Test if the website is responding
    try {
        $pingResponse = Invoke-WebRequest -Uri $baseUrl -Method Head -TimeoutSec 10 -ErrorAction Stop
        Write-Success "Web Service is responding on $baseUrl"
        Write-Info "You can now open your browser and navigate to: $baseUrl"
    } catch {
        Write-Error "Web Service is not responding on $baseUrl"
        Write-Info "Please check IIS configuration and application pool status"
    }
}

# Function to show deployment summary
function Show-DeploymentSummary {
    Write-Info "=== Deployment Summary ==="
    Write-Info "Website Name: $IISSiteName"
    Write-Info "Application Pool: $IISAppPoolName"
    Write-Info "Port: $Port"
    Write-Info "Physical Path: $PublishPath"
    Write-Info "Access URL: http://localhost:$Port"
    Write-Info ""
    Write-Info "To manage the website:"
    Write-Info "1. Open IIS Manager (inetmgr)"
    Write-Info "2. Navigate to Sites > $IISSiteName"
    Write-Info "3. Browse the website or manage application settings"
    Write-Info ""
    Write-Info "To stop the website: Stop-Website -Name '$IISSiteName'"
    Write-Info "To remove the website: Remove-Website -Name '$IISSiteName'"
    Write-Info "To remove the app pool: Remove-WebAppPool -Name '$IISAppPoolName'"
}

# Main execution
try {
    Write-Info "=== Web Service Local Deployment Script ==="
    Write-Info "Configuration: $Configuration"
    Write-Info "IIS Site Name: $IISSiteName"
    Write-Info "IIS App Pool Name: $IISAppPoolName"
    Write-Info "Port: $Port"
    Write-Info ""
    
    # Check prerequisites
    Test-Prerequisites
    
    # Build the project
    Build-WebProject
    
    # Deploy to IIS
    Deploy-ToIIS
    
    # Test the web service
    Test-WebService
    
    # Show summary
    Show-DeploymentSummary
    
    Write-Success "Web Service deployment completed successfully!"
    
} catch {
    Write-Error "Deployment failed: $($_.Exception.Message)"
    Write-Error "Stack trace: $($_.ScriptStackTrace)"
    exit 1
}
