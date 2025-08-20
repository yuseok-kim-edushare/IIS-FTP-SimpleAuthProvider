# IIS FTP SimpleAuthProvider 통합 배포 및 구성 스크립트
# 이 스크립트는 전체 시스템을 한 번에 배포하고 구성합니다.

param(
    [string]$IISPath = "C:\inetpub\wwwroot\ftpauth",
    [string]$SourcePath = "src\ManagementWeb\bin\Release\net48",
    [string]$AuthProviderPath = "src\AuthProvider\bin\Release\net48",
    [string]$BackupPath = "C:\inetpub\backup\ftpauth",
    [string]$UserDataPath = "C:\inetpub\ftpusers",
    [switch]$CreateAppPool,
    [switch]$CreateSite,
    [switch]$Force,
    [switch]$SkipBuild
)

Write-Host "IIS FTP SimpleAuthProvider 통합 배포를 시작합니다..." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan

# 관리자 권한 확인
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "이 스크립트는 관리자 권한으로 실행해야 합니다."
    exit 1
}

# IIS 기능 확인
try {
    Import-Module WebAdministration
    Write-Host "? IIS 관리 모듈 로드 성공" -ForegroundColor Green
} catch {
    Write-Error "IIS 관리 모듈을 로드할 수 없습니다. IIS가 설치되어 있는지 확인하세요."
    exit 1
}

# 1단계: 프로젝트 빌드
if (-not $SkipBuild) {
    Write-Host "`n=== 1단계: 프로젝트 빌드 ===" -ForegroundColor Yellow
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $projectRoot = Split-Path -Parent $scriptDir
    
    Set-Location $projectRoot
    Write-Host "프로젝트 루트: $projectRoot" -ForegroundColor Cyan
    
    try {
        Write-Host "프로젝트를 빌드하는 중..." -ForegroundColor Yellow
        dotnet build --configuration Release
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
    Metrics = @{
        Enabled = $true
        MetricsFilePath = "C:\inetpub\ftpmetrics\ftp_metrics.prom"
        ExportIntervalSeconds = 60
    }
}

$config | ConvertTo-Json -Depth 10 | Out-File -FilePath $configPath -Encoding UTF8
Write-Host "? 구성 파일을 생성했습니다: $configPath" -ForegroundColor Green

# 샘플 사용자 파일 생성
$SampleUsersPath = "$UserDataPath\users.json"
if (!(Test-Path $SampleUsersPath)) {
    $sampleUsers = @{
        Users = @(
            @{
                Username = "admin"
                PasswordHash = "$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewdBPj4J/8Kq8QqG"
                DisplayName = "Administrator"
                HomeDirectory = "/"
                Permissions = @(
                    @{
                        Path = "/"
                        Read = $true
                        Write = $true
                        Delete = $true
                    }
                )
                IsActive = $true
                Created = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
            }
        )
    }
    
    $sampleUsers | ConvertTo-Json -Depth 10 | Out-File -FilePath $SampleUsersPath -Encoding UTF8
    Write-Host "? 샘플 사용자 파일을 생성했습니다: $SampleUsersPath" -ForegroundColor Green
}

# 6단계: IIS 애플리케이션 풀 및 사이트 생성
Write-Host "`n=== 6단계: IIS 구성 ===" -ForegroundColor Yellow
if ($CreateAppPool) {
    $appPoolName = "ftpauth-pool"
    if (!(Get-IISAppPool -Name $appPoolName -ErrorAction SilentlyContinue)) {
        New-WebAppPool -Name $appPoolName
        Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value "v4.0"
        Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
        Write-Host "? 애플리케이션 풀을 생성했습니다: $appPoolName" -ForegroundColor Green
    }
}

if ($CreateSite) {
    $siteName = "ftpauth"
    if (!(Get-Website -Name $siteName -ErrorAction SilentlyContinue)) {
        New-Website -Name $siteName -PhysicalPath $IISPath -Port 8080
        Write-Host "? 웹사이트를 생성했습니다: $siteName (포트 8080)" -ForegroundColor Green
    }
}

# 7단계: IIS FTP 커스텀 프로바이더 등록
Write-Host "`n=== 7단계: IIS FTP 커스텀 프로바이더 등록 ===" -ForegroundColor Yellow
try {
    # 커스텀 인증 활성화
    Set-WebConfigurationProperty -Filter "system.ftpServer/security/authentication/customAuthentication" -Name "enabled" -Value $true
    
    # 커스텀 인증 프로바이더 추가
    Add-WebConfigurationProperty -Filter "system.ftpServer/security/authentication/customAuthentication/providers" -Name "." -Value @{
        name = "SimpleAuth"
        enabled = $true
        type = "IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthenticationProvider"
    }
    
    # 커스텀 권한 활성화
    Set-WebConfigurationProperty -Filter "system.ftpServer/security/authorization/customAuthorization" -Name "enabled" -Value $true
    
    # 커스텀 권한 프로바이더 추가
    Add-WebConfigurationProperty -Filter "system.ftpServer/security/authorization/customAuthorization" -Name "." -Value @{
        name = "SimpleAuth"
        enabled = $true
        type = "IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthorizationProvider"
    }
    
    Write-Host "? IIS FTP 커스텀 프로바이더 등록 완료" -ForegroundColor Green
} catch {
    Write-Warning "IIS FTP 커스텀 프로바이더 등록 중 오류가 발생했습니다: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "수동으로 등록해야 할 수 있습니다." -ForegroundColor Yellow
}

# 8단계: 권한 설정
Write-Host "`n=== 8단계: 권한 설정 ===" -ForegroundColor Yellow
try {
    $acl = Get-Acl $UserDataPath
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule)
    Set-Acl -Path $UserDataPath -AclObject $acl
    Write-Host "? IIS_IUSRS 권한 설정 완료" -ForegroundColor Green
} catch {
    Write-Warning "권한 설정 중 오류가 발생했습니다: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 9단계: 배포 정보 기록
Write-Host "`n=== 9단계: 배포 정보 기록 ===" -ForegroundColor Yellow
$deploymentInfo = @{
    DeployedAt = Get-Date
    Version = "1.0.0"
    SourcePath = $SourcePath
    AuthProviderPath = $AuthProviderPath
    BackupPath = $BackupPath
    ConfigPath = $configPath
    UserDataPath = $UserDataPath
} | ConvertTo-Json

$deploymentInfoPath = Join-Path $IISPath "deployment-info.json"
$deploymentInfo | Out-File -FilePath $deploymentInfoPath -Encoding UTF8
Write-Host "? 배포 정보를 기록했습니다." -ForegroundColor Green

# 10단계: 서비스 재시작
Write-Host "`n=== 10단계: 서비스 재시작 ===" -ForegroundColor Yellow
try {
    Write-Host "IIS를 재시작하는 중..." -ForegroundColor Yellow
    iisreset /restart
    Write-Host "? IIS 재시작 완료" -ForegroundColor Green
    
    Write-Host "FTP 서비스를 재시작하는 중..." -ForegroundColor Yellow
    Restart-Service FTPSVC -Force
    Write-Host "? FTP 서비스 재시작 완료" -ForegroundColor Green
} catch {
    Write-Warning "서비스 재시작 중 오류가 발생했습니다: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "? IIS FTP SimpleAuthProvider 통합 배포가 완료되었습니다!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan

Write-Host "`n? 배포 요약:" -ForegroundColor Yellow
Write-Host "  웹 관리 콘솔: http://localhost:8080" -ForegroundColor Cyan
Write-Host "  기본 관리자 계정: admin / admin123" -ForegroundColor Cyan
Write-Host "  사용자 데이터 경로: $UserDataPath" -ForegroundColor Cyan
Write-Host "  구성 파일: $configPath" -ForegroundColor Cyan
Write-Host "  배포 정보: $deploymentInfoPath" -ForegroundColor Cyan

Write-Host "`n? 다음 단계:" -ForegroundColor Yellow
Write-Host "  1. 웹 관리 콘솔에 접속하여 시스템 상태 확인" -ForegroundColor White
Write-Host "  2. FTP 클라이언트로 연결 테스트" -ForegroundColor White
Write-Host "  3. 이벤트 로그에서 오류 메시지 확인" -ForegroundColor White
Write-Host "  4. 필요시 암호화 키 환경 변수 설정" -ForegroundColor White

Write-Host "`n? 문제 해결:" -ForegroundColor Yellow
Write-Host "  - 이벤트 로그 확인: Get-EventLog -LogName Application -Source 'IIS-FTP-SimpleAuth'" -ForegroundColor White
Write-Host "  - 배포 상태 확인: .\deploy\check-deployment-status.ps1" -ForegroundColor White
Write-Host "  - 수동 구성: IIS Manager에서 FTP 사이트 설정 확인" -ForegroundColor White
