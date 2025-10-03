# Test Automation Platform - Startup Script
# This script starts both the API and Web projects

Write-Host "🚀 Starting Test Automation Platform..." -ForegroundColor Cyan
Write-Host ""

# Check if .NET is installed
try {
    $dotnetVersion = dotnet --version
    Write-Host "✓ .NET SDK detected: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "✗ .NET SDK not found. Please install .NET 8.0 SDK" -ForegroundColor Red
    Write-Host "  Download from: https://dotnet.microsoft.com/download" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --nologo --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "✗ Build failed. Please check for errors." -ForegroundColor Red
    exit 1
}

Write-Host "✓ Build successful" -ForegroundColor Green
Write-Host ""

# Start API in background
Write-Host "Starting API (Backend)..." -ForegroundColor Yellow
$apiJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    cd TestAutomationApp.API
    dotnet run --no-build
}

# Wait a bit for API to start
Start-Sleep -Seconds 3

# Start Web in background
Write-Host "Starting Web (Frontend)..." -ForegroundColor Yellow
$webJob = Start-Job -ScriptBlock {
    Set-Location $using:PWD
    cd TestAutomationApp.Web
    dotnet run --no-build
}

# Wait for both to be ready
Start-Sleep -Seconds 5

Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host "  ✓ Test Automation Platform is running!" -ForegroundColor Green
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "  🌐 Web Application:  http://localhost:5000" -ForegroundColor White
Write-Host "  🔌 API Backend:      http://localhost:5001" -ForegroundColor White
Write-Host "  📚 API Docs:         http://localhost:5001/swagger" -ForegroundColor White
Write-Host ""
Write-Host "═══════════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Opening browser..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
Start-Process "http://localhost:5000"

Write-Host ""
Write-Host "Press Ctrl+C to stop both servers" -ForegroundColor Yellow
Write-Host ""

# Keep script running and show output
try {
    while ($true) {
        # Check if jobs are still running
        if ($apiJob.State -ne 'Running' -or $webJob.State -ne 'Running') {
            Write-Host "One or more services stopped unexpectedly" -ForegroundColor Red
            break
        }
        
        # Show any output from jobs
        Receive-Job -Job $apiJob -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host "[API] $_" -ForegroundColor DarkGray
        }
        Receive-Job -Job $webJob -ErrorAction SilentlyContinue | ForEach-Object {
            Write-Host "[WEB] $_" -ForegroundColor DarkGray
        }
        
        Start-Sleep -Seconds 1
    }
} finally {
    Write-Host ""
    Write-Host "Stopping services..." -ForegroundColor Yellow
    Stop-Job -Job $apiJob, $webJob
    Remove-Job -Job $apiJob, $webJob
    Write-Host "✓ All services stopped" -ForegroundColor Green
}
