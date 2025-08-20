# IIS FTP SimpleAuthProvider 배포 상태 확인 스크립트
param(
    [string]$IISPath = "C:\inetpub\wwwroot\ftpauth",
    [string]$BackupPath = "C:\inetpub\backup\ftpauth",
    [string]$IISSystemPath = "C:\Windows\System32\inetsrv",
    [switch]$Detailed
)

Write-Host "IIS FTP SimpleAuthProvider 배포 상태를 확인합니다..." -ForegroundColor Cyan

# 관리자 권한 확인
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Warning "관리자 권한이 없습니다. 일부 정보를 확인할 수 없을 수 있습니다."
}

# IIS 기능 확인
try {
    Import-Module WebAdministration
    $iisAvailable = $true
} catch {
    $iisAvailable = $false
    Write-Warning "IIS 관리 모듈을 로드할 수 없습니다."
}

# 웹 애플리케이션 상태 확인
Write-Host "`n=== 웹 애플리케이션 상태 ===" -ForegroundColor Green
if (Test-Path $IISPath) {
    Write-Host "? 웹 애플리케이션 디렉토리가 존재합니다: $IISPath" -ForegroundColor Green
    
    # 배포 정보 확인
    $deploymentInfoPath = Join-Path $IISPath "deployment-info.json"
    if (Test-Path $deploymentInfoPath) {
        try {
            $deploymentInfo = Get-Content $deploymentInfoPath | ConvertFrom-Json
            Write-Host "  배포 시간: $($deploymentInfo.DeployedAt)" -ForegroundColor Cyan
            Write-Host "  버전: $($deploymentInfo.Version)" -ForegroundColor Cyan
            Write-Host "  소스 경로: $($deploymentInfo.SourcePath)" -ForegroundColor Cyan
        } catch {
            Write-Warning "배포 정보를 읽을 수 없습니다."
        }
    } else {
        Write-Warning "배포 정보 파일이 없습니다."
    }
    
    # 주요 파일 확인
    $requiredFiles = @(
        "ManagementWeb.dll",
        "IIS.Ftp.SimpleAuth.Core.dll",
        "Web.config"
    )
    
    Write-Host "`n  주요 파일 상태:" -ForegroundColor Yellow
    foreach ($file in $requiredFiles) {
        $filePath = Join-Path $IISPath $file
        if (Test-Path $filePath) {
            $fileInfo = Get-Item $filePath
            Write-Host "    ? $file ($($fileInfo.Length) bytes, $($fileInfo.LastWriteTime))" -ForegroundColor Green
        } else {
            Write-Host "    ? $file (누락)" -ForegroundColor Red
        }
    }
    
    if ($Detailed) {
        # 전체 파일 목록
        Write-Host "`n  전체 파일 목록:" -ForegroundColor Yellow
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
    Write-Host "? 웹 애플리케이션 디렉토리가 존재하지 않습니다: $IISPath" -ForegroundColor Red
}

# AuthProvider DLL 상태 확인
Write-Host "`n=== AuthProvider DLL 상태 ===" -ForegroundColor Green
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
        Write-Host "  ? $dll (누락)" -ForegroundColor Red
    }
}

# IIS 사이트 및 애플리케이션 풀 상태 확인
if ($iisAvailable) {
    Write-Host "`n=== IIS 구성 상태 ===" -ForegroundColor Green
    
    # 사이트 상태
    $siteName = "ftpauth"
    $site = Get-Website -Name $siteName -ErrorAction SilentlyContinue
    if ($site) {
        Write-Host "? 웹사이트가 존재합니다: $siteName" -ForegroundColor Green
        Write-Host "  상태: $($site.State)" -ForegroundColor Cyan
        Write-Host "  포트: $($site.Bindings.Collection[0].BindingInformation)" -ForegroundColor Cyan
        Write-Host "  물리적 경로: $($site.PhysicalPath)" -ForegroundColor Cyan
    } else {
        Write-Host "? 웹사이트가 존재하지 않습니다: $siteName" -ForegroundColor Red
    }
    
    # 애플리케이션 풀 상태
    $appPoolName = "ftpauth-pool"
    $appPool = Get-IISAppPool -Name $appPoolName -ErrorAction SilentlyContinue
    if ($appPool) {
        Write-Host "? 애플리케이션 풀이 존재합니다: $appPoolName" -ForegroundColor Green
        Write-Host "  상태: $($appPool.State)" -ForegroundColor Cyan
        Write-Host "  .NET 버전: $($appPool.ManagedRuntimeVersion)" -ForegroundColor Cyan
    } else {
        Write-Host "? 애플리케이션 풀이 존재하지 않습니다: $appPoolName" -ForegroundColor Red
    }
}

# 사용자 데이터 디렉토리 상태 확인
Write-Host "`n=== 사용자 데이터 상태 ===" -ForegroundColor Green
$UserDataPath = "C:\inetpub\ftpusers"
if (Test-Path $UserDataPath) {
    Write-Host "? 사용자 데이터 디렉토리가 존재합니다: $UserDataPath" -ForegroundColor Green
    
    # 사용자 파일 확인
    $usersFile = Join-Path $UserDataPath "users.json"
    if (Test-Path $usersFile) {
        $fileInfo = Get-Item $usersFile
        Write-Host "  ? users.json ($($fileInfo.Length) bytes, $($fileInfo.LastWriteTime))" -ForegroundColor Green
    } else {
        Write-Host "  ? users.json (누락)" -ForegroundColor Red
    }
    
    # 디렉토리 권한 확인
    try {
        $acl = Get-Acl $UserDataPath
        $iisUsersRule = $acl.Access | Where-Object { $_.IdentityReference -eq "IIS_IUSRS" }
        if ($iisUsersRule) {
            Write-Host "  ? IIS_IUSRS 권한: $($iisUsersRule.FileSystemRights)" -ForegroundColor Green
        } else {
            Write-Host "  ? IIS_IUSRS 권한이 설정되지 않았습니다." -ForegroundColor Yellow
        }
    } catch {
        Write-Warning "권한 정보를 확인할 수 없습니다."
    }
} else {
    Write-Host "? 사용자 데이터 디렉토리가 존재하지 않습니다: $UserDataPath" -ForegroundColor Red
}

# 백업 상태 확인
Write-Host "`n=== 백업 상태 ===" -ForegroundColor Green
if (Test-Path $BackupPath) {
    $backups = Get-ChildItem -Path $BackupPath -Directory | Sort-Object CreationTime -Descending
    if ($backups.Count -gt 0) {
        Write-Host "? 백업이 존재합니다 ($($backups.Count)개):" -ForegroundColor Green
        foreach ($backup in $backups | Select-Object -First 5) {
            $size = (Get-ChildItem $backup.FullName -Recurse | Measure-Object -Property Length -Sum).Sum
            $sizeMB = [math]::Round($size / 1MB, 2)
            Write-Host "  ? $($backup.Name) ($sizeMB MB, $($backup.CreationTime))" -ForegroundColor Cyan
        }
        
        if ($backups.Count -gt 5) {
            Write-Host "  ... 및 $($backups.Count - 5)개의 추가 백업" -ForegroundColor Gray
        }
    } else {
        Write-Host "? 백업 디렉토리가 비어있습니다." -ForegroundColor Yellow
    }
} else {
    Write-Host "? 백업 디렉토리가 존재하지 않습니다: $BackupPath" -ForegroundColor Red
}

# 시스템 요구사항 확인
Write-Host "`n=== 시스템 요구사항 ===" -ForegroundColor Green

# .NET Framework 버전 확인
$netFrameworkPath = "HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full"
if (Test-Path $netFrameworkPath) {
    $release = Get-ItemProperty $netFrameworkPath -Name Release
    if ($release.Release -ge 528040) {
        Write-Host "? .NET Framework 4.8 이상이 설치되어 있습니다." -ForegroundColor Green
    } else {
        Write-Host "? .NET Framework 4.8 이상이 필요합니다. (현재: $($release.Release))" -ForegroundColor Yellow
    }
} else {
    Write-Host "? .NET Framework 4.0 이상이 설치되지 않았습니다." -ForegroundColor Red
}

# IIS 설치 확인
if (Get-Service -Name "W3SVC" -ErrorAction SilentlyContinue) {
    Write-Host "? IIS가 설치되어 있습니다." -ForegroundColor Green
} else {
    Write-Host "? IIS가 설치되지 않았습니다." -ForegroundColor Red
}

Write-Host "`n=== 상태 확인 완료 ===" -ForegroundColor Green
