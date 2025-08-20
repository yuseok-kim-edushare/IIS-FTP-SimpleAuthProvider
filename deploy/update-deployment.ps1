# IIS FTP SimpleAuthProvider ������Ʈ ��ũ��Ʈ
param(
    [string]$IISPath = "C:\inetpub\wwwroot\ftpauth",
    [string]$SourcePath = "src\ManagementWeb\bin\Release\net48",
    [string]$AuthProviderPath = "src\AuthProvider\bin\Release\net48",
    [string]$BackupPath = "C:\inetpub\backup\ftpauth",
    [switch]$RollbackOnError
)

Write-Host "IIS FTP SimpleAuthProvider ������Ʈ�� �����մϴ�..." -ForegroundColor Green

# ���� ���� ���� Ȯ��
if (!(Test-Path $IISPath)) {
    Write-Error "���� ������ �߰ߵ��� �ʾҽ��ϴ�. ���� �ʱ� ������ �����ϼ���."
    exit 1
}

# ���� ���� Ȯ��
$deploymentInfoPath = Join-Path $IISPath "deployment-info.json"
if (Test-Path $deploymentInfoPath) {
    try {
        $currentDeployment = Get-Content $deploymentInfoPath | ConvertFrom-Json
        Write-Host "���� ���� ����:" -ForegroundColor Cyan
        Write-Host "  ����: $($currentDeployment.Version)" -ForegroundColor Cyan
        Write-Host "  ���� �ð�: $($currentDeployment.DeployedAt)" -ForegroundColor Cyan
    } catch {
        Write-Warning "���� ���� ������ ���� �� �����ϴ�."
    }
}

# ��� ����
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
$backupPathWithTimestamp = "$BackupPath\$timestamp"
if (!(Test-Path $BackupPath)) {
    New-Item -ItemType Directory -Path $BackupPath -Force
}

Copy-Item $IISPath -Destination $backupPathWithTimestamp -Recurse -Force
Write-Host "���� ������ ����߽��ϴ�: $backupPathWithTimestamp" -ForegroundColor Yellow

try {
    # ���� ������Ʈ
    Write-Host "������ ������Ʈ�ϴ� ��..." -ForegroundColor Yellow
    Copy-Item "$SourcePath\*" -Destination $IISPath -Recurse -Force
    
    # AuthProvider DLL ������Ʈ
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
            # ���� DLL ���
            if (Test-Path $targetDll) {
                $backupDll = Join-Path $BackupPath "$dll.backup"
                Copy-Item $targetDll -Destination $backupDll -Force
            }
            
            Copy-Item $sourceDll -Destination $targetDll -Force
            Write-Host "$dll�� ������Ʈ�߽��ϴ�." -ForegroundColor Yellow
        }
    }
    
    # ���� ���� ������Ʈ
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
    
    Write-Host "������Ʈ�� �Ϸ�Ǿ����ϴ�!" -ForegroundColor Green
    
} catch {
    Write-Error "������Ʈ �� ������ �߻��߽��ϴ�: $($_.Exception.Message)"
    
    if ($RollbackOnError) {
        Write-Host "������� ������ �����մϴ�..." -ForegroundColor Yellow
        Remove-Item $IISPath -Recurse -Force
        Copy-Item $backupPathWithTimestamp -Destination $IISPath -Recurse -Force
        Write-Host "������� ������ �Ϸ�Ǿ����ϴ�." -ForegroundColor Green
    }
    
    exit 1
}
