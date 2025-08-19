# IIS FTP SimpleAuthProvider 업데이트 스크립트
param(
    [string]$IISPath = "C:\inetpub\wwwroot\ftpauth",
    [string]$SourcePath = "src\ManagementWeb\bin\Release\net48",
    [string]$AuthProviderPath = "src\AuthProvider\bin\Release\net48",
    [string]$BackupPath = "C:\inetpub\backup\ftpauth",
    [switch]$RollbackOnError
)

Write-Host "IIS FTP SimpleAuthProvider 업데이트를 시작합니다..." -ForegroundColor Green

# 현재 배포 상태 확인
if (!(Test-Path $IISPath)) {
    Write-Error "현재 배포가 발견되지 않았습니다. 먼저 초기 배포를 수행하세요."
    exit 1
}

# 배포 정보 확인
$deploymentInfoPath = Join-Path $IISPath "deployment-info.json"
if (Test-Path $deploymentInfoPath) {
    try {
        $currentDeployment = Get-Content $deploymentInfoPath | ConvertFrom-Json
        Write-Host "현재 배포 정보:" -ForegroundColor Cyan
        Write-Host "  버전: $($currentDeployment.Version)" -ForegroundColor Cyan
        Write-Host "  배포 시간: $($currentDeployment.DeployedAt)" -ForegroundColor Cyan
    } catch {
        Write-Warning "현재 배포 정보를 읽을 수 없습니다."
    }
}

# 백업 생성
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupPathWithTimestamp = "$BackupPath\$timestamp"
if (!(Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath -Force
}

Copy-Item $IISPath -Destination $backupPathWithTimestamp -Recurse -Force
Write-Host "현재 배포를 백업했습니다: $backupPathWithTimestamp" -ForegroundColor Yellow

try {
    # 파일 업데이트
    Write-Host "파일을 업데이트하는 중..." -ForegroundColor Yellow
    Copy-Item "$SourcePath\*" -Destination $IISPath -Recurse -Force
    
    # AuthProvider DLL 업데이트
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
            # 기존 DLL 백업
            if (Test-Path $targetDll) {
                $backupDll = Join-Path $BackupPath "$dll.backup"
                Copy-Item $targetDll -Destination $backupDll -Force
            }
            
            Copy-Item $sourceDll -Destination $targetDll -Force
            Write-Host "$dll을 업데이트했습니다." -ForegroundColor Yellow
        }
    }
    
    # 배포 정보 업데이트
    $newDeploymentInfo = @{
        DeployedAt = Get-Date
        Version = "1.0.1"
        SourcePath = $SourcePath
        AuthProviderPath = $AuthProviderPath
        BackupPath = $BackupPath
        PreviousBackup = $backupPathWithTimestamp
    } | ConvertTo-Json
    
    $deploymentInfoPath = Join-Path $IISPath "deployment-info.json"
    $newDeploymentInfo | Out-File -FilePath $deploymentInfoPath -Encoding UTF8
    
    Write-Host "업데이트가 완료되었습니다!" -ForegroundColor Green
    
} catch {
    Write-Error "업데이트 중 오류가 발생했습니다: $($_.Exception.Message)"
    
    if ($RollbackOnError) {
        Write-Host "백업에서 복원을 시작합니다..." -ForegroundColor Yellow
        Remove-Item $IISPath -Recurse -Force
        Copy-Item $backupPathWithTimestamp -Destination $IISPath -Recurse -Force
        Write-Host "백업에서 복원이 완료되었습니다." -ForegroundColor Green
    }
    
    exit 1
}
