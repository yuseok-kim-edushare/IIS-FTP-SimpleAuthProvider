# IIS FTP SimpleAuthProvider ���� ���� ��ũ��Ʈ
# �� ��ũ��Ʈ�� ����/�׽�Ʈ ȯ�濡�� ���� ������ ���� ���Դϴ�.

Write-Host "IIS FTP SimpleAuthProvider ���� ������ �����մϴ�..." -ForegroundColor Green

# ������Ʈ ��Ʈ Ȯ��
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectRoot = Split-Path -Parent $scriptDir

Write-Host "������Ʈ ��Ʈ: $projectRoot" -ForegroundColor Cyan

# MSBuild ��� Ȯ��
Write-Host "`nMSBuild�� Ȯ���ϴ� ��..." -ForegroundColor Yellow
$msbuildPaths = @(
    "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe",
    "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
)

$msbuildPath = $null
foreach ($path in $msbuildPaths) {
    if (Test-Path $path) {
        $msbuildPath = $path
        break
    }
}

if (-not $msbuildPath) {
    Write-Error "MSBuild.exe�� ã�� �� �����ϴ�. Visual Studio Build Tools �Ǵ� Visual Studio�� ��ġ���ּ���."
    Write-Error "�ٿ�ε�: https://visualstudio.microsoft.com/downloads/#build-tools-for-visual-studio-2022"
    exit 1
}

Write-Host "MSBuild ���: $msbuildPath" -ForegroundColor Green

# ������Ʈ ����
Write-Host "`n������Ʈ�� �����ϴ� ��..." -ForegroundColor Yellow
Set-Location $projectRoot

try {
    & $msbuildPath "IIS-FTP-SimpleAuthProvider.slnx" "/p:Configuration=Release" "/p:Platform=`"Any CPU`"" "/verbosity:minimal"
    if ($LASTEXITCODE -ne 0) {
        Write-Error "���忡 �����߽��ϴ�."
        exit 1
    }
    Write-Host "? ���尡 �Ϸ�Ǿ����ϴ�." -ForegroundColor Green
} catch {
    Write-Error "���� �� ������ �߻��߽��ϴ�: $($_.Exception.Message)"
    exit 1
}

# IIS ���� ��ũ��Ʈ ����
Write-Host "`nIIS�� �����ϴ� ��..." -ForegroundColor Yellow
$deployScript = Join-Path $scriptDir "deploy-to-iis.ps1"

if (Test-Path $deployScript) {
    try {
        & $deployScript -CreateAppPool -CreateSite
        Write-Host "? ������ �Ϸ�Ǿ����ϴ�!" -ForegroundColor Green
    } catch {
        Write-Error "���� �� ������ �߻��߽��ϴ�: $($_.Exception.Message)"
        exit 1
    }
} else {
    Write-Error "���� ��ũ��Ʈ�� ã�� �� �����ϴ�: $deployScript"
    exit 1
}

# ���� ���� Ȯ��
Write-Host "`n���� ���¸� Ȯ���ϴ� ��..." -ForegroundColor Yellow
$statusScript = Join-Path $scriptDir "check-deployment-status.ps1"

if (Test-Path $statusScript) {
    & $statusScript
} else {
    Write-Warning "���� Ȯ�� ��ũ��Ʈ�� ã�� �� �����ϴ�."
}

Write-Host "`n? ���� ������ �Ϸ�Ǿ����ϴ�!" -ForegroundColor Green
Write-Host "�� ���� �ܼ�: http://localhost:8080" -ForegroundColor Cyan
Write-Host "�⺻ ������ ����: admin / admin123" -ForegroundColor Cyan
