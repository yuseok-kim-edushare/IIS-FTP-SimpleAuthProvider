# IIS FTP SimpleAuthProvider 배포 가이드

이 폴더에는 IIS FTP SimpleAuthProvider를 IIS에 배포하고 관리하기 위한 PowerShell 스크립트들이 포함되어 있습니다.

## 📋 사전 요구사항

### 시스템 요구사항
- Windows Server 2016+ 또는 Windows 10/11 Pro
- IIS 10.0+ 설치 및 활성화
- .NET Framework 4.8 설치
- IIS FTP Server 기능 설치
- PowerShell 5.0+ (Windows 10/11 기본 포함)

### 권한 요구사항
- **관리자 권한**: 모든 스크립트는 관리자 권한으로 실행해야 합니다
- **IIS 관리 권한**: IIS 사이트 및 애플리케이션 풀 생성/수정 권한

## 🚀 스크립트 개요

### 1. `integrated-deploy.ps1` - 🆕 통합 배포 스크립트 (권장)
**전체 시스템을 한 번에 배포하고 구성하는 통합 스크립트입니다.**

**주요 기능:**
- 프로젝트 빌드부터 IIS 구성까지 전체 과정 자동화
- FTP 커스텀 프로바이더 자동 등록
- 구성 파일 자동 생성 및 배치
- 샘플 사용자 데이터 생성
- 서비스 자동 재시작
- 10단계 체계적 배포 프로세스

**사용법:**
```powershell
# 전체 시스템 통합 배포 (권장)
.\integrated-deploy.ps1 -CreateAppPool -CreateSite

# 빌드 건너뛰기 (이미 빌드된 경우)
.\integrated-deploy.ps1 -CreateAppPool -CreateSite -SkipBuild

# 기존 배포 강제 덮어쓰기
.\integrated-deploy.ps1 -CreateAppPool -CreateSite -Force
```

### 2. `diagnose-ftp-issues.ps1` - 🆕 문제 진단 스크립트
**FTP 인증 문제를 체계적으로 진단하고 해결 방법을 제시합니다.**

**주요 기능:**
- 8단계 체계적 문제 진단
- 시스템 요구사항, 파일 배포, IIS 구성 등 전면 점검
- 발견된 문제에 대한 구체적 해결 방안 제시
- 자동 수정 옵션 (통합 배포 스크립트 연동)

**사용법:**
```powershell
# 문제 진단만 수행
.\diagnose-ftp-issues.ps1

# 문제 진단 후 자동 수정
.\diagnose-ftp-issues.ps1 -FixIssues

# 상세 정보 포함
.\diagnose-ftp-issues.ps1 -Verbose
```

### 3. `deploy-to-iis.ps1` - 메인 배포 스크립트
IIS FTP SimpleAuthProvider를 IIS에 처음 배포하거나 기존 배포를 업데이트합니다.

**주요 기능:**
- 웹 애플리케이션 파일 복사
- AuthProvider DLL을 IIS 시스템 디렉토리에 복사
- 사용자 데이터 디렉토리 생성 및 권한 설정
- IIS 애플리케이션 풀 및 사이트 생성 (선택사항)
- 기존 배포 자동 백업

**사용법:**
```powershell
# 기본 배포
.\deploy-to-iis.ps1

# IIS 구성 요소까지 생성
.\deploy-to-iis.ps1 -CreateAppPool -CreateSite

# 기존 배포 강제 덮어쓰기
.\deploy-to-iis.ps1 -Force
```

### 4. `deploy-web-service-local.ps1` - 웹 서비스 로컬 배포 스크립트
**웹 관리 인터페이스만을 로컬 IIS에 배포하고 테스트합니다.**

**주요 기능:**
- ManagementWeb 프로젝트 빌드 및 배포
- IIS 애플리케이션 풀 및 사이트 자동 생성
- 웹 서비스 테스트 및 상태 확인
- 개발/테스트 환경에 최적화

**사용법:**
```powershell
# 기본 웹 서비스 배포
.\deploy-web-service-local.ps1

# 커스텀 설정으로 배포
.\deploy-web-service-local.ps1 -Port 9000 -Configuration Debug

# 배포만 수행 (빌드 및 테스트 건너뛰기)
.\deploy-web-service-local.ps1 -SkipBuild -SkipTest
```

### 5. `quick-deploy.ps1` - 빠른 배포 스크립트
개발/테스트 환경에서 빠른 배포를 위한 간소화된 스크립트입니다.

**주요 기능:**
- 프로젝트 자동 빌드
- IIS 배포 자동화
- 배포 상태 자동 확인

**사용법:**
```powershell
# 빠른 배포 실행
.\quick-deploy.ps1
```

### 6. `update-deployment.ps1` - 업데이트 스크립트
기존 배포를 안전하게 업데이트합니다.

**주요 기능:**
- 현재 배포 자동 백업
- 파일 및 DLL 업데이트
- 오류 시 자동 롤백 (선택사항)

**사용법:**
```powershell
# 기본 업데이트
.\update-deployment.ps1

# 오류 시 자동 롤백
.\update-deployment.ps1 -RollbackOnError
```

### 7. `rollback-deployment.ps1` - 배포 철회 스크립트
배포를 철회하고 시스템을 이전 상태로 복원합니다.

**주요 기능:**
- 웹 애플리케이션 제거
- AuthProvider DLL 제거/복원
- IIS 구성 요소 제거 (선택사항)
- 백업에서 복원 옵션

**사용법:**
```powershell
# 기본 철회
.\rollback-deployment.ps1

# IIS 구성 요소까지 제거
.\rollback-deployment.ps1 -RemoveSite -RemoveAppPool

# 강제 철회 (확인 없음)
.\rollback-deployment.ps1 -Force
```

### 8. `check-deployment-status.ps1` - 상태 확인 스크립트
현재 배포 상태와 시스템 상태를 확인합니다.

**주요 기능:**
- 웹 애플리케이션 상태 확인
- AuthProvider DLL 상태 확인
- IIS 구성 상태 확인
- 백업 상태 확인
- 시스템 요구사항 확인

**사용법:**
```powershell
# 기본 상태 확인
.\check-deployment-status.ps1

# 상세 정보 포함
.\check-deployment-status.ps1 -Detailed
```

### 9. `cleanup-web-service-local.ps1` - 웹 서비스 정리 스크립트
로컬 IIS에서 웹 서비스를 제거하고 정리합니다.

**주요 기능:**
- IIS 사이트 및 애플리케이션 풀 제거
- 배포된 파일 정리
- 로컬 환경 정리

**사용법:**
```powershell
# 웹 서비스 정리
.\cleanup-web-service-local.ps1

# 강제 정리 (확인 없음)
.\cleanup-web-service-local.ps1 -Force
```

### 10. `test-build.ps1` - 빌드 테스트 스크립트
배포 전에 빌드 프로세스를 테스트합니다.

**주요 기능:**
- 빌드 도구 및 환경 확인
- 프로젝트 빌드 테스트
- 배포 전 검증

**사용법:**
```powershell
# 빌드 테스트
.\test-build.ps1

# Debug 구성으로 테스트
.\test-build.ps1 -Configuration Debug
```

## 🔧 개선된 배포 워크플로우

### 🆕 권장 워크플로우 (통합 배포)
```powershell
# 1. 문제 진단 (선택사항)
.\diagnose-ftp-issues.ps1

# 2. 통합 배포 (전체 시스템 구성)
.\integrated-deploy.ps1 -CreateAppPool -CreateSite

# 3. 배포 상태 확인
.\check-deployment-status.ps1

# 4. 문제 발생 시 진단 및 자동 수정
.\diagnose-ftp-issues.ps1 -FixIssues
```

### 웹 서비스 전용 워크플로우
```powershell
# 1. 빌드 테스트
.\test-build.ps1

# 2. 웹 서비스 배포
.\deploy-web-service-local.ps1

# 3. 상태 확인
.\check-deployment-status.ps1

# 4. 필요시 정리
.\cleanup-web-service-local.ps1
```

### 기존 워크플로우
```powershell
# 1. 프로젝트 빌드
dotnet build --configuration Release

# 2. IIS에 배포
.\deploy-to-iis.ps1 -CreateAppPool -CreateSite

# 3. 배포 상태 확인
.\check-deployment-status.ps1
```

### 업데이트
```powershell
# 1. 프로젝트 빌드
dotnet build --configuration Release

# 2. 안전한 업데이트
.\update-deployment.ps1 -RollbackOnError

# 3. 업데이트 후 상태 확인
.\check-deployment-status.ps1
```

### 문제 발생 시
```powershell
# 1. 문제 진단
.\diagnose-ftp-issues.ps1

# 2. 자동 수정 (권장)
.\diagnose-ftp-issues.ps1 -FixIssues

# 3. 수동 수정이 필요한 경우
.\check-deployment-status.ps1
.\rollback-deployment.ps1
```

## 📁 배포 구조

```
C:\inetpub\wwwroot\ftpauth\          # 웹 애플리케이션
├── ManagementWeb.dll                 # 메인 웹 애플리케이션
├── IIS.Ftp.SimpleAuth.Core.dll      # 핵심 라이브러리
├── Web.config                        # 웹 설정
├── deployment-info.json              # 배포 정보
└── [기타 웹 파일들]

C:\Windows\System32\inetsrv\         # IIS 시스템 디렉토리
├── IIS.Ftp.SimpleAuth.Provider.dll  # FTP 인증 공급자
├── IIS.Ftp.SimpleAuth.Core.dll      # 핵심 라이브러리
├── WelsonJS.Esent.dll               # ESENT 데이터베이스 지원
├── Esent.Interop.dll                # ESENT 인터페이스
└── ftpauth.config.json              # 🆕 구성 파일 (기본 위치)

C:\inetpub\ftpusers\                 # 사용자 데이터
├── users.json                        # 사용자 정보
└── [기타 데이터 파일들]

C:\inetpub\backup\ftpauth\           # 백업 디렉토리
├── 20241201_143022\                 # 타임스탬프별 백업
├── 20241201_150015\
└── [DLL 백업 파일들]
```

## 🆕 주요 개선사항

### 1. 통합 배포 스크립트
- **10단계 체계적 배포**: 빌드부터 IIS 구성까지 전체 과정 자동화
- **FTP 프로바이더 자동 등록**: 수동 IIS 설정 불필요
- **구성 파일 자동 생성**: 올바른 위치에 올바른 형식으로 배치
- **서비스 자동 재시작**: 배포 후 즉시 사용 가능

### 2. 문제 진단 시스템
- **8단계 체계적 진단**: 시스템 요구사항부터 IIS 구성까지 전면 점검
- **구체적 해결 방안**: 발견된 문제에 대한 명확한 해결 방법 제시
- **자동 수정 옵션**: 진단 후 자동으로 문제 해결

### 3. 웹 서비스 전용 배포
- **빌드 테스트**: 배포 전 빌드 프로세스 검증
- **로컬 환경 최적화**: 개발/테스트 환경에 맞춘 설정
- **자동 정리**: 필요시 배포된 리소스 정리

### 4. 향상된 오류 처리
- **DLL 차단 해제**: 인터넷 다운로드로 인한 차단 자동 해제
- **권한 문제 진단**: IIS_IUSRS 권한 설정 상태 확인
- **구성 파일 검증**: JSON 형식 및 내용 유효성 검사

### 5. 표준화된 구성
- **일관된 명명 규칙**: 모든 스크립트에서 `ftpauth` 사용
- **중앙화된 설정**: `deployment-config.ps1`로 통일된 구성 관리
- **재사용 가능한 함수**: 공통 기능의 모듈화

## ⚠️ 주의사항

### 보안
- 모든 스크립트는 관리자 권한으로 실행됩니다
- 프로덕션 환경에서 실행하기 전에 테스트 환경에서 검증하세요
- 백업이 자동으로 생성되지만 중요한 데이터는 별도로 백업하세요

### IIS 설정
- 🆕 **통합 배포 스크립트 사용 시**: FTP 커스텀 프로바이더가 자동으로 등록됩니다
- 🆕 **수동 배포 시**: IIS FTP 사이트에 AuthProvider를 수동으로 등록해야 합니다
- 애플리케이션 풀의 .NET Framework 버전이 4.0으로 설정되어야 합니다
- IIS_IUSRS 그룹에 사용자 데이터 디렉토리 접근 권한이 필요합니다

### 문제 해결
- 🆕 **문제 진단 스크립트**: `diagnose-ftp-issues.ps1`을 먼저 실행하여 문제 파악
- 스크립트 실행 오류 시 PowerShell 실행 정책을 확인하세요: `Get-ExecutionPolicy`
- IIS 관리 모듈 로드 실패 시 IIS가 제대로 설치되었는지 확인하세요
- DLL 복사 실패 시 바이러스 백신 소프트웨어의 간섭을 확인하세요

## 🔍 문제 해결

### 일반적인 문제들

#### 1. PowerShell 실행 정책 오류
```powershell
# 현재 정책 확인
Get-ExecutionPolicy

# 정책 변경 (관리자 권한 필요)
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

#### 2. IIS 관리 모듈 로드 실패
```powershell
# IIS 기능 확인
Get-WindowsFeature -Name Web-Server

# IIS 관리 도구 설치
Install-WindowsFeature -Name Web-Mgmt-Tools
```

#### 3. 권한 오류
```powershell
# IIS_IUSRS 그룹에 권한 부여
$acl = Get-Acl "C:\inetpub\ftpusers"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "Modify", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($accessRule)
Set-Acl -Path "C:\inetpub\ftpusers" -AclObject $acl
```

#### 🆕 4. FTP 인증 실패 (새로 추가)
```powershell
# 문제 진단 실행
.\diagnose-ftp-issues.ps1

# 자동 수정 실행
.\diagnose-ftp-issues.ps1 -FixIssues

# 이벤트 로그 확인
Get-EventLog -LogName Application -Source "IIS-FTP-SimpleAuth" -Newest 20
```

#### 🆕 5. 구성 파일 문제 (새로 추가)
```powershell
# 구성 파일 위치 확인
Test-Path "C:\Windows\System32\inetsrv\ftpauth.config.json"

# 구성 파일 내용 확인
Get-Content "C:\Windows\System32\inetsrv\ftpauth.config.json" | ConvertFrom-Json

# 통합 배포로 재생성
.\integrated-deploy.ps1 -CreateAppPool -CreateSite -Force
```

#### 🆕 6. 웹 서비스 배포 문제 (새로 추가)
```powershell
# 빌드 테스트
.\test-build.ps1

# 웹 서비스 배포
.\deploy-web-service-local.ps1

# 상태 확인
.\check-deployment-status.ps1

# 필요시 정리
.\cleanup-web-service-local.ps1
```

## 📞 지원

문제가 발생하거나 추가 도움이 필요한 경우:
1. 🆕 **`diagnose-ftp-issues.ps1`을 실행하여 문제 진단** (권장)
2. `check-deployment-status.ps1`을 실행하여 현재 상태 확인
3. Windows 이벤트 로그에서 오류 메시지 확인
4. 프로젝트 이슈 페이지에 문제 보고

## 📝 변경 이력

- **v1.0.0**: 초기 배포 스크립트 생성
- **v1.1.0**: 업데이트 및 롤백 기능 추가
- **v1.2.0**: 상태 확인 및 문제 해결 기능 추가
- **v2.0.0**: 🆕 통합 배포 스크립트 및 문제 진단 시스템 추가
  - `integrated-deploy.ps1`: 전체 시스템 통합 배포
  - `diagnose-ftp-issues.ps1`: 체계적 문제 진단 및 자동 수정
  - 향상된 오류 처리 및 사용자 경험 개선
- **v2.1.0**: 🆕 웹 서비스 전용 배포 및 표준화 개선
  - `deploy-web-service-local.ps1`: 웹 서비스 전용 배포
  - `cleanup-web-service-local.ps1`: 웹 서비스 정리
  - `test-build.ps1`: 빌드 프로세스 테스트
  - `deployment-config.ps1`: 표준화된 구성 관리
  - 일관된 명명 규칙 및 경로 표준화
