# IIS FTP SimpleAuthProvider ���� ���� �� �ذ� ��ũ��Ʈ
# �� ��ũ��Ʈ�� FTP ���� ������ ü�������� �����ϰ� �ذ� ����� �����մϴ�.

param(
    [switch]$FixIssues,
    [switch]$Verbose
)

Write-Host "IIS FTP SimpleAuthProvider ���� ������ �����մϴ�..." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan

# ������ ���� Ȯ��
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "�� ��ũ��Ʈ�� ������ �������� �����ؾ� �մϴ�."
    exit 1
}

$issues = @()
$warnings = @()
$fixes = @()

# 1�ܰ�: �ý��� �䱸���� Ȯ��
Write-Host "`n=== 1�ܰ�: �ý��� �䱸���� Ȯ�� ===" -ForegroundColor Yellow

# .NET Framework ���� Ȯ��
$netFrameworkPath = "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"
if (Test-Path $netFrameworkPath) {
    $release = Get-ItemProperty $netFrameworkPath -Name Release
    if ($release.Release -ge 528040) {
        Write-Host "? .NET Framework 4.8 �̻��� ��ġ�Ǿ� �ֽ��ϴ�." -ForegroundColor Green
    } else {
        $issues += "? .NET Framework 4.8 �̻��� �ʿ��մϴ�. (����: $($release.Release))"
        $fixes += "dotnet --list-runtimes�� ��ġ�� ��Ÿ�� Ȯ�� �� 4.8 �̻� ��ġ"
    }
} else {
    $issues += "? .NET Framework 4.0 �̻��� ��ġ���� �ʾҽ��ϴ�."
    $fixes += "Microsoft .NET Framework 4.8 �ٿ�ε� �� ��ġ"
}

# IIS ��ġ Ȯ��
if (Get-Service -Name "W3SVC" -ErrorAction SilentlyContinue) {
    Write-Host "? IIS�� ��ġ�Ǿ� �ֽ��ϴ�." -ForegroundColor Green
} else {
    $issues += "? IIS�� ��ġ���� �ʾҽ��ϴ�."
    $fixes += "Windows ��ɿ��� IIS Web Server Ȱ��ȭ"
}

# FTP ���� Ȯ��
if (Get-Service -Name "FTPSVC" -ErrorAction SilentlyContinue) {
    Write-Host "? FTP ���񽺰� ��ġ�Ǿ� �ֽ��ϴ�." -ForegroundColor Green
} else {
    $issues += "? FTP ���񽺰� ��ġ���� �ʾҽ��ϴ�."
    $fixes += "Windows ��ɿ��� IIS-FTPServer, IIS-FTPSvc Ȱ��ȭ"
}

# 2�ܰ�: ���� ���� ���� Ȯ��
Write-Host "`n=== 2�ܰ�: ���� ���� ���� Ȯ�� ===" -ForegroundColor Yellow

$IISSystemPath = "C:\Windows\System32\inetsrv"
$requiredDlls = @(
    "IIS.Ftp.SimpleAuth.Provider.dll",
    "IIS.Ftp.SimpleAuth.Core.dll",
    "WelsonJS.Esent.dll",
    "Esent.Interop.dll"
)

foreach ($dll in $requiredDlls) {
    $dllPath = Join-Path $IISSystemPath $dll
    if (Test-Path $dllPath) {
        $fileInfo = Get-Item $dllPath
        Write-Host "? $dll ($($fileInfo.Length) bytes, $($fileInfo.LastWriteTime))" -ForegroundColor Green
        
        # DLL ���� Ȯ��
        try {
            $zone = Get-ItemProperty -Path $dllPath -Name Zone.Identifier -ErrorAction SilentlyContinue
            if ($zone) {
                $warnings += "?? $dll�� ���ͳݿ��� �ٿ�ε�Ǿ� ���ܵǾ��� �� �ֽ��ϴ�."
                if ($FixIssues) {
                    Unblock-File -Path $dllPath
                    Write-Host "? $dll ���� ���� �Ϸ�" -ForegroundColor Green
                }
            }
        } catch {
            # Zone.Identifier�� ������ ����
        }
    } else {
        $issues += "? $dll�� IIS �ý��� ���丮�� �����ϴ�."
        $fixes += "deploy\integrated-deploy.ps1 �����Ͽ� DLL �����"
    }
}

# 3�ܰ�: ���� ���� Ȯ��
Write-Host "`n=== 3�ܰ�: ���� ���� Ȯ�� ===" -ForegroundColor Yellow

$configPath = "C:\Windows\System32\inetsrv\ftpauth.config.json"
if (Test-Path $configPath) {
    Write-Host "? ���� ������ �����մϴ�: $configPath" -ForegroundColor Green
    
    try {
        $configContent = Get-Content $configPath -Raw | ConvertFrom-Json
        Write-Host "  ���� ���� ���� Ȯ�� �Ϸ�" -ForegroundColor Green
        
        # ���� ��ȿ�� �˻�
        if ($configContent.UserStore.Path -and $configContent.UserStore.Path -ne "C:\inetpub\ftpusers\users.json") {
            $warnings += "?? ����� ����� ��ΰ� �⺻���� �ٸ��ϴ�: $($configContent.UserStore.Path)"
        }
        
        if ($configContent.Logging.EventLogSource -ne "IIS-FTP-SimpleAuth") {
            $warnings += "?? �̺�Ʈ �α� �ҽ��� �⺻���� �ٸ��ϴ�: $($configContent.Logging.EventLogSource)"
        }
    } catch {
        $issues += "? ���� ���� JSON ������ �߸��Ǿ����ϴ�."
        $fixes += "���� ������ �ùٸ� JSON �������� ����"
    }
} else {
    $issues += "? ���� ������ �����ϴ�: $configPath"
    $fixes += "deploy\integrated-deploy.ps1 �����Ͽ� ���� ���� ����"
}

# 4�ܰ�: ����� ������ Ȯ��
Write-Host "`n=== 4�ܰ�: ����� ������ Ȯ�� ===" -ForegroundColor Yellow

$UserDataPath = "C:\inetpub\ftpusers"
if (Test-Path $UserDataPath) {
    Write-Host "? ����� ������ ���丮�� �����մϴ�: $UserDataPath" -ForegroundColor Green
    
    $usersFile = Join-Path $UserDataPath "users.json"
    if (Test-Path $usersFile) {
        $fileInfo = Get-Item $usersFile
        Write-Host "  ? users.json ($($fileInfo.Length) bytes)" -ForegroundColor Green
        
        # ���� ���� Ȯ��
        try {
            $usersContent = Get-Content $usersFile -Raw | ConvertFrom-Json
            if ($usersContent.Users -and $usersContent.Users.Count -gt 0) {
                Write-Host "  ? ����� �����Ͱ� ���ԵǾ� �ֽ��ϴ� ($($usersContent.Users.Count)��)" -ForegroundColor Green
            } else {
                $warnings += "?? users.json�� ����� �����Ͱ� �����ϴ�."
            }
        } catch {
            $issues += "? users.json ������ �߸��Ǿ����ϴ�."
            $fixes += "���� ����� ���Ϸ� �����"
        }
    } else {
        $issues += "? users.json ������ �����ϴ�."
        $fixes += "deploy\integrated-deploy.ps1 �����Ͽ� ���� ����� ���� ����"
    }
    
    # ���� Ȯ��
    try {
        $acl = Get-Acl $UserDataPath
        $iisUsersRule = $acl.Access | Where-Object { $_.IdentityReference -eq "IIS_IUSRS" }
        if ($iisUsersRule) {
            Write-Host "  ? IIS_IUSRS ����: $($iisUsersRule.FileSystemRights)" -ForegroundColor Green
        } else {
            $issues += "? IIS_IUSRS ������ �������� �ʾҽ��ϴ�."
            $fixes += "deploy\integrated-deploy.ps1 �����Ͽ� ���� ����"
        }
    } catch {
        $warnings += "?? ���� ������ Ȯ���� �� �����ϴ�."
    }
} else {
    $issues += "? ����� ������ ���丮�� �����ϴ�: $UserDataPath"
    $fixes += "deploy\integrated-deploy.ps1 �����Ͽ� ���丮 ����"
}

# 5�ܰ�: IIS ���� Ȯ��
Write-Host "`n=== 5�ܰ�: IIS ���� Ȯ�� ===" -ForegroundColor Yellow

try {
    Import-Module WebAdministration
    
    # Ŀ���� ���� Ȯ��
    $customAuth = Get-WebConfiguration "/system.ftpServer/security/authentication/customAuthentication"
    if ($customAuth.enabled -eq $true) {
        Write-Host "? Ŀ���� ������ Ȱ��ȭ�Ǿ� �ֽ��ϴ�." -ForegroundColor Green
        
        $providers = Get-WebConfiguration "/system.ftpServer/security/authentication/customAuthentication/providers"
        if ($providers.Collection.Count -gt 0) {
            Write-Host "  ? Ŀ���� ���� ���ι��̴��� ��ϵǾ� �ֽ��ϴ�." -ForegroundColor Green
            foreach ($provider in $providers.Collection) {
                Write-Host "    - $($provider.name): $($provider.type)" -ForegroundColor Cyan
            }
        } else {
            $issues += "? Ŀ���� ���� ���ι��̴��� ��ϵ��� �ʾҽ��ϴ�."
            $fixes += "deploy\integrated-deploy.ps1 �����Ͽ� ���ι��̴� ���"
        }
    } else {
        $issues += "? Ŀ���� ������ ��Ȱ��ȭ�Ǿ� �ֽ��ϴ�."
        $fixes += "deploy\integrated-deploy.ps1 �����Ͽ� Ŀ���� ���� Ȱ��ȭ"
    }
    
    # Ŀ���� ���� Ȯ��
    $customAuthz = Get-WebConfiguration "/system.ftpServer/security/authorization/customAuthorization"
    if ($customAuthz.enabled -eq $true) {
        Write-Host "? Ŀ���� ������ Ȱ��ȭ�Ǿ� �ֽ��ϴ�." -ForegroundColor Green
        
        $providers = Get-WebConfiguration "/system.ftpServer/security/authorization/customAuthorization/providers"
        if ($providers.Collection.Count -gt 0) {
            Write-Host "  ? Ŀ���� ���� ���ι��̴��� ��ϵǾ� �ֽ��ϴ�." -ForegroundColor Green
            foreach ($provider in $providers.Collection) {
                Write-Host "    - $($provider.name): $($provider.type)" -ForegroundColor Cyan
            }
        } else {
            $issues += "? Ŀ���� ���� ���ι��̴��� ��ϵ��� �ʾҽ��ϴ�."
            $fixes += "deploy\integrated-deploy.ps1 �����Ͽ� ���ι��̴� ���"
        }
    } else {
        $issues += "? Ŀ���� ������ ��Ȱ��ȭ�Ǿ� �ֽ��ϴ�."
        $fixes += "deploy\integrated-deploy.ps1 �����Ͽ� Ŀ���� ���� Ȱ��ȭ"
    }
    
} catch {
    $issues += "? IIS ������ Ȯ���� �� �����ϴ�: $($_.Exception.Message)"
    $fixes += "IIS ���� ��� ��ġ �� ���� Ȯ��"
}

# 6�ܰ�: �̺�Ʈ �α� Ȯ��
Write-Host "`n=== 6�ܰ�: �̺�Ʈ �α� Ȯ�� ===" -ForegroundColor Yellow

try {
    $eventLogs = Get-EventLog -LogName Application -Source "IIS-FTP-SimpleAuth" -Newest 5 -ErrorAction SilentlyContinue
    if ($eventLogs) {
        Write-Host "? �̺�Ʈ �α� �ҽ��� �����մϴ�." -ForegroundColor Green
        Write-Host "  �ֱ� �̺�Ʈ ($($eventLogs.Count)��):" -ForegroundColor Cyan
        foreach ($log in $eventLogs) {
            $color = if ($log.EntryType -eq "Error") { "Red" } elseif ($log.EntryType -eq "Warning") { "Yellow" } else { "Green" }
            Write-Host "    $($log.TimeGenerated): $($log.EntryType) - $($log.Message)" -ForegroundColor $color
        }
    } else {
        $warnings += "?? �̺�Ʈ �α� �ҽ��� �׸��� �����ϴ�."
        
        # �̺�Ʈ �α� �ҽ� ���� ���� Ȯ��
        $sources = Get-EventLog -LogName Application -Newest 1 | Select-Object -ExpandProperty Source -Unique
        if ($sources -contains "IIS-FTP-SimpleAuth") {
            Write-Host "  ? �̺�Ʈ �α� �ҽ��� ���������� �׸��� �����ϴ�." -ForegroundColor Green
        } else {
            $issues += "? �̺�Ʈ �α� �ҽ� 'IIS-FTP-SimpleAuth'�� �������� �ʾҽ��ϴ�."
            $fixes += "CLI ������ �� �� �����Ͽ� �̺�Ʈ �α� �ҽ� ����"
        }
    }
} catch {
    $issues += "? �̺�Ʈ �α׸� Ȯ���� �� �����ϴ�: $($_.Exception.Message)"
    $fixes += "�̺�Ʈ �α� ���� Ȯ�� �� CLI ���� ����"
}

# 7�ܰ�: ���� ���� Ȯ��
Write-Host "`n=== 7�ܰ�: ���� ���� Ȯ�� ===" -ForegroundColor Yellow

$services = @("W3SVC", "FTPSVC")
foreach ($service in $services) {
    $svc = Get-Service -Name $service -ErrorAction SilentlyContinue
    if ($svc) {
        if ($svc.Status -eq "Running") {
            Write-Host "? $service ���񽺰� ���� ���Դϴ�." -ForegroundColor Green
        } else {
            $issues += "? $service ���񽺰� ������� �ʰ� �ֽ��ϴ�. (����: $($svc.Status))"
            $fixes += "Start-Service $service ������� ���� ����"
        }
    } else {
        $issues += "? $service ���񽺰� ��ġ���� �ʾҽ��ϴ�."
        $fixes += "Windows ��ɿ��� �ش� ���� Ȱ��ȭ"
    }
}

# 8�ܰ�: ���� ��� �� �ذ� ���
Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "? ���� ��� ���" -ForegroundColor Yellow
Write-Host "==================================================" -ForegroundColor Cyan

if ($issues.Count -eq 0) {
    Write-Host "? �ɰ��� ������ �߰ߵ��� �ʾҽ��ϴ�!" -ForegroundColor Green
} else {
    Write-Host "? �߰ߵ� ���� ($($issues.Count)��):" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host "  $issue" -ForegroundColor Red
    }
}

if ($warnings.Count -gt 0) {
    Write-Host "`n?? ���ǻ��� ($($warnings.Count)��):" -ForegroundColor Yellow
    foreach ($warning in $warnings) {
        Write-Host "  $warning" -ForegroundColor Yellow
    }
}

if ($issues.Count -gt 0) {
    Write-Host "`n? �ذ� ���:" -ForegroundColor Cyan
    foreach ($fix in $fixes) {
        Write-Host "  ? $fix" -ForegroundColor White
    }
    
    if ($FixIssues) {
        Write-Host "`n? �ڵ� ������ �����մϴ�..." -ForegroundColor Yellow
        
        # ���� ���� ��ũ��Ʈ ����
        $integratedScript = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "integrated-deploy.ps1"
        if (Test-Path $integratedScript) {
            Write-Host "���� ���� ��ũ��Ʈ�� �����մϴ�..." -ForegroundColor Yellow
            & $integratedScript -CreateAppPool -CreateSite -Force
        } else {
            Write-Error "���� ���� ��ũ��Ʈ�� ã�� �� �����ϴ�: $integratedScript"
        }
    } else {
        Write-Host "`n? �ڵ� ������ ���Ͻø� -FixIssues �Ű������� ����ϼ���:" -ForegroundColor Cyan
        Write-Host "  .\deploy\diagnose-ftp-issues.ps1 -FixIssues" -ForegroundColor White
    }
}

Write-Host "`n? �߰� ���� ���:" -ForegroundColor Yellow
Write-Host "  - ���� ���� Ȯ��: .\deploy\check-deployment-status.ps1" -ForegroundColor White
Write-Host "  - �̺�Ʈ �α� �� Ȯ��: Get-EventLog -LogName Application -Source 'IIS-FTP-SimpleAuth' -Newest 20" -ForegroundColor White
Write-Host "  - IIS ���� Ȯ��: Get-WebConfiguration '/system.ftpServer/security/authentication/customAuthentication'" -ForegroundColor White
Write-Host "  - ���� ���� Ȯ��: Get-Service W3SVC, MSFTPSVC" -ForegroundColor White

Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "���� �Ϸ�" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
