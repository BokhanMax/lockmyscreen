# Собирает программу и установщик: dist\LockMyScreen-Setup-<версия>.exe
# Запуск: powershell -ExecutionPolicy Bypass -File build-installer.ps1

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host '== Сборка программы ==' -ForegroundColor Cyan
dotnet build LockMyScreen.csproj -c Release -nologo
if ($LASTEXITCODE -ne 0) { throw 'Сборка программы не удалась' }

$iscc = @(
    "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
) | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $iscc) { throw 'Inno Setup 6 не найден. Установите: winget install JRSoftware.InnoSetup' }

Write-Host '== Сборка установщика ==' -ForegroundColor Cyan
& $iscc /Q installer\LockMyScreen.iss
if ($LASTEXITCODE -ne 0) { throw 'Сборка установщика не удалась' }

Get-ChildItem dist\*.exe | Sort-Object LastWriteTime | Select-Object -Last 1 |
    ForEach-Object { Write-Host "Готово: $($_.FullName) ($([math]::Round($_.Length / 1KB)) КБ)" -ForegroundColor Green }
