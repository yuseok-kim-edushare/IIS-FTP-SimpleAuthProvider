# IIS FTP SimpleAuthProvider 통합 배포 스크립트
# 이 스크립트는 전체 시스템을 한 번에 배포합니다.

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

Write-Info "=== IIS FTP SimpleAuthProvider 통합 배포 스크립트 ==="
Write-Info "이 스크립트는 전체 시스템을 배포합니다."
Write-Info ""

# 1단계: 프로젝트 빌드
if (!$SkipBuild) {
    Write-Host "`n=== 1단계: 프로젝트 빌드 ===" -ForegroundColor Yellow
    
    # MSBuild 경로 확인
    Write-Host "MSBuild를 확인하는 중..." -ForegroundColor Yellow
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
        Write-Error "MSBuild.exe를 찾을 수 없습니다. Visual Studio Build Tools 또는 Visual Studio를 설치해주세요."
        Write-Error "다운로드: https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022"
        exit 1
    }

    Write-Host "MSBuild 경로: $msbuildPath" -ForegroundColor Green
    
    Set-Location $projectRoot
    Write-Host "프로젝트 루트: $projectRoot" -ForegroundColor Cyan
    
    try {
        Write-Host "프로젝트를 빌드하는 중..." -ForegroundColor Yellow
        & $msbuildPath "IIS-FTP-SimpleAuthProvider.slnx" "/p:Configuration=Release" "/p:Platform=`"Any CPU`"" "/verbosity:minimal"
        if ($LASTEXITCODE -ne 0) {
            Write-Error "빌드에 실패했습니다."
            exit 1
        }
        Write-Host "? 빌드 완료" -ForegroundColor Green
    } catch {
        Write-Error "빌드 중 오류가 발생했습니다: $($_.Exception.Message)"
        exit 1
    }
} else {
    Write-Host "`n=== 1단계: 빌드 건너뛰기 ===" -ForegroundColor Yellow
    Write-Host "사용자가 빌드를 건너뛰도록 선택했습니다." -ForegroundColor Cyan
}

# 2단계: 기존 배포 백업
Write-Host "`n=== 2단계: 기존 배포 백업 ===" -ForegroundColor Yellow
if (Test-Path $IISPath) {
    if (!$Force) {
        $response = Read-Host "기존 배포가 발견되었습니다. 백업하시겠습니까? (y/n)"
        if ($response -eq 'y' -or $response -eq 'Y') {
            $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
            $backupPathWithTimestamp = "$BackupPath\$timestamp"
            
            if (!(Test-Path $BackupPath)) {
                New-Item -ItemType Directory -Path $BackupPath -Force | Out-Null
            }
            
            Copy-Item $IISPath -Destination $backupPathWithTimestamp -Recurse -Force
            Write-Host "? 기존 배포를 백업했습니다: $backupPathWithTimestamp" -ForegroundColor Green
        }
    }
}

# 3단계: 웹 애플리케이션 배포
Write-Host "`n=== 3단계: 웹 애플리케이션 배포 ===" -ForegroundColor Yellow
if (Test-Path $IISPath) {
    if ($Force) {
        Remove-Item $IISPath -Recurse -Force
        Write-Host "기존 배포를 제거했습니다." -ForegroundColor Yellow
    }
}
New-Item -ItemType Directory -Path $IISPath -Force | Out-Null

Write-Host "웹 애플리케이션 파일을 복사하는 중..." -ForegroundColor Yellow
Copy-Item "$SourcePath\*" -Destination $IISPath -Recurse -Force
Write-Host "? 웹 애플리케이션 배포 완료" -ForegroundColor Green

# 4단계: AuthProvider DLL 배포
Write-Host "`n=== 4단계: AuthProvider DLL 배포 ===" -ForegroundColor Yellow
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
            Write-Host "기존 $dll을 백업했습니다." -ForegroundColor Yellow
        }
        
        Copy-Item $sourceDll -Destination $targetDll -Force
        Write-Host "? $dll을 IIS 시스템 디렉토리에 복사했습니다." -ForegroundColor Green
    } else {
        Write-Warning "소스 DLL을 찾을 수 없습니다: $sourceDll" -ForegroundColor Yellow
    }
}

# 5단계: 사용자 데이터 디렉토리 및 구성 파일 생성
Write-Host "`n=== 5단계: 사용자 데이터 및 구성 파일 생성 ===" -ForegroundColor Yellow
if (!(Test-Path $UserDataPath)) {
    New-Item -ItemType Directory -Path $UserDataPath -Force | Out-Null
    Write-Host "사용자 데이터 디렉토리를 생성했습니다: $UserDataPath" -ForegroundColor Green
}

# 구성 파일 생성
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
Write-Host "? 구성 파일을 생성했습니다: $configPath" -ForegroundColor Green

# 기본 사용자 파일 생성
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
    Write-Host "? 기본 사용자 파일을 생성했습니다: $usersPath" -ForegroundColor Green
}

# 6단계: IIS 구성
Write-Host "`n=== 6단계: IIS 구성 ===" -ForegroundColor Yellow

# IIS 모듈 가져오기
try {
    Import-Module WebAdministration -ErrorAction Stop
    Write-Success "IIS WebAdministration 모듈을 가져왔습니다."
} catch {
    Write-Error "IIS WebAdministration 모듈을 가져올 수 없습니다: $($_.Exception.Message)"
    Write-Error "IIS가 제대로 설치되지 않았거나 구성되지 않았습니다."
    exit 1
}

# 애플리케이션 풀 생성
if ($CreateAppPool) {
    $appPoolName = "ftpauth-pool"
    if (!(Test-Path "IIS:\AppPools\$appPoolName")) {
        New-WebAppPool -Name $appPoolName
        Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value "v4.0"
        Set-ItemProperty "IIS:\AppPools\$appPoolName" -Name "processModel" -Value @{identityType="ApplicationPoolIdentity"}
        Write-Host "? 애플리케이션 풀을 생성했습니다: $appPoolName" -ForegroundColor Green
    } else {
        Write-Host "애플리케이션 풀이 이미 존재합니다: $appPoolName" -ForegroundColor Cyan
    }
}

# 웹사이트 생성
if ($CreateSite) {
    $siteName = "ftpauth"
    if (!(Test-Path "IIS:\Sites\$siteName")) {
        New-Website -Name $siteName -Port 8080 -PhysicalPath $IISPath -ApplicationPool $appPoolName
        Write-Host "? 웹사이트를 생성했습니다: $siteName (포트 8080)" -ForegroundColor Green
    } else {
        Write-Host "웹사이트가 이미 존재합니다: $siteName" -ForegroundColor Cyan
        # 물리적 경로와 애플리케이션 풀 업데이트
        Set-ItemProperty "IIS:\Sites\$siteName" -Name "physicalPath" -Value $IISPath
        if ($CreateAppPool) {
            Set-ItemProperty "IIS:\Sites\$siteName" -Name "applicationPool" -Value $appPoolName
        }
        Write-Host "웹사이트를 업데이트했습니다." -ForegroundColor Cyan
    }
}

# 7단계: 배포 완료
Write-Host "`n=== 7단계: 배포 완료 ===" -ForegroundColor Yellow
Write-Host "? IIS FTP SimpleAuthProvider 배포가 완료되었습니다!" -ForegroundColor Green
Write-Host ""
Write-Host "배포된 구성 요소:" -ForegroundColor Cyan
Write-Host "- 웹 관리 콘솔: $IISPath" -ForegroundColor White
Write-Host "- AuthProvider DLL: $IISSystemPath" -ForegroundColor White
Write-Host "- 사용자 데이터: $UserDataPath" -ForegroundColor White
Write-Host "- 구성 파일: $configPath" -ForegroundColor White

if ($CreateSite) {
    Write-Host ""
    Write-Host "웹 관리 콘솔에 접근하려면:" -ForegroundColor Cyan
    Write-Host "http://localhost:8080" -ForegroundColor White
    Write-Host "기본 관리자 계정: admin / password" -ForegroundColor White
}

Write-Host ""
Write-Host "다음 단계:" -ForegroundColor Cyan
Write-Host "1. IIS에서 FTP 사이트를 구성하세요" -ForegroundColor White
Write-Host "2. FTP 인증 공급자를 설정하세요" -ForegroundColor White
Write-Host "3. 사용자 계정을 관리하세요" -ForegroundColor White
Write-Host "4. FTP 연결을 테스트하세요" -ForegroundColor White
