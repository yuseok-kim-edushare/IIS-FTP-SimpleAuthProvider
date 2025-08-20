# IIS FTP SimpleAuthProvider ���� ���� Ȯ�� ��ũ��Ʈ
param(
    [string]$IISPath = "C:\inetpub\wwwroot\ftpauth",
    [string]$BackupPath = "C:\inetpub\backup\ftpauth",
    [string]$IISSystemPath = "C:\Windows\System32\inetsrv",
    [switch]$Detailed
)

Write-Host "IIS FTP SimpleAuthProvider ���� ���¸� Ȯ���մϴ�..." -ForegroundColor Cyan

# ������ ���� Ȯ��
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Warning "������ ������ �����ϴ�. �Ϻ� ������ Ȯ���� �� ���� �� �ֽ��ϴ�."
}

# IIS ��� Ȯ��
try {
    Import-Module WebAdministration
    $iisAvailable = $true
} catch {
    $iisAvailable = $false
    Write-Warning "IIS ���� ����� �ε��� �� �����ϴ�."
}

# �� ���ø����̼� ���� Ȯ��
Write-Host "`n=== �� ���ø����̼� ���� ===" -ForegroundColor Green
if (Test-Path $IISPath) {
    Write-Host "? �� ���ø����̼� ���丮�� �����մϴ�: $IISPath" -ForegroundColor Green
    
    # ���� ���� Ȯ��
    $deploymentInfoPath = Join-Path $IISPath "deployment-info.json"
    if (Test-Path $deploymentInfoPath) {
        try {
            $deploymentInfo = Get-Content $deploymentInfoPath | ConvertFrom-Json
            Write-Host "  ���� �ð�: $($deploymentInfo.DeployedAt)" -ForegroundColor Cyan
            Write-Host "  ����: $($deploymentInfo.Version)" -ForegroundColor Cyan
            Write-Host "  �ҽ� ���: $($deploymentInfo.SourcePath)" -ForegroundColor Cyan
        } catch {
            Write-Warning "���� ������ ���� �� �����ϴ�."
        }
    } else {
        Write-Warning "���� ���� ������ �����ϴ�."
    }
    
    # �ֿ� ���� Ȯ��
    $requiredFiles = @(
        "ManagementWeb.dll",
        "IIS.Ftp.SimpleAuth.Core.dll",
        "Web.config"
    )
    
    Write-Host "`n  �ֿ� ���� ����:" -ForegroundColor Yellow
    foreach ($file in $requiredFiles) {
        $filePath = Join-Path $IISPath $file
        if (Test-Path $filePath) {
            $fileInfo = Get-Item $filePath
            Write-Host "    ? $file ($($fileInfo.Length) bytes, $($fileInfo.LastWriteTime))" -ForegroundColor Green
        } else {
            Write-Host "    ? $file (����)" -ForegroundColor Red
        }
    }
    
    if ($Detailed) {
        # ��ü ���� ���
        Write-Host "`n  ��ü ���� ���:" -ForegroundColor Yellow
        Get-ChildItem $IISPath -Recurse | ForEach-Object {
            $relativePath = $_.FullName.Replace($IISPath, "").TrimStart("\")
            if ($_.PSIsContainer) {
                Write-Host "    ? $relativePath" -ForegroundColor Blue
            } else {
                Write-Host "    ? $relativePath ($($_.Length) bytes)" -ForegroundColor Gray
            }
        }
    }
} else {
    Write-Host "? �� ���ø����̼� ���丮�� �������� �ʽ��ϴ�: $IISPath" -ForegroundColor Red
}

# AuthProvider DLL ���� Ȯ��
Write-Host "`n=== AuthProvider DLL ���� ===" -ForegroundColor Green
$authProviderDlls = @(
    "IIS.Ftp.SimpleAuth.Provider.dll",
    "IIS.Ftp.SimpleAuth.Core.dll",
    "WelsonJS.Esent.dll",
    "Esent.Interop.dll"
)

foreach ($dll in $authProviderDlls) {
    $targetDll = Join-Path $IISSystemPath $dll
    if (Test-Path $targetDll) {
        $fileInfo = Get-Item $targetDll
        Write-Host "  ? $dll ($($fileInfo.Length) bytes, $($fileInfo.LastWriteTime))" -ForegroundColor Green
    } else {
        Write-Host "  ? $dll (����)" -ForegroundColor Red
    }
}

# IIS ����Ʈ �� ���ø����̼� Ǯ ���� Ȯ��
if ($iisAvailable) {
    Write-Host "`n=== IIS ���� ���� ===" -ForegroundColor Green
    
    # ����Ʈ ����
    $siteName = "ftpauth"
    $site = Get-Website -Name $siteName -ErrorAction SilentlyContinue
    if ($site) {
        Write-Host "? ������Ʈ�� �����մϴ�: $siteName" -ForegroundColor Green
        Write-Host "  ����: $($site.State)" -ForegroundColor Cyan
        Write-Host "  ��Ʈ: $($site.Bindings.Collection[0].BindingInformation)" -ForegroundColor Cyan
        Write-Host "  ������ ���: $($site.PhysicalPath)" -ForegroundColor Cyan
    } else {
        Write-Host "? ������Ʈ�� �������� �ʽ��ϴ�: $siteName" -ForegroundColor Red
    }
    
    # ���ø����̼� Ǯ ����
    $appPoolName = "ftpauth-pool"
    $appPool = Get-IISAppPool -Name $appPoolName -ErrorAction SilentlyContinue
    if ($appPool) {
        Write-Host "? ���ø����̼� Ǯ�� �����մϴ�: $appPoolName" -ForegroundColor Green
        Write-Host "  ����: $($appPool.State)" -ForegroundColor Cyan
        Write-Host "  .NET ����: $($appPool.ManagedRuntimeVersion)" -ForegroundColor Cyan
    } else {
        Write-Host "? ���ø����̼� Ǯ�� �������� �ʽ��ϴ�: $appPoolName" -ForegroundColor Red
    }
}

# ����� ������ ���丮 ���� Ȯ��
Write-Host "`n=== ����� ������ ���� ===" -ForegroundColor Green
$UserDataPath = "C:\inetpub\ftpusers"
if (Test-Path $UserDataPath) {
    Write-Host "? ����� ������ ���丮�� �����մϴ�: $UserDataPath" -ForegroundColor Green
    
    # ����� ���� Ȯ��
    $usersFile = Join-Path $UserDataPath "users.json"
    if (Test-Path $usersFile) {
        $fileInfo = Get-Item $usersFile
        Write-Host "  ? users.json ($($fileInfo.Length) bytes, $($fileInfo.LastWriteTime))" -ForegroundColor Green
    } else {
        Write-Host "  ? users.json (����)" -ForegroundColor Red
    }
    
    # ���丮 ���� Ȯ��
    try {
        $acl = Get-Acl $UserDataPath
        $iisUsersRule = $acl.Access | Where-Object { $_.IdentityReference -eq "IIS_IUSRS" }
        if ($iisUsersRule) {
            Write-Host "  ? IIS_IUSRS ����: $($iisUsersRule.FileSystemRights)" -ForegroundColor Green
        } else {
            Write-Host "  ? IIS_IUSRS ������ �������� �ʾҽ��ϴ�." -ForegroundColor Yellow
        }
    } catch {
        Write-Warning "���� ������ Ȯ���� �� �����ϴ�."
    }
} else {
    Write-Host "? ����� ������ ���丮�� �������� �ʽ��ϴ�: $UserDataPath" -ForegroundColor Red
}

# ��� ���� Ȯ��
Write-Host "`n=== ��� ���� ===" -ForegroundColor Green
if (Test-Path $BackupPath) {
    $backups = Get-ChildItem -Path $BackupPath -Directory | Sort-Object CreationTime -Descending
    if ($backups.Count -gt 0) {
        Write-Host "? ����� �����մϴ� ($($backups.Count)��):" -ForegroundColor Green
        foreach ($backup in $backups | Select-Object -First 5) {
            $size = (Get-ChildItem $backup.FullName -Recurse | Measure-Object -Property Length -Sum).Sum
            $sizeMB = [math]::Round($size / 1MB, 2)
            Write-Host "  ? $($backup.Name) ($sizeMB MB, $($backup.CreationTime))" -ForegroundColor Cyan
        }
        
        if ($backups.Count -gt 5) {
            Write-Host "  ... �� $($backups.Count - 5)���� �߰� ���" -ForegroundColor Gray
        }
    } else {
        Write-Host "? ��� ���丮�� ����ֽ��ϴ�." -ForegroundColor Yellow
    }
} else {
    Write-Host "? ��� ���丮�� �������� �ʽ��ϴ�: $BackupPath" -ForegroundColor Red
}

# �ý��� �䱸���� Ȯ��
Write-Host "`n=== �ý��� �䱸���� ===" -ForegroundColor Green

# .NET Framework ���� Ȯ��
$netFrameworkPath = "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"
if (Test-Path $netFrameworkPath) {
    $release = Get-ItemProperty $netFrameworkPath -Name Release
    if ($release.Release -ge 528040) {
        Write-Host "? .NET Framework 4.8 �̻��� ��ġ�Ǿ� �ֽ��ϴ�." -ForegroundColor Green
    } else {
        Write-Host "? .NET Framework 4.8 �̻��� �ʿ��մϴ�. (����: $($release.Release))" -ForegroundColor Yellow
    }
} else {
    Write-Host "? .NET Framework 4.0 �̻��� ��ġ���� �ʾҽ��ϴ�." -ForegroundColor Red
}

# IIS ��ġ Ȯ��
if (Get-Service -Name "W3SVC" -ErrorAction SilentlyContinue) {
    Write-Host "? IIS�� ��ġ�Ǿ� �ֽ��ϴ�." -ForegroundColor Green
} else {
    Write-Host "? IIS�� ��ġ���� �ʾҽ��ϴ�." -ForegroundColor Red
}

Write-Host "`n=== ���� Ȯ�� �Ϸ� ===" -ForegroundColor Green
