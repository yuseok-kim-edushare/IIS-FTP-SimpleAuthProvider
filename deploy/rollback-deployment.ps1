# IIS FTP SimpleAuthProvider 배포 철회 스크립트
param(
    [string]$IISPath = "C:\inetpub\wwwroot\ftpauth",
    [string]$BackupPath = "C:\inetpub\backup\ftpauth",
    [string]$IISSystemPath = "C:\Windows\System32\inetsrv",
    [switch]$RemoveSite,
    [switch]$RemoveAppPool,
    [switch]$Force
)

Write-Host "IIS FTP SimpleAuthProvider 배포 철회를 시작합니다..." -ForegroundColor Yellow

# 관리자 권한 확인
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "이 스크립트는 관리자 권한으로 실행해야 합니다."
    exit 1
}

# IIS 기능 확인
try {
    Import-Module WebAdministration
} catch {
    Write-Error "IIS 관리 모듈을 로드할 수 없습니다."
    exit 1
}

# 배포 정보 확인
$deploymentInfoPath = Join-Path $IISPath "deployment-info.json"
if (Test-Path $deploymentInfoPath) {
    try {
        $deploymentInfo = Get-Content $deploymentInfoPath | ConvertFrom-Json
        Write-Host "배포 정보를 발견했습니다:" -ForegroundColor Cyan
        Write-Host "  배포 시간: $($deploymentInfo.DeployedAt)" -ForegroundColor Cyan
        Write-Host "  버전: $($deploymentInfo.Version)" -ForegroundColor Cyan
        Write-Host "  소스 경로: $($deploymentInfo.SourcePath)" -ForegroundColor Cyan
    } catch {
        Write-Warning "배포 정보를 읽을 수 없습니다."
    }
}

# 최신 백업 찾기
$latestBackup = Get-ChildItem -Path $BackupPath -Directory | Sort-Object CreationTime -Descending | Select-Object -First 1
if ($latestBackup) {
    Write-Host "최신 백업을 발견했습니다: $($latestBackup.Name)" -ForegroundColor Cyan
    Write-Host "  생성 시간: $($latestBackup.CreationTime)" -ForegroundColor Cyan
} else {
    Write-Warning "백업을 찾을 수 없습니다: $BackupPath"
}

# 사용자 확인
if (!$Force) {
    $response = Read-Host "정말로 배포를 철회하시겠습니까? 이 작업은 되돌릴 수 없습니다. (yes/no)"
    if ($response -ne "yes") {
        Write-Host "배포 철회가 취소되었습니다." -ForegroundColor Green
        exit 0
    }
}

# IIS 사이트 제거 (선택사항)
if ($RemoveSite) {
    $siteName = "ftpauth"
    if (Get-Website -Name $siteName -ErrorAction SilentlyContinue) {
        Remove-Website -Name $siteName
        Write-Host "웹사이트를 제거했습니다: $siteName" -ForegroundColor Yellow
    }
}

# IIS 애플리케이션 풀 제거 (선택사항)
if ($RemoveAppPool) {
    $appPoolName = "ftpauth-pool"
    if (Get-IISAppPool -Name $appPoolName -ErrorAction SilentlyContinue) {
        Remove-WebAppPool -Name $appPoolName
        Write-Host "애플리케이션 풀을 제거했습니다: $appPoolName" -ForegroundColor Yellow
    }
}

# AuthProvider DLL 제거
$authProviderDlls = @(
    "IIS.Ftp.SimpleAuth.Provider.dll",
    "IIS.Ftp.SimpleAuth.Core.dll",
    "WelsonJS.Esent.dll",
    "Esent.Interop.dll"
)

foreach ($dll in $authProviderDlls) {
    $targetDll = Join-Path $IISSystemPath $dll
    $backupDll = Join-Path $BackupPath "$dll.backup"
    
    if (Test-Path $targetDll) {
        # 백업에서 복원 시도
        if (Test-Path $backupDll) {
            Copy-Item $backupDll -Destination $targetDll -Force
            Write-Host "$dll을 백업에서 복원했습니다." -ForegroundColor Yellow
        } else {
            # 백업이 없으면 제거
            Remove-Item $targetDll -Force
            Write-Host "$dll을 제거했습니다." -ForegroundColor Yellow
        }
    }
}

# 웹 애플리케이션 디렉토리 제거
if (Test-Path $IISPath) {
    Remove-Item $IISPath -Recurse -Force
    Write-Host "웹 애플리케이션 디렉토리를 제거했습니다: $IISPath" -ForegroundColor Yellow
}

# 사용자 데이터 디렉토리 제거 (선택사항)
$UserDataPath = "C:\inetpub\ftpusers"
if (Test-Path $UserDataPath) {
    $response = Read-Host "사용자 데이터 디렉토리도 제거하시겠습니까? (y/n)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        Remove-Item $UserDataPath -Recurse -Force
        Write-Host "사용자 데이터 디렉토리를 제거했습니다: $UserDataPath" -ForegroundColor Yellow
    } else {
        Write-Host "사용자 데이터 디렉토리는 유지됩니다: $UserDataPath" -ForegroundColor Cyan
    }
}

Write-Host "배포 철회가 완료되었습니다!" -ForegroundColor Green

# 복원 옵션 제공
if ($latestBackup) {
    $response = Read-Host "백업에서 복원하시겠습니까? (y/n)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        Write-Host "백업에서 복원을 시작합니다..." -ForegroundColor Cyan
        & "$PSScriptRoot\deploy-to-iis.ps1" -Force
    }
}
