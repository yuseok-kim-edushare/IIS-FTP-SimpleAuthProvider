# PowerShell 실행 정책 설정 스크립트
# 이 스크립트는 배포 스크립트 실행을 위해 필요한 실행 정책을 설정합니다.

Write-Host "PowerShell 실행 정책을 설정합니다..." -ForegroundColor Green

# 관리자 권한 확인
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "이 스크립트는 관리자 권한으로 실행해야 합니다."
    Write-Host "PowerShell을 관리자 권한으로 다시 실행하세요." -ForegroundColor Yellow
    exit 1
}

# 현재 실행 정책 확인
Write-Host "`n현재 실행 정책:" -ForegroundColor Cyan
$currentPolicy = Get-ExecutionPolicy
Write-Host "  시스템: $currentPolicy" -ForegroundColor Yellow

$currentUserPolicy = Get-ExecutionPolicy -Scope CurrentUser
Write-Host "  현재 사용자: $currentUserPolicy" -ForegroundColor Yellow

$localMachinePolicy = Get-ExecutionPolicy -Scope LocalMachine
Write-Host "  로컬 머신: $localMachinePolicy" -ForegroundColor Yellow

# 권장 정책 설정
Write-Host "`n권장 실행 정책을 설정합니다..." -ForegroundColor Cyan

try {
    # 현재 사용자에 대해 RemoteSigned 정책 설정
    Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force
    Write-Host "? 현재 사용자 실행 정책이 RemoteSigned로 설정되었습니다." -ForegroundColor Green
    
    # 로컬 머신에 대해 RemoteSigned 정책 설정 (선택사항)
    $response = Read-Host "`n로컬 머신 전체에 대해서도 RemoteSigned 정책을 설정하시겠습니까? (y/n)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope LocalMachine -Force
        Write-Host "? 로컬 머신 실행 정책이 RemoteSigned로 설정되었습니다." -ForegroundColor Green
    }
    
} catch {
    Write-Error "실행 정책 설정 중 오류가 발생했습니다: $($_.Exception.Message)"
    exit 1
}

# 설정 후 정책 확인
Write-Host "`n설정 후 실행 정책:" -ForegroundColor Cyan
$newCurrentUserPolicy = Get-ExecutionPolicy -Scope CurrentUser
Write-Host "  현재 사용자: $newCurrentUserPolicy" -ForegroundColor Green

$newLocalMachinePolicy = Get-ExecutionPolicy -Scope LocalMachine
Write-Host "  로컬 머신: $newLocalMachinePolicy" -ForegroundColor Green

# 정책 설명
Write-Host "`n실행 정책 설명:" -ForegroundColor Cyan
Write-Host "  RemoteSigned: 로컬 스크립트는 서명 없이 실행, 원격 스크립트는 신뢰할 수 있는 서명 필요" -ForegroundColor Gray
Write-Host "  이 정책은 보안과 편의성의 균형을 제공합니다." -ForegroundColor Gray

Write-Host "`n? 실행 정책 설정이 완료되었습니다!" -ForegroundColor Green
Write-Host "이제 배포 스크립트를 실행할 수 있습니다." -ForegroundColor Cyan
