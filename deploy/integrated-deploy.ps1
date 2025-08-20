# IIS FTP SimpleAuthProvider ���� ���� �� ���� ��ũ��Ʈ
# �� ��ũ��Ʈ�� ��ü �ý����� �� ���� �����ϰ� �����մϴ�.

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

Write-Host "IIS FTP SimpleAuthProvider ���� ������ �����մϴ�..." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan

# ������ ���� Ȯ��
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "�� ��ũ��Ʈ�� ������ �������� �����ؾ� �մϴ�."
    exit 1
}

# IIS ��� Ȯ��
try {
    Import-Module WebAdministration
    Write-Host "? IIS ���� ��� �ε� ����" -ForegroundColor Green
} catch {
    Write-Error "IIS ���� ����� �ε��� �� �����ϴ�. IIS�� ��ġ�Ǿ� �ִ��� Ȯ���ϼ���."
    exit 1
}

# 1�ܰ�: ������Ʈ ����
if (-not $SkipBuild) {
    Write-Host "`n=== 1�ܰ�: ������Ʈ ���� ===" -ForegroundColor Yellow
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $projectRoot = Split-Path -Parent $scriptDir
    
    Set-Location $projectRoot
    Write-Host "������Ʈ ��Ʈ: $projectRoot" -ForegroundColor Cyan
    
    try {
        Write-Host "������Ʈ�� �����ϴ� ��..." -ForegroundColor Yellow
        dotnet build --configuration Release
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
    Metrics = @{
        Enabled = $true
        MetricsFilePath = "C:\inetpub\ftpmetrics\ftp_metrics.prom"
        ExportIntervalSeconds = 60
    }
}

$config | ConvertTo-Json -Depth 10 | Out-File -FilePath $configPath -Encoding UTF8
Write-Host "? ���� ������ �����߽��ϴ�: $configPath" -ForegroundColor Green

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
    Write-Host "? ���� ����� ������ �����߽��ϴ�: $SampleUsersPath" -ForegroundColor Green
}

# 6�ܰ�: IIS ���ø����̼� Ǯ �� ����Ʈ ����
Write-Host "`n=== 6�ܰ�: IIS ���� ===" -ForegroundColor Yellow
if ($CreateAppPool) {
    $appPoolName = "ftpauth-pool"
    if (!(Get-IISAppPool -Name $appPoolName -ErrorAction SilentlyContinue)) {
        New-WebAppPool -Name $appPoolName
        Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "managedRuntimeVersion" -Value "v4.0"
        Set-ItemProperty -Path "IIS:\AppPools\$appPoolName" -Name "processModel.identityType" -Value "ApplicationPoolIdentity"
        Write-Host "? ���ø����̼� Ǯ�� �����߽��ϴ�: $appPoolName" -ForegroundColor Green
    }
}

if ($CreateSite) {
    $siteName = "ftpauth"
    if (!(Get-Website -Name $siteName -ErrorAction SilentlyContinue)) {
        New-Website -Name $siteName -PhysicalPath $IISPath -Port 8080
        Write-Host "? ������Ʈ�� �����߽��ϴ�: $siteName (��Ʈ 8080)" -ForegroundColor Green
    }
}

# 7�ܰ�: IIS FTP Ŀ���� ���ι��̴� ���
Write-Host "`n=== 7�ܰ�: IIS FTP Ŀ���� ���ι��̴� ��� ===" -ForegroundColor Yellow
try {
    # Ŀ���� ���� Ȱ��ȭ
    Set-WebConfigurationProperty -Filter "system.ftpServer/security/authentication/customAuthentication" -Name "enabled" -Value $true
    
    # Ŀ���� ���� ���ι��̴� �߰�
    Add-WebConfigurationProperty -Filter "system.ftpServer/security/authentication/customAuthentication/providers" -Name "." -Value @{
        name = "SimpleAuth"
        enabled = $true
        type = "IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthenticationProvider"
    }
    
    # Ŀ���� ���� Ȱ��ȭ
    Set-WebConfigurationProperty -Filter "system.ftpServer/security/authorization/customAuthorization" -Name "enabled" -Value $true
    
    # Ŀ���� ���� ���ι��̴� �߰�
    Add-WebConfigurationProperty -Filter "system.ftpServer/security/authorization/customAuthorization" -Name "." -Value @{
        name = "SimpleAuth"
        enabled = $true
        type = "IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthorizationProvider"
    }
    
    Write-Host "? IIS FTP Ŀ���� ���ι��̴� ��� �Ϸ�" -ForegroundColor Green
} catch {
    Write-Warning "IIS FTP Ŀ���� ���ι��̴� ��� �� ������ �߻��߽��ϴ�: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host "�������� ����ؾ� �� �� �ֽ��ϴ�." -ForegroundColor Yellow
}

# 8�ܰ�: ���� ����
Write-Host "`n=== 8�ܰ�: ���� ���� ===" -ForegroundColor Yellow
try {
    $acl = Get-Acl $UserDataPath
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule)
    Set-Acl -Path $UserDataPath -AclObject $acl
    Write-Host "? IIS_IUSRS ���� ���� �Ϸ�" -ForegroundColor Green
} catch {
    Write-Warning "���� ���� �� ������ �߻��߽��ϴ�: $($_.Exception.Message)" -ForegroundColor Yellow
}

# 9�ܰ�: ���� ���� ���
Write-Host "`n=== 9�ܰ�: ���� ���� ��� ===" -ForegroundColor Yellow
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
Write-Host "? ���� ������ ����߽��ϴ�." -ForegroundColor Green

# 10�ܰ�: ���� �����
Write-Host "`n=== 10�ܰ�: ���� ����� ===" -ForegroundColor Yellow
try {
    Write-Host "IIS�� ������ϴ� ��..." -ForegroundColor Yellow
    iisreset /restart
    Write-Host "? IIS ����� �Ϸ�" -ForegroundColor Green
    
    Write-Host "FTP ���񽺸� ������ϴ� ��..." -ForegroundColor Yellow
    Restart-Service FTPSVC -Force
    Write-Host "? FTP ���� ����� �Ϸ�" -ForegroundColor Green
} catch {
    Write-Warning "���� ����� �� ������ �߻��߽��ϴ�: $($_.Exception.Message)" -ForegroundColor Yellow
}

Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "? IIS FTP SimpleAuthProvider ���� ������ �Ϸ�Ǿ����ϴ�!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan

Write-Host "`n? ���� ���:" -ForegroundColor Yellow
Write-Host "  �� ���� �ܼ�: http://localhost:8080" -ForegroundColor Cyan
Write-Host "  �⺻ ������ ����: admin / admin123" -ForegroundColor Cyan
Write-Host "  ����� ������ ���: $UserDataPath" -ForegroundColor Cyan
Write-Host "  ���� ����: $configPath" -ForegroundColor Cyan
Write-Host "  ���� ����: $deploymentInfoPath" -ForegroundColor Cyan

Write-Host "`n? ���� �ܰ�:" -ForegroundColor Yellow
Write-Host "  1. �� ���� �ֿܼ� �����Ͽ� �ý��� ���� Ȯ��" -ForegroundColor White
Write-Host "  2. FTP Ŭ���̾�Ʈ�� ���� �׽�Ʈ" -ForegroundColor White
Write-Host "  3. �̺�Ʈ �α׿��� ���� �޽��� Ȯ��" -ForegroundColor White
Write-Host "  4. �ʿ�� ��ȣȭ Ű ȯ�� ���� ����" -ForegroundColor White

Write-Host "`n? ���� �ذ�:" -ForegroundColor Yellow
Write-Host "  - �̺�Ʈ �α� Ȯ��: Get-EventLog -LogName Application -Source 'IIS-FTP-SimpleAuth'" -ForegroundColor White
Write-Host "  - ���� ���� Ȯ��: .\deploy\check-deployment-status.ps1" -ForegroundColor White
Write-Host "  - ���� ����: IIS Manager���� FTP ����Ʈ ���� Ȯ��" -ForegroundColor White
