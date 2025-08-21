# IIS FTP SimpleAuthProvider ���� ���� ��ũ��Ʈ
# �� ��ũ��Ʈ�� ��ü �ý����� �� ���� �����մϴ�.

param(
    [switch]$SkipBuild,
    [switch]$Force,
    [switch]$CreateAppPool,
    [switch]$CreateSite
)

# Set error action preference
$ErrorActionPreference = "Stop"

# Configuration variables
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$IISPath = "C:\inetpub\wwwroot\ftpauth"
$BackupPath = "C:\inetpub\backup\ftpauth"
$UserDataPath = "C:\inetpub\ftpusers"
$SourcePath = "src\ManagementWeb\bin\Release\net48"
$AuthProviderPath = "src\AuthProvider\bin\Release\net48"

# Colors for output
$Host.UI.RawUI.ForegroundColor = "White"
function Write-Info { param($Message) Write-Host "[INFO] $Message" -ForegroundColor Cyan }
function Write-Success { param($Message) Write-Host "[SUCCESS] $Message" -ForegroundColor Green }
function Write-Warning { param($Message) Write-Host "[WARNING] $Message" -ForegroundColor Yellow }
function Write-Error { param($Message) Write-Host "[ERROR] $Message" -ForegroundColor Red }

Write-Info "=== IIS FTP SimpleAuthProvider ���� ���� ��ũ��Ʈ ==="
Write-Info "�� ��ũ��Ʈ�� ��ü �ý����� �����մϴ�."
Write-Info ""

# 1�ܰ�: ������Ʈ ����
if (!$SkipBuild) {
    Write-Host "`n=== 1�ܰ�: ������Ʈ ���� ===" -ForegroundColor Yellow
    
    # MSBuild ��� Ȯ��
    Write-Host "MSBuild�� Ȯ���ϴ� ��..." -ForegroundColor Yellow
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
        Write-Error "MSBuild.exe�� ã�� �� �����ϴ�. Visual Studio Build Tools �Ǵ� Visual Studio�� ��ġ���ּ���."
        Write-Error "�ٿ�ε�: https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022"
        exit 1
    }

    Write-Host "MSBuild ���: $msbuildPath" -ForegroundColor Green
    
    Set-Location $projectRoot
    Write-Host "������Ʈ ��Ʈ: $projectRoot" -ForegroundColor Cyan
    
    try {
        Write-Host "������Ʈ�� �����ϴ� ��..." -ForegroundColor Yellow
        & $msbuildPath "IIS-FTP-SimpleAuthProvider.slnx" "/p:Configuration=Release" "/p:Platform=`"Any CPU`"" "/verbosity:minimal"
        if ($LASTEXITCODE -ne 0) {
            Write-Error "���忡 �����߽��ϴ�."
            exit 1
        }
        Write-Host "? ���� �Ϸ�" -ForegroundColor Green
    } catch {
        Write-Error "���� �� ������ �߻��߽��ϴ�: $($_.Exception.Message)"
        exit 1
    }
} else {
    Write-Host "`n=== 1�ܰ�: ���� �ǳʶٱ� ===" -ForegroundColor Yellow
    Write-Host "����ڰ� ���带 �ǳʶٵ��� �����߽��ϴ�." -ForegroundColor Cyan
}

# 2�ܰ�: ���� ���� ���
Write-Host "`n=== 2�ܰ�: ���� ���� ��� ===" -ForegroundColor Yellow
if (Test-Path $IISPath) {
    if (!$Force) {
        $response = Read-Host "���� ������ �߰ߵǾ����ϴ�. ����Ͻðڽ��ϱ�? (y/n)"
        if ($response -eq 'y' -or $response -eq 'Y') {
            $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
            $backupPathWithTimestamp = "$BackupPath\$timestamp"
            
            if (!(Test-Path $BackupPath)) {
                New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
            }
            
            Copy-Item $IISPath -Destination $backupPathWithTimestamp -Recurse -Force
            Write-Host "? ���� ������ ����߽��ϴ�: $backupPathWithTimestamp" -ForegroundColor Green
        }
    }
}

# 3�ܰ�: �� ���ø����̼� ����
Write-Host "`n=== 3�ܰ�: �� ���ø����̼� ���� ===" -ForegroundColor Yellow
if (Test-Path $IISPath) {
    if ($Force) {
        Remove-Item $IISPath -Recurse -Force
        Write-Host "���� ������ �����߽��ϴ�." -ForegroundColor Yellow
    }
}
New-Item -ItemType Directory -Path $IISPath -Force | Out-Null

Write-Host "�� ���ø����̼� ������ �����ϴ� ��..." -ForegroundColor Yellow
Copy-Item "$SourcePath\*" -Destination $IISPath -Recurse -Force
Write-Host "? �� ���ø����̼� ���� �Ϸ�" -ForegroundColor Green

# 4�ܰ�: AuthProvider DLL ����
Write-Host "`n=== 4�ܰ�: AuthProvider DLL ���� ===" -ForegroundColor Yellow
$IISSystemPath = "C:\Windows\System32\inetsrv"
$authProviderDlls = @(
    "IIS.Ftp.SimpleAuth.Provider.dll",
    "IIS.Ftp.SimpleAuth.Core.dll",
    "WelsonJS.Esent.dll",
    "Esent.Interop.dll"
)

foreach ($dll in $authProviderDlls) {
    $sourceDll = Join-Path $AuthProviderPath $dll
    $targetDll = Join-Path $IISSystemPath $dll
    
    if (Test-Path $sourceDll) {
        if (Test-Path $targetDll) {
            $backupDll = Join-Path $BackupPath "$dll.backup"
            Copy-Item $targetDll -Destination $backupDll -Force
            Write-Host "���� $dll�� ����߽��ϴ�." -ForegroundColor Yellow
        }
        
        Copy-Item $sourceDll -Destination $targetDll -Force
        Write-Host "? $dll�� IIS �ý��� ���丮�� �����߽��ϴ�." -ForegroundColor Green
    } else {
        Write-Warning "�ҽ� DLL�� ã�� �� �����ϴ�: $sourceDll" -ForegroundColor Yellow
    }
}

# 5�ܰ�: ����� ������ ���丮 �� ���� ���� ����
Write-Host "`n=== 5�ܰ�: ����� ������ �� ���� ���� ���� ===" -ForegroundColor Yellow
if (!(Test-Path $UserDataPath)) {
    New-Item -ItemType Directory -Path $UserDataPath -Force | Out-Null
    Write-Host "����� ������ ���丮�� �����߽��ϴ�: $UserDataPath" -ForegroundColor Green
}

# ���� ���� ����
$configPath = "C:\Windows\System32\inetsrv\ftpauth.config.json"
$config = @{
    UserStore = @{
        Type = "Json"
        Path = "$UserDataPath\users.json"
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
}

$configJson = $config | ConvertTo-Json -Depth 10
Set-Content -Path $configPath -Value $configJson -Encoding UTF8
Write-Host "? ���� ������ �����߽��ϴ�: $configPath" -ForegroundColor Green

# �⺻ ����� ���� ����
$usersPath = "$UserDataPath\users.json"
if (!(Test-Path $usersPath)) {
    $defaultUsers = @{
        Users = @(
            @{
                Username = "admin"
                PasswordHash = "$2a$10$92IXUNpkjO0rOQ5byMi.Ye4oKoEa3Ro9llC/.og/at2.uheWG/igi" # "password"
                DisplayName = "Administrator"
                HomeDirectory = "/"
                Permissions = @(
                    @{
                        Path = "/"
                        Read = $true
                        Write = $true
                    }
                )
                IsActive = $true
                CreatedAt = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        )
    }
    
    $usersJson = $defaultUsers | ConvertTo-Json -Depth 10
    Set-Content -Path $usersPath -Value $usersJson -Encoding UTF8
    Write-Host "? �⺻ ����� ������ �����߽��ϴ�: $usersPath" -ForegroundColor Green
}

# 6�ܰ�: IIS ����
Write-Host "`n=== 6�ܰ�: IIS ���� ===" -ForegroundColor Yellow

# IIS ��� ��������
try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Success "IIS WebAdministration ����� �����Խ��ϴ�."
} catch {
    Write-Error "IIS WebAdministration ����� ������ �� �����ϴ�: $($_.Exception.Message)"
    Write-Error "IIS�� ����� ��ġ���� �ʾҰų� �������� �ʾҽ��ϴ�."
    exit 1
}

# ���ø����̼� Ǯ ����
if ($CreateAppPool) {
    $appPoolName = "ftpauth-pool"
    if (!(Test-Path "IIS:\AppPools\$appPoolName")) {
        New-WebAppPool -Name $appPoolName
        Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value "v4.0"
        Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "processModel" -Value @{identityType="ApplicationPoolIdentity"}
        Write-Host "? ���ø����̼� Ǯ�� �����߽��ϴ�: $appPoolName" -ForegroundColor Green
    } else {
        Write-Host "���ø����̼� Ǯ�� �̹� �����մϴ�: $appPoolName" -ForegroundColor Cyan
    }
}

# ������Ʈ ����
if ($CreateSite) {
    $siteName = "ftpauth"
    if (!(Test-Path "IIS:\Sites\$siteName")) {
        New-Website -Name $siteName -Port 8080 -PhysicalPath $IISPath -ApplicationPool $appPoolName
        Write-Host "? ������Ʈ�� �����߽��ϴ�: $siteName (��Ʈ 8080)" -ForegroundColor Green
    } else {
        Write-Host "������Ʈ�� �̹� �����մϴ�: $siteName" -ForegroundColor Cyan
        # ������ ��ο� ���ø����̼� Ǯ ������Ʈ
        Set-ItemProperty "IIS:\Sites\$siteName" -Name "physicalPath" -Value $IISPath
        if ($CreateAppPool) {
            Set-ItemProperty "IIS:\Sites\$siteName" -Name "applicationPool" -Value $appPoolName
        }
        Write-Host "������Ʈ�� ������Ʈ�߽��ϴ�." -ForegroundColor Cyan
    }
}

# 7�ܰ�: ���� �Ϸ�
Write-Host "`n=== 7�ܰ�: ���� �Ϸ� ===" -ForegroundColor Yellow
Write-Host "? IIS FTP SimpleAuthProvider ������ �Ϸ�Ǿ����ϴ�!" -ForegroundColor Green
Write-Host ""
Write-Host "������ ���� ���:" -ForegroundColor Cyan
Write-Host "- �� ���� �ܼ�: $IISPath" -ForegroundColor White
Write-Host "- AuthProvider DLL: $IISSystemPath" -ForegroundColor White
Write-Host "- ����� ������: $UserDataPath" -ForegroundColor White
Write-Host "- ���� ����: $configPath" -ForegroundColor White

if ($CreateSite) {
    Write-Host ""
    Write-Host "�� ���� �ֿܼ� �����Ϸ���:" -ForegroundColor Cyan
    Write-Host "http://localhost:8080" -ForegroundColor White
    Write-Host "�⺻ ������ ����: admin / password" -ForegroundColor White
}

Write-Host ""
Write-Host "���� �ܰ�:" -ForegroundColor Cyan
Write-Host "1. IIS���� FTP ����Ʈ�� �����ϼ���" -ForegroundColor White
Write-Host "2. FTP ���� �����ڸ� �����ϼ���" -ForegroundColor White
Write-Host "3. ����� ������ �����ϼ���" -ForegroundColor White
Write-Host "4. FTP ������ �׽�Ʈ�ϼ���" -ForegroundColor White
