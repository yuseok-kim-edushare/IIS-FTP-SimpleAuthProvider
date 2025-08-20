# IIS FTP SimpleAuthProvider ���� ��ũ��Ʈ
param(
    [string]$IISPath = "C:\inetpub\wwwroot\ftpauth",
    [string]$SourcePath = "src\ManagementWeb\bin\Release\net48",
    [string]$AuthProviderPath = "src\AuthProvider\bin\Release\net48",
    [string]$BackupPath = "C:\inetpub\backup\ftpauth",
    [switch]$CreateAppPool,
    [switch]$CreateSite,
    [switch]$Force
)

Write-Host "IIS FTP SimpleAuthProvider ������ �����մϴ�..." -ForegroundColor Green

# ������ ���� Ȯ��
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "�� ��ũ��Ʈ�� ������ �������� �����ؾ� �մϴ�."
    exit 1
}

# IIS ��� Ȯ��
try {
    Import-Module WebAdministration
} catch {
    Write-Error "IIS ���� ����� �ε��� �� �����ϴ�. IIS�� ��ġ�Ǿ� �ִ��� Ȯ���ϼ���."
    exit 1
}

# ���� ���� ���
if (Test-Path $IISPath) {
    if (!$Force) {
        $response = Read-Host "���� ������ �߰ߵǾ����ϴ�. ����Ͻðڽ��ϱ�? (y/n)"
        if ($response -eq 'y' -or $response -eq 'Y') {
            $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
            $backupPathWithTimestamp = "$BackupPath\$timestamp"
            
            if (!(Test-Path $BackupPath)) {
                New-Item -ItemType Directory -Path $BackupPath -Force
            }
            
            Copy-Item $IISPath -Destination $backupPathWithTimestamp -Recurse -Force
            Write-Host "���� ������ ����߽��ϴ�: $backupPathWithTimestamp" -ForegroundColor Yellow
        }
    }
}

# ��� ���丮 ����/����
if (Test-Path $IISPath) {
    if ($Force) {
        Remove-Item $IISPath -Recurse -Force
        Write-Host "���� ������ �����߽��ϴ�." -ForegroundColor Yellow
    }
}
New-Item -ItemType Directory -Path $IISPath -Force | Out-Null

# ���� ����
Write-Host "������ �����ϴ� ��..." -ForegroundColor Yellow
Copy-Item "$SourcePath\*" -Destination $IISPath -Recurse -Force

# AuthProvider DLL ���� (IIS �ý��� ���丮)
$IISSystemPath = "C:\Windows\System32\inetsrv"
if (Test-Path $IISSystemPath) {
    # ���� DLL ���
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
            Write-Host "$dll�� IIS �ý��� ���丮�� �����߽��ϴ�." -ForegroundColor Yellow
        }
    }
}

# ����� ������ ���丮 ����
$UserDataPath = "C:\inetpub\ftpusers"
if (!(Test-Path $UserDataPath)) {
    New-Item -ItemType Directory -Path $UserDataPath -Force
    Write-Host "����� ������ ���丮�� �����߽��ϴ�: $UserDataPath" -ForegroundColor Yellow
}

# ���� ����� ���� ����
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
    Write-Host "���� ����� ������ �����߽��ϴ�: $SampleUsersPath" -ForegroundColor Yellow
}

# IIS ���ø����̼� Ǯ ���� (���û���)
if ($CreateAppPool) {
    $appPoolName = "ftpauth-pool"
    if (!(Get-IISAppPool -Name $appPoolName -ErrorAction SilentlyContinue)) {
        New-WebAppPool -Name $appPoolName
        Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value "v4.0"
        Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
        Write-Host "���ø����̼� Ǯ�� �����߽��ϴ�: $appPoolName" -ForegroundColor Yellow
    }
}

# IIS ����Ʈ ���� (���û���)
if ($CreateSite) {
    $siteName = "ftpauth"
    if (!(Get-Website -Name $siteName -ErrorAction SilentlyContinue)) {
        New-Website -Name $siteName -PhysicalPath $IISPath -Port 8080
        Write-Host "������Ʈ�� �����߽��ϴ�: $siteName (��Ʈ 8080)" -ForegroundColor Yellow
    }
}

# ���� ����
$acl = Get-Acl $UserDataPath
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl -Path $UserDataPath -AclObject $acl

# ���� ���� ���
$deploymentInfo = @{
    DeployedAt = Get-Date
    Version = "1.0.0"
    SourcePath = $SourcePath
    AuthProviderPath = $AuthProviderPath
    BackupPath = $BackupPath
} | ConvertTo-Json

$deploymentInfoPath = Join-Path $IISPath "deployment-info.json"
$deploymentInfo | Out-File -FilePath $deploymentInfoPath -Encoding UTF8

Write-Host "������ �Ϸ�Ǿ����ϴ�!" -ForegroundColor Green
Write-Host "�� ���� �ܼ�: http://localhost:8080" -ForegroundColor Cyan
Write-Host "�⺻ ������ ����: admin / admin123" -ForegroundColor Cyan
Write-Host "����� ������ ���: $UserDataPath" -ForegroundColor Cyan
Write-Host "���� ����: $deploymentInfoPath" -ForegroundColor Cyan