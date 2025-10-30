# Local Development Environment Variables Test Script
# This script helps you test environment variable configuration locally

Write-Host "🧪 Testing Environment Variable Configuration" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Gray

# Check if .env file exists
if (Test-Path ".env") {
    Write-Host "✅ Found .env file" -ForegroundColor Green
    Write-Host "Loading environment variables from .env..." -ForegroundColor Yellow
    
    # Load .env file
    Get-Content .env | ForEach-Object {
        if ($_ -match '^([^#][^=]+)=(.*)$') {
            $name = $matches[1].Trim()
            $value = $matches[2].Trim()
            [Environment]::SetEnvironmentVariable($name, $value, "Process")
            Write-Host "  Set: $name" -ForegroundColor Gray
        }
    }
}
else {
    Write-Host "⚠️  No .env file found. Using default appsettings.json" -ForegroundColor Yellow
    Write-Host "Copy .env.example to .env and configure it for local development" -ForegroundColor Gray
}

Write-Host "`n📋 Configuration Status:" -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Gray

# Check Database
$dbConnString = [Environment]::GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
$dbHost = [Environment]::GetEnvironmentVariable("DATABASE_HOST")
if ($dbConnString) {
    Write-Host "✅ Database: Using DATABASE_CONNECTION_STRING" -ForegroundColor Green
}
elseif ($dbHost) {
    Write-Host "✅ Database: Using individual DATABASE_* variables" -ForegroundColor Green
    Write-Host "   Host: $dbHost" -ForegroundColor Gray
    Write-Host "   Port: $([Environment]::GetEnvironmentVariable('DATABASE_PORT'))" -ForegroundColor Gray
    Write-Host "   Name: $([Environment]::GetEnvironmentVariable('DATABASE_NAME'))" -ForegroundColor Gray
}
else {
    Write-Host "⚠️  Database: Will use appsettings.json" -ForegroundColor Yellow
}

# Check JWT
$jwtSecret = [Environment]::GetEnvironmentVariable("JWT_SECRET_KEY")
if ($jwtSecret) {
    $length = $jwtSecret.Length
    if ($length -ge 32) {
        Write-Host "✅ JWT: Secret key configured ($length characters)" -ForegroundColor Green
    }
    else {
        Write-Host "❌ JWT: Secret key too short ($length characters, need 32+)" -ForegroundColor Red
    }
}
else {
    Write-Host "⚠️  JWT: Will use appsettings.json" -ForegroundColor Yellow
}

# Check Google OAuth
$googleClientId = [Environment]::GetEnvironmentVariable("GOOGLE_CLIENT_ID")
$googleSecret = [Environment]::GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
if ($googleClientId -and $googleSecret) {
    Write-Host "✅ Google OAuth: Configured" -ForegroundColor Green
}
else {
    Write-Host "⚠️  Google OAuth: Not configured (will use appsettings.json)" -ForegroundColor Yellow
}

# Check Email
$emailProvider = [Environment]::GetEnvironmentVariable("EMAIL_PROVIDER")
if ($emailProvider) {
    Write-Host "✅ Email: Provider = $emailProvider" -ForegroundColor Green
}
else {
    Write-Host "⚠️  Email: Will use appsettings.json" -ForegroundColor Yellow
}

# Check Razorpay
$razorpayKey = [Environment]::GetEnvironmentVariable("RAZORPAY_KEY_ID")
if ($razorpayKey) {
    Write-Host "✅ Razorpay: Configured" -ForegroundColor Green
}
else {
    Write-Host "⚠️  Razorpay: Not configured (optional)" -ForegroundColor Yellow
}

Write-Host "`n🚀 Starting Application..." -ForegroundColor Cyan
Write-Host "=" * 60 -ForegroundColor Gray

# Change to API directory and run
Set-Location src\ActivoosCRM.API
dotnet run

# Clean up process environment variables
Write-Host "`n🧹 Cleaning up..." -ForegroundColor Gray
