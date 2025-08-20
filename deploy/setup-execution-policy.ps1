# PowerShell ���� ��å ���� ��ũ��Ʈ
# �� ��ũ��Ʈ�� ���� ��ũ��Ʈ ������ ���� �ʿ��� ���� ��å�� �����մϴ�.

Write-Host "PowerShell ���� ��å�� �����մϴ�..." -ForegroundColor Green

# ������ ���� Ȯ��
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "�� ��ũ��Ʈ�� ������ �������� �����ؾ� �մϴ�."
    Write-Host "PowerShell�� ������ �������� �ٽ� �����ϼ���." -ForegroundColor Yellow
    exit 1
}

# ���� ���� ��å Ȯ��
Write-Host "`n���� ���� ��å:" -ForegroundColor Cyan
$currentPolicy = Get-ExecutionPolicy
Write-Host "  �ý���: $currentPolicy" -ForegroundColor Yellow

$currentUserPolicy = Get-ExecutionPolicy -Scope CurrentUser
Write-Host "  ���� �����: $currentUserPolicy" -ForegroundColor Yellow

$localMachinePolicy = Get-ExecutionPolicy -Scope LocalMachine
Write-Host "  ���� �ӽ�: $localMachinePolicy" -ForegroundColor Yellow

# ���� ��å ����
Write-Host "`n���� ���� ��å�� �����մϴ�..." -ForegroundColor Cyan

try {
    # ���� ����ڿ� ���� RemoteSigned ��å ����
    Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser -Force
    Write-Host "? ���� ����� ���� ��å�� RemoteSigned�� �����Ǿ����ϴ�." -ForegroundColor Green
    
    # ���� �ӽſ� ���� RemoteSigned ��å ���� (���û���)
    $response = Read-Host "`n���� �ӽ� ��ü�� ���ؼ��� RemoteSigned ��å�� �����Ͻðڽ��ϱ�? (y/n)"
    if ($response -eq 'y' -or $response -eq 'Y') {
        Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope LocalMachine -Force
        Write-Host "? ���� �ӽ� ���� ��å�� RemoteSigned�� �����Ǿ����ϴ�." -ForegroundColor Green
    }
    
} catch {
    Write-Error "���� ��å ���� �� ������ �߻��߽��ϴ�: $($_.Exception.Message)"
    exit 1
}

# ���� �� ��å Ȯ��
Write-Host "`n���� �� ���� ��å:" -ForegroundColor Cyan
$newCurrentUserPolicy = Get-ExecutionPolicy -Scope CurrentUser
Write-Host "  ���� �����: $newCurrentUserPolicy" -ForegroundColor Green

$newLocalMachinePolicy = Get-ExecutionPolicy -Scope LocalMachine
Write-Host "  ���� �ӽ�: $newLocalMachinePolicy" -ForegroundColor Green

# ��å ����
Write-Host "`n���� ��å ����:" -ForegroundColor Cyan
Write-Host "  RemoteSigned: ���� ��ũ��Ʈ�� ���� ���� ����, ���� ��ũ��Ʈ�� �ŷ��� �� �ִ� ���� �ʿ�" -ForegroundColor Gray
Write-Host "  �� ��å�� ���Ȱ� ���Ǽ��� ������ �����մϴ�." -ForegroundColor Gray

Write-Host "`n? ���� ��å ������ �Ϸ�Ǿ����ϴ�!" -ForegroundColor Green
Write-Host "���� ���� ��ũ��Ʈ�� ������ �� �ֽ��ϴ�." -ForegroundColor Cyan
