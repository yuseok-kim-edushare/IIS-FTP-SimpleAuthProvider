# Windows 11 Pro 클라이언트에서 IIS FTP Simple Authentication Provider 실행 가이드

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![Windows 11 Pro](https://img.shields.io/badge/Windows-11%20Pro-blue.svg)](https://www.microsoft.com/windows)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-blue.svg)](https://dotnet.microsoft.com/download/dotnet-framework/net48)

Windows 11 Pro 클라이언트 환경에서 IIS FTP Simple Authentication Provider를 실행하기 위한 완전한 설정 가이드입니다.

## 📋 목차

- [시스템 요구사항](#시스템-요구사항)
- [1단계: Windows 기능 활성화](#1단계-windows-기능-활성화)
- [2단계: 솔루션 빌드 및 배포](#2단계-솔루션-빌드-및-배포)
- [3단계: Custom Provider 등록](#3단계-custom-provider-등록)
- [4단계: IIS FTP 사이트 구성](#4단계-iis-ftp-사이트-구성)
- [5단계: 테스트 및 검증](#5단계-테스트-및-검증)
- [문제 해결](#문제-해결)
- [보안 고려사항](#보안-고려사항)

## 🔧 시스템 요구사항

### Windows 11 Pro 요구사항
- **운영체제**: Windows 11 Pro (빌드 22000.0 이상)
- **아키텍처**: x64 (64비트)
- **메모리**: 최소 4GB RAM (권장 8GB 이상)
- **디스크 공간**: 최소 10GB 여유 공간
- **권한**: 로컬 관리자 권한 필요

### 소프트웨어 요구사항
- **.NET Framework SDK**: 4.8 이상 (windows 11 Pro에는 .NET Framework Runtime이 4.8 버전으로 내장되어 있음음)
- **PowerShell**: 5.1 이상 (Windows 11 Pro에 기본 포함)
- **빌드 도구**: 다음 중 하나 선택
  - **.NET SDK 9.x 이상** (권장)
  - **Visual Studio 2022 Community 이상**

## 🚀 1단계: Windows 기능 활성화

### 1.1 IIS 및 FTP 서비스 활성화

Windows 11 Pro에서는 IIS가 기본적으로 설치되어 있지 않습니다. 다음 명령으로 필요한 기능을 활성화해야 합니다:

```powershell
# 관리자 권한으로 PowerShell 실행
# Windows 기능 활성화
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServerRole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-CommonHttpFeatures
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpErrors
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpLogging
Enable-WindowsOptionalFeature -Online -FeatureName IIS-RequestFiltering
Enable-WindowsOptionalFeature -Online -FeatureName IIS-StaticContent
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DefaultDocument
Enable-WindowsOptionalFeature -Online -FeatureName IIS-DirectoryBrowsing
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ASPNET45
Enable-WindowsOptionalFeature -Online -FeatureName IIS-NetFxExtensibility45
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HealthAndDiagnostics
Enable-WindowsOptionalFeature -Online -FeatureName IIS-HttpCompressionDynamic
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebSockets
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ApplicationInit

# FTP 서비스 활성화
Enable-WindowsOptionalFeature -Online -FeatureName IIS-FTPServer
Enable-WindowsOptionalFeature -Online -FeatureName IIS-FTPSvc
Enable-WindowsOptionalFeature -Online -FeatureName IIS-FTPExtensibility

# 관리 도구 활성화
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementConsole
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementScriptingTools
Enable-WindowsOptionalFeature -Online -FeatureName IIS-ManagementService
```

### 1.2 활성화 확인

```powershell
# 활성화된 기능 확인
Get-WindowsOptionalFeature -Online -FeatureName IIS-* | Where-Object { $_.State -eq "Enabled" }

# IIS 서비스 상태 확인
Get-Service -Name "W3SVC", "MSFTPSVC" | Select-Object Name, Status, StartType
```

### 1.3 방화벽 규칙 설정

```powershell
# HTTP 및 FTP 포트 방화벽 규칙 추가
New-NetFirewallRule -DisplayName "IIS HTTP" -Direction Inbound -Protocol TCP -LocalPort 80 -Action Allow
New-NetFirewallRule -DisplayName "IIS FTP" -Direction Inbound -Protocol TCP -LocalPort 21 -Action Allow
New-NetFirewallRule -DisplayName "IIS FTP Data" -Direction Inbound -Protocol TCP -LocalPort 20 -Action Allow

# FTP 수동 모드 포트 범위 (필요시)
New-NetFirewallRule -DisplayName "IIS FTP Passive" -Direction Inbound -Protocol TCP -LocalPort 49152-65535 -Action Allow
```

## 🏗️ 2단계: 솔루션 빌드 및 배포

### 2.1 솔루션 빌드

```powershell
# 프로젝트 디렉토리로 이동
cd "C:\your-path-to\IIS-FTP-SimpleAuthProvider"

# 의존성 복원 및 빌드
dotnet restore
dotnet build --configuration Release

# 빌드 성공 확인
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ 빌드 성공!" -ForegroundColor Green
} else {
    Write-Host "❌ 빌드 실패!" -ForegroundColor Red
    exit 1
}
```

### 2.2 배포 디렉토리 생성

```powershell
# 배포 디렉토리 생성
$deployDirs = @(
    "C:\Program Files\IIS-FTP-SimpleAuth",
    "C:\inetpub\wwwroot\ftpauth",
    "C:\inetpub\ftpusers",
    "C:\inetpub\ftproot"
)

foreach ($dir in $deployDirs) {
    New-Item -ItemType Directory -Path $dir -Force | Out-Null
    Write-Host "✅ 디렉토리 생성: $dir" -ForegroundColor Green
}
```

### 2.3 컴포넌트 배포

```powershell
# ManagementWeb 배포
Write-Host "🌐 ManagementWeb 배포 중..." -ForegroundColor Yellow
Get-ChildItem "src\ManagementWeb" -Exclude "bin", "obj" | Copy-Item -Destination "C:\inetpub\wwwroot\ftpauth\" -Recurse -Force
Copy-Item "src\ManagementWeb\bin\Release\net48\*" "C:\inetpub\wwwroot\ftpauth\bin\" -Recurse -Force

# CLI 도구 배포
Write-Host "🛠️ CLI 도구 배포 중..." -ForegroundColor Yellow
Copy-Item "src\ManagementCli\bin\Release\net48\ftpauth.exe" "C:\Program Files\IIS-FTP-SimpleAuth\"

# Core 라이브러리 배포
Write-Host "📚 Core 라이브러리 배포 중..." -ForegroundColor Yellow
Copy-Item "src\Core\bin\Release\net48\*.dll" "C:\Windows\System32\inetsrv\"

# AuthProvider 배포 (중요!)
Write-Host "🔐 AuthProvider 배포 중..." -ForegroundColor Yellow
Copy-Item "src\AuthProvider\bin\Release\net48\*.dll" "C:\Windows\System32\inetsrv\"

Write-Host "✅ 모든 컴포넌트 배포 완료!" -ForegroundColor Green
```

## 🔧 3단계: Custom Provider 등록

### 3.1 GAC (Global Assembly Cache) 등록

Windows 11 Pro 클라이언트에서는 custom provider를 GAC에 등록해야 IIS가 인식할 수 있습니다:

```powershell
# GAC 등록 도구 확인
$gacutil = "${env:ProgramFiles(x86)}\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\gacutil.exe"

if (Test-Path $gacutil) {
    Write-Host "✅ GAC 도구 발견: $gacutil" -ForegroundColor Green
} else {
    # Visual Studio 설치 경로에서 찾기
    $gacutil = "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\Common7\Tools\VsDevCmd.bat"
    if (Test-Path $gacutil) {
        Write-Host "✅ Visual Studio 개발자 명령 프롬프트 발견" -ForegroundColor Green
    } else {
        Write-Host "❌ GAC 도구를 찾을 수 없습니다. Visual Studio 또는 .NET FX SDK를 설치하세요." -ForegroundColor Red
        exit 1
    }
}

# Core 라이브러리 GAC 등록
Write-Host "🔧 Core 라이브러리 GAC 등록 중..." -ForegroundColor Yellow
& $gacutil -i "C:\Windows\System32\inetsrv\IIS.Ftp.SimpleAuth.Core.dll"

# AuthProvider GAC 등록
Write-Host "🔧 AuthProvider GAC 등록 중..." -ForegroundColor Yellow
& $gacutil -i "C:\Windows\System32\inetsrv\IIS.Ftp.SimpleAuth.Provider.dll"

# GAC 등록 확인
Write-Host "🔍 GAC 등록 확인 중..." -ForegroundColor Yellow
& $gacutil -l | Select-String "IIS.Ftp.SimpleAuth"
```

### 3.2 IIS 구성 파일에 Provider 등록

```powershell
# IIS 구성 파일 경로
$configPath = "C:\Windows\System32\inetsrv\config\applicationHost.config"

# 백업 생성
Copy-Item $configPath "$configPath.backup.$(Get-Date -Format 'yyyyMMdd-HHmmss')"

# Provider 등록을 위한 XML 구성
$providerConfig = @"
        <add name="SimpleFtpAuthenticationProvider" type="IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthenticationProvider, IIS.Ftp.SimpleAuth.Provider" />
"@

$authorizationConfig = @"
        <add name="SimpleFtpAuthorizationProvider" type="IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthorizationProvider, IIS.Ftp.SimpleAuth.Provider" />
"@

# XML 파일 수정 (PowerShell로 직접 수정)
$xml = [xml](Get-Content $configPath)

# FTP 서버 섹션 찾기
$ftpServer = $xml.configuration.'system.ftpServer'

if ($ftpServer) {
    # Custom Authentication Providers 섹션 찾기 또는 생성
    $customAuth = $ftpServer.security.authentication.customAuthentication
    if (-not $customAuth) {
        $customAuth = $xml.CreateElement("customAuthentication")
        $ftpServer.security.authentication.AppendChild($customAuth)
    }
    
    # Providers 섹션 찾기 또는 생성
    $providers = $customAuth.providers
    if (-not $providers) {
        $providers = $xml.CreateElement("providers")
        $customAuth.AppendChild($providers)
    }
    
    # Provider 추가
    $provider = $xml.CreateElement("add")
    $provider.SetAttribute("name", "SimpleFtpAuthenticationProvider")
    $provider.SetAttribute("type", "IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthenticationProvider, IIS.Ftp.SimpleAuth.Provider")
    $providers.AppendChild($provider)
    
    # Custom Authorization Providers 섹션 찾기 또는 생성
    $customAuthz = $ftpServer.security.authorization.customAuthorization
    if (-not $customAuthz) {
        $customAuthz = $xml.CreateElement("customAuthorization")
        $ftpServer.security.authorization.AppendChild($customAuthz)
    }
    
    # Authorization Providers 섹션 찾기 또는 생성
    $authzProviders = $customAuthz.providers
    if (-not $authzProviders) {
        $authzProviders = $xml.CreateElement("providers")
        $customAuthz.AppendChild($authzProviders)
    }
    
    # Authorization Provider 추가
    $authzProvider = $xml.CreateElement("add")
    $authzProvider.SetAttribute("name", "SimpleFtpAuthorizationProvider")
    $authzProvider.SetAttribute("type", "IIS.Ftp.SimpleAuth.Provider.SimpleFtpAuthorizationProvider, IIS.Ftp.SimpleAuth.Provider")
    $authzProviders.AppendChild($authzProvider)
    
    # 파일 저장
    $xml.Save($configPath)
    Write-Host "✅ IIS 구성 파일에 Provider 등록 완료!" -ForegroundColor Green
} else {
    Write-Host "❌ FTP 서버 섹션을 찾을 수 없습니다." -ForegroundColor Red
}
```

### 3.3 IIS 재시작

```powershell
# IIS 재시작
Write-Host "🔄 IIS 재시작 중..." -ForegroundColor Yellow
iisreset /restart

# 서비스 상태 확인
Start-Sleep -Seconds 5
Get-Service -Name "W3SVC", "MSFTPSVC" | Select-Object Name, Status, StartType
```

## 🌐 4단계: IIS FTP 사이트 구성

### 4.1 FTP 사이트 생성

```powershell
# IIS 관리 모듈 로드
Import-Module WebAdministration

# FTP 사이트 생성
Write-Host "🌐 FTP 사이트 생성 중..." -ForegroundColor Yellow
New-WebFtpSite -Name "FTP-SimpleAuth" -Port 21 -PhysicalPath "C:\inetpub\ftproot"

# FTP 사이트 설정
Set-ItemProperty -Path "IIS:\Sites\FTP-SimpleAuth" -Name "ftpServer.security.authentication.basicAuthentication.enabled" -Value $false
Set-ItemProperty -Path "IIS:\Sites\FTP-SimpleAuth" -Name "ftpServer.security.authentication.anonymousAuthentication.enabled" -Value $false
Set-ItemProperty -Path "IIS:\Sites\FTP-SimpleAuth" -Name "ftpServer.security.authentication.customAuthentication.enabled" -Value $true

# Custom Provider 활성화
Set-ItemProperty -Path "IIS:\Sites\FTP-SimpleAuth" -Name "ftpServer.security.authentication.customAuthentication.providers" -Value "SimpleFtpAuthenticationProvider"
Set-ItemProperty -Path "IIS:\Sites\FTP-SimpleAuth" -Name "ftpServer.security.authorization.customAuthorization.enabled" -Value $true
Set-ItemProperty -Path "IIS:\Sites\FTP-SimpleAuth" -Name "ftpServer.security.authorization.customAuthorization.providers" -Value "SimpleFtpAuthorizationProvider"

Write-Host "✅ FTP 사이트 구성 완료!" -ForegroundColor Green
```

### 4.2 애플리케이션 풀 생성

```powershell
# 웹 애플리케이션용 애플리케이션 풀 생성
Write-Host "🏊 애플리케이션 풀 생성 중..." -ForegroundColor Yellow
New-WebAppPool -Name "FTP-SimpleAuth-Pool"
Set-ItemProperty -Path "IIS:\AppPools\FTP-SimpleAuth-Pool" -Name "managedRuntimeVersion" -Value "v4.0"
Set-ItemProperty -Path "IIS:\AppPools\FTP-SimpleAuth-Pool" -Name "managedPipelineMode" -Value "Integrated"

# 웹 애플리케이션 생성
New-WebApplication -Name "ftpauth" -Site "Default Web Site" -PhysicalPath "C:\inetpub\wwwroot\ftpauth" -ApplicationPool "FTP-SimpleAuth-Pool"

Write-Host "✅ 웹 애플리케이션 구성 완료!" -ForegroundColor Green
```

## ⚙️ 5단계: 테스트 및 검증

### 5.1 Provider 등록 확인

```powershell
# GAC 등록 확인
Write-Host "🔍 GAC 등록 확인 중..." -ForegroundColor Yellow
& $gacutil -l | Select-String "IIS.Ftp.SimpleAuth"

# IIS 구성 확인
Write-Host "🔍 IIS 구성 확인 중..." -ForegroundColor Yellow
Get-WebConfiguration "/system.ftpServer/security/authentication/customAuthentication" -Site "FTP-SimpleAuth"
Get-WebConfiguration "/system.ftpServer/security/authorization/customAuthorization" -Site "FTP-SimpleAuth"
```

### 5.2 웹 인터페이스 테스트

```powershell
# 웹 인터페이스 접근성 테스트
Write-Host "🧪 웹 인터페이스 테스트 중..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost/ftpauth/" -UseBasicParsing
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ 웹 인터페이스 접근 가능!" -ForegroundColor Green
    } else {
        Write-Host "❌ 웹 인터페이스 상태 코드: $($response.StatusCode)" -ForegroundColor Red
    }
} catch {
    Write-Host "❌ 웹 인터페이스 테스트 실패: $($_.Exception.Message)" -ForegroundColor Red
}
```

### 5.3 FTP 연결 테스트

```powershell
# FTP 서비스 상태 확인
Write-Host "🧪 FTP 서비스 테스트 중..." -ForegroundColor Yellow
$ftpService = Get-Service -Name "MSFTPSVC"
if ($ftpService.Status -eq "Running") {
    Write-Host "✅ FTP 서비스 실행 중!" -ForegroundColor Green
    Write-Host "FTP 클라이언트로 테스트:" -ForegroundColor Cyan
    Write-Host "Host: localhost" -ForegroundColor White
    Write-Host "Port: 21" -ForegroundColor White
    Write-Host "Username: admin" -ForegroundColor White
    Write-Host "Password: SecureAdminPass123!" -ForegroundColor White
} else {
    Write-Host "❌ FTP 서비스가 실행되지 않음: $($ftpService.Status)" -ForegroundColor Red
}
```

## 🔧 문제 해결

### 일반적인 문제들

#### 1. Provider 로드 실패
```powershell
# 이벤트 로그 확인
Get-EventLog -LogName Application -Source "IIS-FTP-SimpleAuth" -Newest 10

# GAC 등록 재확인
& $gacutil -l | Select-String "IIS.Ftp.SimpleAuth"

# DLL 파일 권한 확인
Get-Acl "C:\Windows\System32\inetsrv\IIS.Ftp.SimpleAuth.Provider.dll" | Format-List
```

#### 2. IIS 구성 오류
```powershell
# IIS 구성 파일 검증
%windir%\system32\inetsrv\appcmd.exe config /validate

# 구성 백업에서 복원
$backupFile = Get-ChildItem "C:\Windows\System32\inetsrv\config\applicationHost.config.backup.*" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($backupFile) {
    Copy-Item $backupFile.FullName "C:\Windows\System32\inetsrv\config\applicationHost.config" -Force
    Write-Host "✅ 구성 파일 복원 완료: $($backupFile.Name)" -ForegroundColor Green
    iisreset /restart
}
```

#### 3. 권한 문제
```powershell
# IIS_IUSRS 그룹에 필요한 권한 부여
$acl = Get-Acl "C:\inetpub\ftpusers"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "Read", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl "C:\inetpub\ftpusers" $acl

# 이벤트 로그 소스 생성
& "C:\Program Files\IIS-FTP-SimpleAuth\ftpauth.exe" list-users -f "C:\inetpub\ftpusers\dummy.json"
```

### 디버깅 도구

#### 1. IIS 로그 확인
```powershell
# IIS 로그 파일 위치
$logPath = "C:\inetpub\logs\LogFiles\FTP-SimpleAuth"
if (Test-Path $logPath) {
    Get-ChildItem $logPath | Sort-Object LastWriteTime -Descending | Select-Object -First 5
}
```

#### 2. 프로세스 모니터링
```powershell
# IIS 프로세스 확인
Get-Process | Where-Object { $_.ProcessName -like "*w3wp*" -or $_.ProcessName -like "*inetinfo*" } | Select-Object ProcessName, Id, WorkingSet
```

## 🛡️ 보안 고려사항

### Windows 11 Pro 클라이언트 특화 보안

#### 1. 방화벽 설정
```powershell
# 기본 방화벽 규칙만 허용 (필요한 포트만)
Get-NetFirewallRule -DisplayName "IIS*" | Set-NetFirewallRule -Profile Private,Domain

# 불필요한 방화벽 규칙 제거
Get-NetFirewallRule -DisplayName "IIS*" | Where-Object { $_.DisplayName -notlike "*HTTP*" -and $_.DisplayName -notlike "*FTP*" } | Remove-NetFirewallRule
```

#### 2. 사용자 계정 보안
```powershell
# 기본 관리자 계정 비활성화 (선택사항)
# Set-LocalUser -Name "Administrator" -Enabled $false

# 새로운 관리자 계정 생성
New-LocalUser -Name "FTPAdmin" -Description "FTP Management Account" -Password (ConvertTo-SecureString "SecurePass123!" -AsPlainText -Force)
Add-LocalGroupMember -Group "Administrators" -Member "FTPAdmin"
```

#### 3. 네트워크 보안
```powershell
# FTP 수동 모드 포트 범위 제한
Set-NetFirewallRule -DisplayName "IIS FTP Passive" -LocalPort 50000-51000

# 특정 IP에서만 접근 허용 (필요시)
New-NetFirewallRule -DisplayName "IIS FTP Restricted" -Direction Inbound -Protocol TCP -LocalPort 21 -RemoteAddress 192.168.1.0/24 -Action Allow
```

## 📚 추가 리소스

### 관련 문서
- [메인 README](../readme.md) - 프로젝트 개요 및 빠른 시작
- [설치 및 설정 가이드](installation-and-setup-guide.md) - 상세 설치 가이드
- [디자인 문서](design.md) - 아키텍처 및 설계 결정사항

### 지원 및 문제 해결
- **GitHub Issues**: 버그 신고 및 기능 요청
- **이벤트 로그**: Windows 이벤트 뷰어에서 상세 오류 정보 확인
- **커뮤니티**: GitHub Discussions에서 토론 참여

### 유용한 도구
- **IIS Manager**: 기본 IIS 관리 인터페이스
- **PowerShell**: 자동화 및 일괄 작업
- **Event Viewer**: 시스템 및 애플리케이션 모니터링
- **Process Monitor**: 파일 및 레지스트리 액세스 추적

---

## 🎯 빠른 참조 명령어

### 필수 명령어
```powershell
# 시스템 상태 확인
Get-Service -Name "W3SVC", "MSFTPSVC"

# IIS 재시작
iisreset /restart

# 웹 애플리케이션 확인
Get-WebApplication -Site "Default Web Site"

# FTP 사용자 목록
& "C:\Program Files\IIS-FTP-SimpleAuth\ftpauth.exe" list-users -f "C:\inetpub\ftpusers\users.json"

# 새 암호화 키 생성
& "C:\Program Files\IIS-FTP-SimpleAuth\ftpauth.exe" generate-key
```

### 구성 파일 위치
- **웹 구성**: `C:\inetpub\wwwroot\ftpauth\Web.config`
- **FTP 구성**: `C:\inetpub\ftpusers\ftpauth.config.json`
- **사용자 저장소**: `C:\inetpub\ftpusers\users.enc`
- **이벤트 로그**: Windows 이벤트 뷰어 → 애플리케이션 → IIS-FTP-SimpleAuth

---

*이 가이드는 Windows 11 Pro 클라이언트 환경에서 IIS FTP Simple Authentication Provider를 실행하기 위한 완전한 설정 과정을 다룹니다. 고급 구성 옵션은 개별 컴포넌트 문서를 참조하세요.*
