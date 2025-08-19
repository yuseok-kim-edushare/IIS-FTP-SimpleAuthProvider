# IIS FTP SimpleAuthProvider 빠른 배포 스크립트
# 이 스크립트는 개발/테스트 환경에서 빠른 배포를 위한 것입니다.

Write-Host "IIS FTP SimpleAuthProvider 빠른 배포를 시작합니다..." -ForegroundColor Green

# 현재 디렉토리 확인
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir

Write-Host "프로젝트 루트: $projectRoot" -ForegroundColor Cyan

# 프로젝트 빌드
Write-Host "`n프로젝트를 빌드하는 중..." -ForegroundColor Yellow
Set-Location $projectRoot

try {
    dotnet build --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "빌드에 실패했습니다."
        exit 1
    }
    Write-Host "✓ 빌드가 완료되었습니다." -ForegroundColor Green
} catch {
    Write-Error "빌드 중 오류가 발생했습니다: $($_.Exception.Message)"
    exit 1
}

# 메인 배포 스크립트 실행
Write-Host "`nIIS에 배포하는 중..." -ForegroundColor Yellow
$deployScript = Join-Path $scriptDir "deploy-to-iis.ps1"

if (Test-Path $deployScript) {
    try {
        & $deployScript -CreateAppPool -CreateSite
        Write-Host "✓ 배포가 완료되었습니다!" -ForegroundColor Green
    } catch {
        Write-Error "배포 중 오류가 발생했습니다: $($_.Exception.Message)"
        exit 1
    }
} else {
    Write-Error "배포 스크립트를 찾을 수 없습니다: $deployScript"
    exit 1
}

# 배포 상태 확인
Write-Host "`n배포 상태를 확인하는 중..." -ForegroundColor Yellow
$statusScript = Join-Path $scriptDir "check-deployment-status.ps1"

if (Test-Path $statusScript) {
    & $statusScript
} else {
    Write-Warning "상태 확인 스크립트를 찾을 수 없습니다."
}

Write-Host "`n🎉 빠른 배포가 완료되었습니다!" -ForegroundColor Green
Write-Host "웹 관리 콘솔: http://localhost:8080" -ForegroundColor Cyan
Write-Host "기본 관리자 계정: admin / admin123" -ForegroundColor Cyan
