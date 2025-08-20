# IIS FTP SimpleAuthProvider 문제 진단 및 해결 스크립트
# 이 스크립트는 FTP 인증 문제를 체계적으로 진단하고 해결 방법을 제시합니다.

param(
    [switch]$FixIssues,
    [switch]$Verbose
)

Write-Host "IIS FTP SimpleAuthProvider 문제 진단을 시작합니다..." -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan

# 관리자 권한 확인
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "이 스크립트는 관리자 권한으로 실행해야 합니다."
    exit 1
}

$issues = @()
$warnings = @()
$fixes = @()

# 1단계: 시스템 요구사항 확인
Write-Host "`n=== 1단계: 시스템 요구사항 확인 ===" -ForegroundColor Yellow

# .NET Framework 버전 확인
$netFrameworkPath = "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"
if (Test-Path $netFrameworkPath) {
    $release = Get-ItemProperty $netFrameworkPath -Name Release
    if ($release.Release -ge 528040) {
        Write-Host "? .NET Framework 4.8 이상이 설치되어 있습니다." -ForegroundColor Green
    } else {
        $issues += "? .NET Framework 4.8 이상이 필요합니다. (현재: $($release.Release))"
        $fixes += "dotnet --list-runtimes로 설치된 런타임 확인 후 4.8 이상 설치"
    }
} else {
    $issues += "? .NET Framework 4.0 이상이 설치되지 않았습니다."
    $fixes += "Microsoft .NET Framework 4.8 다운로드 및 설치"
}

# IIS 설치 확인
if (Get-Service -Name "W3SVC" -ErrorAction SilentlyContinue) {
    Write-Host "? IIS가 설치되어 있습니다." -ForegroundColor Green
} else {
    $issues += "? IIS가 설치되지 않았습니다."
    $fixes += "Windows 기능에서 IIS Web Server 활성화"
}

# FTP 서비스 확인
if (Get-Service -Name "FTPSVC" -ErrorAction SilentlyContinue) {
    Write-Host "? FTP 서비스가 설치되어 있습니다." -ForegroundColor Green
} else {
    $issues += "? FTP 서비스가 설치되지 않았습니다."
    $fixes += "Windows 기능에서 IIS-FTPServer, IIS-FTPSvc 활성화"
}

# 2단계: 파일 배포 상태 확인
Write-Host "`n=== 2단계: 파일 배포 상태 확인 ===" -ForegroundColor Yellow

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
        
        # DLL 차단 확인
        try {
            $zone = Get-ItemProperty -Path $dllPath -Name Zone.Identifier -ErrorAction SilentlyContinue
            if ($zone) {
                $warnings += "?? $dll이 인터넷에서 다운로드되어 차단되었을 수 있습니다."
                if ($FixIssues) {
                    Unblock-File -Path $dllPath
                    Write-Host "? $dll 차단 해제 완료" -ForegroundColor Green
                }
            }
        } catch {
            # Zone.Identifier가 없으면 정상
        }
    } else {
        $issues += "? $dll이 IIS 시스템 디렉토리에 없습니다."
        $fixes += "deploy\integrated-deploy.ps1 실행하여 DLL 재배포"
    }
}

# 3단계: 구성 파일 확인
Write-Host "`n=== 3단계: 구성 파일 확인 ===" -ForegroundColor Yellow

$configPath = "C:\Windows\System32\inetsrv\ftpauth.config.json"
if (Test-Path $configPath) {
    Write-Host "? 구성 파일이 존재합니다: $configPath" -ForegroundColor Green
    
    try {
        $configContent = Get-Content $configPath -Raw | ConvertFrom-Json
        Write-Host "  구성 파일 내용 확인 완료" -ForegroundColor Green
        
        # 구성 유효성 검사
        if ($configContent.UserStore.Path -and $configContent.UserStore.Path -ne "C:\inetpub\ftpusers\users.json") {
            $warnings += "?? 사용자 저장소 경로가 기본값과 다릅니다: $($configContent.UserStore.Path)"
        }
        
        if ($configContent.Logging.EventLogSource -ne "IIS-FTP-SimpleAuth") {
            $warnings += "?? 이벤트 로그 소스가 기본값과 다릅니다: $($configContent.Logging.EventLogSource)"
        }
    } catch {
        $issues += "? 구성 파일 JSON 형식이 잘못되었습니다."
        $fixes += "구성 파일을 올바른 JSON 형식으로 수정"
    }
} else {
    $issues += "? 구성 파일이 없습니다: $configPath"
    $fixes += "deploy\integrated-deploy.ps1 실행하여 구성 파일 생성"
}

# 4단계: 사용자 데이터 확인
Write-Host "`n=== 4단계: 사용자 데이터 확인 ===" -ForegroundColor Yellow

$UserDataPath = "C:\inetpub\ftpusers"
if (Test-Path $UserDataPath) {
    Write-Host "? 사용자 데이터 디렉토리가 존재합니다: $UserDataPath" -ForegroundColor Green
    
    $usersFile = Join-Path $UserDataPath "users.json"
    if (Test-Path $usersFile) {
        $fileInfo = Get-Item $usersFile
        Write-Host "  ? users.json ($($fileInfo.Length) bytes)" -ForegroundColor Green
        
        # 파일 내용 확인
        try {
            $usersContent = Get-Content $usersFile -Raw | ConvertFrom-Json
            if ($usersContent.Users -and $usersContent.Users.Count -gt 0) {
                Write-Host "  ? 사용자 데이터가 포함되어 있습니다 ($($usersContent.Users.Count)명)" -ForegroundColor Green
            } else {
                $warnings += "?? users.json에 사용자 데이터가 없습니다."
            }
        } catch {
            $issues += "? users.json 형식이 잘못되었습니다."
            $fixes += "샘플 사용자 파일로 재생성"
        }
    } else {
        $issues += "? users.json 파일이 없습니다."
        $fixes += "deploy\integrated-deploy.ps1 실행하여 샘플 사용자 파일 생성"
    }
    
    # 권한 확인
    try {
        $acl = Get-Acl $UserDataPath
        $iisUsersRule = $acl.Access | Where-Object { $_.IdentityReference -eq "IIS_IUSRS" }
        if ($iisUsersRule) {
            Write-Host "  ? IIS_IUSRS 권한: $($iisUsersRule.FileSystemRights)" -ForegroundColor Green
        } else {
            $issues += "? IIS_IUSRS 권한이 설정되지 않았습니다."
            $fixes += "deploy\integrated-deploy.ps1 실행하여 권한 설정"
        }
    } catch {
        $warnings += "?? 권한 정보를 확인할 수 없습니다."
    }
} else {
    $issues += "? 사용자 데이터 디렉토리가 없습니다: $UserDataPath"
    $fixes += "deploy\integrated-deploy.ps1 실행하여 디렉토리 생성"
}

# 5단계: IIS 구성 확인
Write-Host "`n=== 5단계: IIS 구성 확인 ===" -ForegroundColor Yellow

try {
    Import-Module WebAdministration
    
    # 커스텀 인증 확인
    $customAuth = Get-WebConfiguration "/system.ftpServer/security/authentication/customAuthentication"
    if ($customAuth.enabled -eq $true) {
        Write-Host "? 커스텀 인증이 활성화되어 있습니다." -ForegroundColor Green
        
        $providers = Get-WebConfiguration "/system.ftpServer/security/authentication/customAuthentication/providers"
        if ($providers.Collection.Count -gt 0) {
            Write-Host "  ? 커스텀 인증 프로바이더가 등록되어 있습니다." -ForegroundColor Green
            foreach ($provider in $providers.Collection) {
                Write-Host "    - $($provider.name): $($provider.type)" -ForegroundColor Cyan
            }
        } else {
            $issues += "? 커스텀 인증 프로바이더가 등록되지 않았습니다."
            $fixes += "deploy\integrated-deploy.ps1 실행하여 프로바이더 등록"
        }
    } else {
        $issues += "? 커스텀 인증이 비활성화되어 있습니다."
        $fixes += "deploy\integrated-deploy.ps1 실행하여 커스텀 인증 활성화"
    }
    
    # 커스텀 권한 확인
    $customAuthz = Get-WebConfiguration "/system.ftpServer/security/authorization/customAuthorization"
    if ($customAuthz.enabled -eq $true) {
        Write-Host "? 커스텀 권한이 활성화되어 있습니다." -ForegroundColor Green
        
        $providers = Get-WebConfiguration "/system.ftpServer/security/authorization/customAuthorization/providers"
        if ($providers.Collection.Count -gt 0) {
            Write-Host "  ? 커스텀 권한 프로바이더가 등록되어 있습니다." -ForegroundColor Green
            foreach ($provider in $providers.Collection) {
                Write-Host "    - $($provider.name): $($provider.type)" -ForegroundColor Cyan
            }
        } else {
            $issues += "? 커스텀 권한 프로바이더가 등록되지 않았습니다."
            $fixes += "deploy\integrated-deploy.ps1 실행하여 프로바이더 등록"
        }
    } else {
        $issues += "? 커스텀 권한이 비활성화되어 있습니다."
        $fixes += "deploy\integrated-deploy.ps1 실행하여 커스텀 권한 활성화"
    }
    
} catch {
    $issues += "? IIS 구성을 확인할 수 없습니다: $($_.Exception.Message)"
    $fixes += "IIS 관리 모듈 설치 및 권한 확인"
}

# 6단계: 이벤트 로그 확인
Write-Host "`n=== 6단계: 이벤트 로그 확인 ===" -ForegroundColor Yellow

try {
    $eventLogs = Get-EventLog -LogName Application -Source "IIS-FTP-SimpleAuth" -Newest 5 -ErrorAction SilentlyContinue
    if ($eventLogs) {
        Write-Host "? 이벤트 로그 소스가 존재합니다." -ForegroundColor Green
        Write-Host "  최근 이벤트 ($($eventLogs.Count)개):" -ForegroundColor Cyan
        foreach ($log in $eventLogs) {
            $color = if ($log.EntryType -eq "Error") { "Red" } elseif ($log.EntryType -eq "Warning") { "Yellow" } else { "Green" }
            Write-Host "    $($log.TimeGenerated): $($log.EntryType) - $($log.Message)" -ForegroundColor $color
        }
    } else {
        $warnings += "?? 이벤트 로그 소스에 항목이 없습니다."
        
        # 이벤트 로그 소스 존재 여부 확인
        $sources = Get-EventLog -LogName Application -Newest 1 | Select-Object -ExpandProperty Source -Unique
        if ($sources -contains "IIS-FTP-SimpleAuth") {
            Write-Host "  ? 이벤트 로그 소스는 존재하지만 항목이 없습니다." -ForegroundColor Green
        } else {
            $issues += "? 이벤트 로그 소스 'IIS-FTP-SimpleAuth'가 생성되지 않았습니다."
            $fixes += "CLI 도구를 한 번 실행하여 이벤트 로그 소스 생성"
        }
    }
} catch {
    $issues += "? 이벤트 로그를 확인할 수 없습니다: $($_.Exception.Message)"
    $fixes += "이벤트 로그 권한 확인 및 CLI 도구 실행"
}

# 7단계: 서비스 상태 확인
Write-Host "`n=== 7단계: 서비스 상태 확인 ===" -ForegroundColor Yellow

$services = @("W3SVC", "FTPSVC")
foreach ($service in $services) {
    $svc = Get-Service -Name $service -ErrorAction SilentlyContinue
    if ($svc) {
        if ($svc.Status -eq "Running") {
            Write-Host "? $service 서비스가 실행 중입니다." -ForegroundColor Green
        } else {
            $issues += "? $service 서비스가 실행되지 않고 있습니다. (상태: $($svc.Status))"
            $fixes += "Start-Service $service 명령으로 서비스 시작"
        }
    } else {
        $issues += "? $service 서비스가 설치되지 않았습니다."
        $fixes += "Windows 기능에서 해당 서비스 활성화"
    }
}

# 8단계: 문제 요약 및 해결 방안
Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "? 진단 결과 요약" -ForegroundColor Yellow
Write-Host "==================================================" -ForegroundColor Cyan

if ($issues.Count -eq 0) {
    Write-Host "? 심각한 문제가 발견되지 않았습니다!" -ForegroundColor Green
} else {
    Write-Host "? 발견된 문제 ($($issues.Count)개):" -ForegroundColor Red
    foreach ($issue in $issues) {
        Write-Host "  $issue" -ForegroundColor Red
    }
}

if ($warnings.Count -gt 0) {
    Write-Host "`n?? 주의사항 ($($warnings.Count)개):" -ForegroundColor Yellow
    foreach ($warning in $warnings) {
        Write-Host "  $warning" -ForegroundColor Yellow
    }
}

if ($issues.Count -gt 0) {
    Write-Host "`n? 해결 방안:" -ForegroundColor Cyan
    foreach ($fix in $fixes) {
        Write-Host "  ? $fix" -ForegroundColor White
    }
    
    if ($FixIssues) {
        Write-Host "`n? 자동 수정을 시작합니다..." -ForegroundColor Yellow
        
        # 통합 배포 스크립트 실행
        $integratedScript = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "integrated-deploy.ps1"
        if (Test-Path $integratedScript) {
            Write-Host "통합 배포 스크립트를 실행합니다..." -ForegroundColor Yellow
            & $integratedScript -CreateAppPool -CreateSite -Force
        } else {
            Write-Error "통합 배포 스크립트를 찾을 수 없습니다: $integratedScript"
        }
    } else {
        Write-Host "`n? 자동 수정을 원하시면 -FixIssues 매개변수를 사용하세요:" -ForegroundColor Cyan
        Write-Host "  .\deploy\diagnose-ftp-issues.ps1 -FixIssues" -ForegroundColor White
    }
}

Write-Host "`n? 추가 진단 명령:" -ForegroundColor Yellow
Write-Host "  - 배포 상태 확인: .\deploy\check-deployment-status.ps1" -ForegroundColor White
Write-Host "  - 이벤트 로그 상세 확인: Get-EventLog -LogName Application -Source 'IIS-FTP-SimpleAuth' -Newest 20" -ForegroundColor White
Write-Host "  - IIS 구성 확인: Get-WebConfiguration '/system.ftpServer/security/authentication/customAuthentication'" -ForegroundColor White
Write-Host "  - 서비스 상태 확인: Get-Service W3SVC, MSFTPSVC" -ForegroundColor White

Write-Host "`n==================================================" -ForegroundColor Cyan
Write-Host "진단 완료" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Cyan
