# Environment Variables Configuration - Summary

## ✅ What Was Changed

The application has been configured to use **environment variables** as the primary configuration source, with automatic fallback to `appsettings.json`. This makes it cloud-ready and ideal for Azure App Service deployment.

## 🔧 Modified Files

### 1. **Program.cs**
- Added `builder.Configuration.AddEnvironmentVariables()` to read environment variables
- Updated JWT configuration to read from environment variables first
- Updated Google OAuth to read from environment variables first
- Added configuration validation on startup

### 2. **DependencyInjection.cs** (Infrastructure)
- Added support for `DATABASE_CONNECTION_STRING` environment variable
- Added automatic connection string builder from individual `DATABASE_*` variables
- Environment variables take precedence over appsettings

### 3. **ConfigurationValidator.cs** (New File)
- Validates all required configuration on startup
- Provides clear error messages if configuration is missing
- Warns about optional configurations (like Google OAuth)

### 4. **.env.example**
- Updated with comprehensive environment variable documentation
- Organized into logical sections
- Added Azure-specific guidance

### 5. **DEPLOYMENT.md** (New File)
- Complete Azure App Service deployment guide
- Step-by-step instructions with Azure CLI commands
- Environment variable configuration examples
- Troubleshooting section

### 6. **Scripts**
- `azure-setup-env.ps1`: Interactive script to configure Azure App Service
- `run-with-env.ps1`: Local testing script with environment variable support

## 📝 Configuration Priority

The application reads configuration in this order:

1. **Environment Variables** (Highest Priority) ← Azure App Service uses this
2. **appsettings.{Environment}.json**
3. **appsettings.json** (Lowest Priority)

## 🚀 For Azure App Service Deployment

### Required Environment Variables

Set these in Azure Portal → App Service → Configuration → Application Settings:

```bash
# Database (choose one option)
DATABASE_CONNECTION_STRING=Host=your-db.postgres.database.azure.com;Port=5432;Database=activoos_crm;Username=user;Password=pass;SslMode=Require

# OR use individual components
DATABASE_HOST=your-db.postgres.database.azure.com
DATABASE_PORT=5432
DATABASE_NAME=activoos_crm
DATABASE_USER=adminuser
DATABASE_PASSWORD=YourSecurePassword

# JWT (Required)
JWT_SECRET_KEY=YourSuperSecretKey32CharactersMinimum
JWT_ISSUER=ActivoosCRM
JWT_AUDIENCE=ActivoosCRM

# Google OAuth (Required for Google Sign-In)
GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-client-secret

# Application
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
CORS_ORIGINS=https://your-frontend.com
```

### Optional Environment Variables

```bash
# Email
EMAIL_PROVIDER=SMTP
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
EMAIL_FROM_EMAIL=noreply@yourdomain.com

# Razorpay
RAZORPAY_KEY_ID=rzp_live_xxxxxxxxxx
RAZORPAY_KEY_SECRET=your-secret
RAZORPAY_WEBHOOK_SECRET=your-webhook-secret
PAYMENT_GATEWAY_CALLBACK_URL=https://your-app.azurewebsites.net/api/bookings/webhook/payment
```

## 🧪 Local Development

### Option 1: Using .env file (Recommended)

1. Copy `.env.example` to `.env`
2. Fill in your local values
3. Run: `.\scripts\run-with-env.ps1`

### Option 2: Using appsettings.Development.json

1. Keep your settings in `appsettings.Development.json`
2. Run: `dotnet run` (works as before)

### Option 3: Set Windows Environment Variables

```powershell
$env:DATABASE_HOST="localhost"
$env:DATABASE_PORT="5432"
$env:DATABASE_NAME="activoos_crm_dev"
$env:DATABASE_USER="postgres"
$env:DATABASE_PASSWORD="postgres"
$env:JWT_SECRET_KEY="DevelopmentSecretKeyForJWTTokenGeneration123456789"

dotnet run --project src/ActivoosCRM.API
```

## 🔒 Security Notes

1. **Never commit** `.env` file to git (it's in `.gitignore`)
2. Use **strong secrets** in production (minimum 32 characters for JWT)
3. Enable **SslMode=Require** for PostgreSQL in production
4. Store sensitive values in **Azure Key Vault** for enterprise deployments
5. Rotate secrets regularly

## ✨ Benefits

1. **Cloud-Ready**: Works seamlessly with Azure App Service, AWS, Google Cloud
2. **Secure**: Sensitive data never in source code
3. **Flexible**: Easy to change configuration without redeployment
4. **Environment-Specific**: Different settings for dev/staging/prod
5. **CI/CD Friendly**: Perfect for automated deployments
6. **Backward Compatible**: Still works with appsettings.json

## 📚 Quick Links

- [Full Deployment Guide](./DEPLOYMENT.md)
- [Environment Variables Reference](./.env.example)
- [Azure Setup Script](./scripts/azure-setup-env.ps1)
- [Local Testing Script](./scripts/run-with-env.ps1)

## 🆘 Troubleshooting

### Error: "JWT Secret not configured"
**Solution**: Set `JWT_SECRET_KEY` environment variable (minimum 32 characters)

### Error: "Database connection failed"
**Solution**: Set `DATABASE_CONNECTION_STRING` or individual `DATABASE_*` variables

### Error: "Google OAuth not configured"
**Solution**: Set `GOOGLE_CLIENT_ID` and `GOOGLE_CLIENT_SECRET` environment variables

### Application won't start locally
1. Ensure PostgreSQL is running (Docker: `docker-compose up -d`)
2. Check database connection in `.env` or `appsettings.Development.json`
3. Run validation script: `.\scripts\run-with-env.ps1`

---

**Ready for Azure App Service!** 🎉

Follow [DEPLOYMENT.md](./DEPLOYMENT.md) for complete deployment instructions.
