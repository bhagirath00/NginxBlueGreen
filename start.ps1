# Automation Script for Blue-Green Deployment Proof

Write-Host "--- 🚀 Starting Setup ---" -ForegroundColor Cyan

# 1. Build
Write-Host "1. Building Project..." -ForegroundColor Gray
dotnet publish src/BlueGreenApp.csproj -c Release -o ./publish

# 2. Folders
Write-Host "2. Preparing Folders..." -ForegroundColor Gray
if (!(Test-Path "./www/green")) { New-Item -ItemType Directory -Path "./www/green" -Force | Out-Null }
if (!(Test-Path "./www/blue")) { New-Item -ItemType Directory -Path "./www/blue" -Force | Out-Null }
Copy-Item ./publish/* ./www/green/ -Recurse -Force

# 3. Start
Write-Host "--- ✅ Setup Complete ---" -ForegroundColor Green
Write-Host "Starting Primary Server on Port 2001..." -ForegroundColor Green
Write-Host "Visit: http://localhost:2001" -ForegroundColor Yellow

$env:APP_COLOR="Green"
$env:ASPNETCORE_URLS="http://localhost:2001"
dotnet ./www/green/BlueGreenApp.dll
