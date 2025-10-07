# restore.ps1 - Restore corrupted files from backup
$ErrorActionPreference = "Stop"
$projectRoot = "c:\Users\rkodakalla\CascadeProjects\TestAutomationApp.NET"

Write-Host "`n🔧 Restoring files from backup..." -ForegroundColor Yellow

$backupDir = Join-Path $projectRoot "backups_20251007_091600"
$sourceFile = Join-Path $backupDir "TestAutomationApp.API\Services\TestGeneratorService.cs"
$destFile = Join-Path $projectRoot "TestAutomationApp.API\Services\TestGeneratorService.cs"

Copy-Item $sourceFile $destFile -Force
Write-Host "✅ Restored TestGeneratorService.cs" -ForegroundColor Green

Write-Host "`n✅ Restoration complete!" -ForegroundColor Green
Write-Host "Now rebuilding with dotnet build..." -ForegroundColor Cyan

dotnet build