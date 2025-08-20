# IIS FTP SimpleAuthProvider ���� öȸ ��ũ��Ʈ
param(
    [string]$IISPath = "C:\inetpub\wwwroot\ftpauth",
    [string]$BackupPath = "C:\inetpub\backup\ftpauth",
    [string]$IISSystemPath = "C:\Windows\System32\inetsrv",
    [switch]$RemoveSite,
    [switch]$RemoveAppPool,
    [switch]$Force
)

Write-Host "IIS FTP SimpleAuthProvider ���� öȸ�� �����մϴ�..." -ForegroundColor Yellow

# ������ ���� Ȯ��
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "�� ��ũ��Ʈ�� ������ �������� �����ؾ� �մϴ�."
    exit 1
}

# IIS ��� Ȯ��
try {
    Import-Module WebAdministration
} catch {
    Write-Error "IIS ���� ����� �ε��� �� �����ϴ�."
    exit 1
}

# ���� ���� Ȯ��
$deploymentInfoPath = Join-Path $IISPath "deployment-info.json"
if (Test-Path $deploymentInfoPath) {
    try {
        $deploymentInfo = Get-Content $deploymentInfoPath | ConvertFrom-Json
        Write-Host "���� ������ �߰��߽��ϴ�:" -ForegroundColor Cyan
        Write-Host "  ���� �ð�: $($deploymentInfo.DeployedAt)" -ForegroundColor Cyan
        Write-Host "  ����: $($deploymentInfo.Version)" -ForegroundColor Cyan
        Write-Host "  �ҽ� ���: $($deploymentInfo.SourcePath)" -ForegroundColor Cyan
    } catch {
        Write-Warning "���� ������ ���� �� �����ϴ�."
    }
}

# �ֽ� ��� ã��
$latestBackup = Get-ChildItem -Path $BackupPath -Directory | Sort-Object CreationTime -Descending | Select-Object -First 1
if ($latestBackup) {
    Write-Host "�ֽ� ����� �߰��߽��ϴ�: $($latestBackup.Name)" -ForegroundColor Cyan
    Write-Host "  ���� �ð�: $($latestBackup.CreationTime)" -ForegroundColor Cyan
} else {
    Write-Warning "����� ã�� �� �����ϴ�: $BackupPath"
}

# ����� Ȯ��
if (!$Force) {
    $response = Read-Host "������ ������ öȸ�Ͻðڽ��ϱ�? �� �۾��� �ǵ��� �� �����ϴ�. (yes/no)"
    if ($response -ne "yes") {
        Write-Host "���� öȸ�� ��ҵǾ����ϴ�." -ForegroundColor Green
        exit 0
    }
}

# IIS ����Ʈ ���� (���û���)
if ($RemoveSite) {
    $siteName = "ftpauth"
    if (Get-Website -Name $siteName -ErrorAction SilentlyContinue) {
        Remove-Website -Name $siteName
        Write-Host "������Ʈ�� �����߽��ϴ�: $siteName" -ForegroundColor Yellow
    }
}

# IIS ���ø����̼� Ǯ ���� (���û���)
if ($RemoveAppPool) {
    $appPoolName = "ftpauth-pool"
    if (Get-IISAppPool -Name $appPoolName -ErrorAction SilentlyContinue) {
        Remove-WebAppPool -Name $appPoolName
        Write-Host "���ø����̼� Ǯ�� �����߽��ϴ�: $appPoolName" -ForegroundColor Yellow
    }
}

# AuthProvider DLL ����
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
        # ������� ���� �õ�
        if (Test-Path $backupDll) {
            Copy-Item $backupDll -Destination $targetDll -Force
            Write-Host "$dll�� ������� �����߽��ϴ�." -ForegroundColor Yellow
        } else {
            # ����� ������ ����
            Remove-Item $targetDll -Force
            Write-Host "$dll�� �����߽��ϴ�." -ForegroundColor Yellow
        }
    }
}

# �� ���ø����̼� ���丮 ����
if (Test-Path $IISPath) {
    Remove-Item $IISPath -Recurse -Force
    Write-Host "�� ���ø����̼� ���丮�� �����߽��ϴ�: $IISPath" -ForegroundColor Yellow
}

# ����� ������ ���丮 ���� (���û���)
$UserDataPath = "C:\inetpub\ftpusers"
if (Test-Path $UserDataPath) {
    $response = Read-Host "����� ������ ���丮�� �����Ͻðڽ��ϱ�? (y/n)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        Remove-Item $UserDataPath -Recurse -Force
        Write-Host "����� ������ ���丮�� �����߽��ϴ�: $UserDataPath" -ForegroundColor Yellow
    } else {
        Write-Host "����� ������ ���丮�� �����˴ϴ�: $UserDataPath" -ForegroundColor Cyan
    }
}

Write-Host "���� öȸ�� �Ϸ�Ǿ����ϴ�!" -ForegroundColor Green

# ���� �ɼ� ����
if ($latestBackup) {
    $response = Read-Host "������� �����Ͻðڽ��ϱ�? (y/n)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        Write-Host "������� ������ �����մϴ�..." -ForegroundColor Cyan
        & "$PSScriptRoot\deploy-to-iis.ps1" -Force
    }
}
