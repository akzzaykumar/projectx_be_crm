# Azure App Service - Quick Setup Script
# Run this script to set environment variables in Azure App Service

# Configuration
$RESOURCE_GROUP = "rg-activooscrm"
$APP_NAME = "activooscrm-api"

# Check if logged in to Azure
Write-Host "Checking Azure login..." -ForegroundColor Cyan
az account show | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "Please login to Azure first: az login" -ForegroundColor Red
    exit 1
}

Write-Host "Setting environment variables in Azure App Service..." -ForegroundColor Green

# Required: Database Connection
Write-Host "Enter Database Connection String:" -ForegroundColor Yellow
Write-Host "Example: Host=your-server.postgres.database.azure.com;Port=5432;Database=activoos_crm;Username=adminuser;Password=YourPassword;SslMode=Require" -ForegroundColor Gray
$DATABASE_CONNECTION_STRING = Read-Host "DATABASE_CONNECTION_STRING"

# Required: JWT Configuration
Write-Host "`nEnter JWT Secret Key (minimum 32 characters):" -ForegroundColor Yellow
$JWT_SECRET_KEY = Read-Host "JWT_SECRET_KEY" -AsSecureString
$JWT_SECRET_KEY_PLAIN = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($JWT_SECRET_KEY))

# Required: Google OAuth
Write-Host "`nEnter Google OAuth Client ID:" -ForegroundColor Yellow
$GOOGLE_CLIENT_ID = Read-Host "GOOGLE_CLIENT_ID"

Write-Host "Enter Google OAuth Client Secret:" -ForegroundColor Yellow
$GOOGLE_CLIENT_SECRET = Read-Host "GOOGLE_CLIENT_SECRET" -AsSecureString
$GOOGLE_CLIENT_SECRET_PLAIN = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($GOOGLE_CLIENT_SECRET))

# Optional: Email Configuration
Write-Host "`nConfigure Email? (y/n):" -ForegroundColor Yellow
$configureEmail = Read-Host
$emailSettings = ""

if ($configureEmail -eq "y") {
    Write-Host "Enter SMTP Host (e.g., smtp.gmail.com):" -ForegroundColor Yellow
    $SMTP_HOST = Read-Host "SMTP_HOST"
    
    Write-Host "Enter SMTP Port (e.g., 587):" -ForegroundColor Yellow
    $SMTP_PORT = Read-Host "SMTP_PORT"
    
    Write-Host "Enter SMTP Username:" -ForegroundColor Yellow
    $SMTP_USERNAME = Read-Host "SMTP_USERNAME"
    
    Write-Host "Enter SMTP Password:" -ForegroundColor Yellow
    $SMTP_PASSWORD = Read-Host "SMTP_PASSWORD" -AsSecureString
    $SMTP_PASSWORD_PLAIN = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SMTP_PASSWORD))
    
    $emailSettings = @"
    EMAIL_PROVIDER=SMTP `
    SMTP_HOST=$SMTP_HOST `
    SMTP_PORT=$SMTP_PORT `
    SMTP_ENABLE_SSL=true `
    SMTP_USERNAME=$SMTP_USERNAME `
    SMTP_PASSWORD=$SMTP_PASSWORD_PLAIN `
    EMAIL_FROM_EMAIL=noreply@activooscrm.com `
    EMAIL_FROM_NAME=ActivoosCRM `
"@
}

# Frontend URL for CORS
Write-Host "`nEnter Frontend URL for CORS (e.g., https://your-frontend.com):" -ForegroundColor Yellow
$CORS_ORIGINS = Read-Host "CORS_ORIGINS"

Write-Host "`nApplying configuration to Azure App Service..." -ForegroundColor Cyan

# Apply settings
az webapp config appsettings set `
    --name $APP_NAME `
    --resource-group $RESOURCE_GROUP `
    --settings `
    "DATABASE_CONNECTION_STRING=$DATABASE_CONNECTION_STRING" `
    "JWT_SECRET_KEY=$JWT_SECRET_KEY_PLAIN" `
    "JWT_ISSUER=ActivoosCRM" `
    "JWT_AUDIENCE=ActivoosCRM" `
    "GOOGLE_CLIENT_ID=$GOOGLE_CLIENT_ID" `
    "GOOGLE_CLIENT_SECRET=$GOOGLE_CLIENT_SECRET_PLAIN" `
    "ASPNETCORE_ENVIRONMENT=Production" `
    "ASPNETCORE_URLS=http://0.0.0.0:8080" `
    "CORS_ORIGINS=$CORS_ORIGINS" `
    $emailSettings

if ($LASTEXITCODE -eq 0) {
    Write-Host "`n✅ Configuration applied successfully!" -ForegroundColor Green
    Write-Host "`nNext steps:" -ForegroundColor Cyan
    Write-Host "1. Deploy your application: dotnet publish and upload to Azure" -ForegroundColor White
    Write-Host "2. Restart the app service: az webapp restart --name $APP_NAME --resource-group $RESOURCE_GROUP" -ForegroundColor White
    Write-Host "3. Monitor logs: az webapp log tail --name $APP_NAME --resource-group $RESOURCE_GROUP" -ForegroundColor White
    Write-Host "4. Test health endpoint: https://$APP_NAME.azurewebsites.net/api/health" -ForegroundColor White
}
else {
    Write-Host "`n❌ Failed to apply configuration. Check errors above." -ForegroundColor Red
}
