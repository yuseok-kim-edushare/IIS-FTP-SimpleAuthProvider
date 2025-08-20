# IIS FTP SimpleAuthProvider 배포 스크립트
param(
    [string]$IISPath = "C:\inetpub\wwwroot\ftpauth",
    [string]$SourcePath = "src\ManagementWeb\bin\Release\net48",
    [string]$AuthProviderPath = "src\AuthProvider\bin\Release\net48",
    [string]$BackupPath = "C:\inetpub\backup\ftpauth",
    [switch]$CreateAppPool,
    [switch]$CreateSite,
    [switch]$Force
)

Write-Host "IIS FTP SimpleAuthProvider 배포를 시작합니다..." -ForegroundColor Green

# 관리자 권한 확인
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "이 스크립트는 관리자 권한으로 실행해야 합니다."
    exit 1
}

# IIS 기능 확인
try {
    Import-Module WebAdministration
} catch {
    Write-Error "IIS 관리 모듈을 로드할 수 없습니다. IIS가 설치되어 있는지 확인하세요."
    exit 1
}

# 기존 배포 백업
if (Test-Path $IISPath) {
    if (!$Force) {
        $response = Read-Host "기존 배포가 발견되었습니다. 백업하시겠습니까? (y/n)"
        if ($response -eq 'y' -or $response -eq 'Y') {
            $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
            $backupPathWithTimestamp = "$BackupPath\$timestamp"
            
            if (!(Test-Path $BackupPath)) {
                New-Item -ItemType Directory -Path $BackupPath -Force
            }
            
            Copy-Item $IISPath -Destination $backupPathWithTimestamp -Recurse -Force
            Write-Host "기존 배포를 백업했습니다: $backupPathWithTimestamp" -ForegroundColor Yellow
        }
    }
}

# 대상 디렉토리 생성/정리
if (Test-Path $IISPath) {
    if ($Force) {
        Remove-Item $IISPath -Recurse -Force
        Write-Host "기존 배포를 제거했습니다." -ForegroundColor Yellow
    }
}
New-Item -ItemType Directory -Path $IISPath -Force | Out-Null

# 파일 복사
Write-Host "파일을 복사하는 중..." -ForegroundColor Yellow
Copy-Item "$SourcePath\*" -Destination $IISPath -Recurse -Force

# AuthProvider DLL 복사 (IIS 시스템 디렉토리)
$IISSystemPath = "C:\Windows\System32\inetsrv"
if (Test-Path $IISSystemPath) {
    # 기존 DLL 백업
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
            Write-Host "$dll을 IIS 시스템 디렉토리에 복사했습니다." -ForegroundColor Yellow
        }
    }
}

# 사용자 데이터 디렉토리 생성
$UserDataPath = "C:\inetpub\ftpusers"
if (!(Test-Path $UserDataPath)) {
    New-Item -ItemType Directory -Path $UserDataPath -Force
    Write-Host "사용자 데이터 디렉토리를 생성했습니다: $UserDataPath" -ForegroundColor Yellow
}

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
    Write-Host "샘플 사용자 파일을 생성했습니다: $SampleUsersPath" -ForegroundColor Yellow
}

# IIS 애플리케이션 풀 생성 (선택사항)
if ($CreateAppPool) {
    $appPoolName = "ftpauth-pool"
    if (!(Get-IISAppPool -Name $appPoolName -ErrorAction SilentlyContinue)) {
        New-WebAppPool -Name $appPoolName
        Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value "v4.0"
        Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
        Write-Host "애플리케이션 풀을 생성했습니다: $appPoolName" -ForegroundColor Yellow
    }
}

# IIS 사이트 생성 (선택사항)
if ($CreateSite) {
    $siteName = "ftpauth"
    if (!(Get-Website -Name $siteName -ErrorAction SilentlyContinue)) {
        New-Website -Name $siteName -PhysicalPath $IISPath -Port 8080
        Write-Host "웹사이트를 생성했습니다: $siteName (포트 8080)" -ForegroundColor Yellow
    }
}

# 권한 설정
$acl = Get-Acl $UserDataPath
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl -Path $UserDataPath -AclObject $acl

# 배포 정보 기록
$deploymentInfo = @{
    DeployedAt = Get-Date
    Version = "1.0.0"
    SourcePath = $SourcePath
    AuthProviderPath = $AuthProviderPath
    BackupPath = $BackupPath
} | ConvertTo-Json

$deploymentInfoPath = Join-Path $IISPath "deployment-info.json"
$deploymentInfo | Out-File -FilePath $deploymentInfoPath -Encoding UTF8

Write-Host "배포가 완료되었습니다!" -ForegroundColor Green
Write-Host "웹 관리 콘솔: http://localhost:8080" -ForegroundColor Cyan
Write-Host "기본 관리자 계정: admin / admin123" -ForegroundColor Cyan
Write-Host "사용자 데이터 경로: $UserDataPath" -ForegroundColor Cyan
Write-Host "배포 정보: $deploymentInfoPath" -ForegroundColor Cyan