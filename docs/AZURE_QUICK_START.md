# 🚀 Azure App Service - Quick Reference

## Minimum Required Environment Variables

Copy these to Azure Portal → App Service → Configuration → Application Settings:

| Variable | Example Value | Required |
|----------|---------------|----------|
| `DATABASE_CONNECTION_STRING` | `Host=mydb.postgres.database.azure.com;Port=5432;Database=activoos_crm;Username=admin;Password=***;SslMode=Require` | ✅ Yes |
| `JWT_SECRET_KEY` | `YourSuperSecretKey32CharactersMinimum!!!` | ✅ Yes |
| `JWT_ISSUER` | `ActivoosCRM` | ✅ Yes |
| `JWT_AUDIENCE` | `ActivoosCRM` | ✅ Yes |
| `GOOGLE_CLIENT_ID` | `123456.apps.googleusercontent.com` | ⚠️ If using Google auth |
| `GOOGLE_CLIENT_SECRET` | `GOCSPX-***` | ⚠️ If using Google auth |
| `ASPNETCORE_ENVIRONMENT` | `Production` | ✅ Yes |
| `ASPNETCORE_URLS` | `http://0.0.0.0:8080` | ✅ Yes |
| `CORS_ORIGINS` | `https://your-frontend.com` | ✅ Yes |

## Azure CLI - One Command Setup

```bash
az webapp config appsettings set \
  --name YOUR-APP-NAME \
  --resource-group YOUR-RESOURCE-GROUP \
  --settings \
    DATABASE_CONNECTION_STRING="Host=your-db.postgres.database.azure.com;Port=5432;Database=activoos_crm;Username=admin;Password=YOUR_PASSWORD;SslMode=Require" \
    JWT_SECRET_KEY="YourSuperSecretKey32CharactersMinimumRequired!!!" \
    JWT_ISSUER="ActivoosCRM" \
    JWT_AUDIENCE="ActivoosCRM" \
    GOOGLE_CLIENT_ID="your-id.apps.googleusercontent.com" \
    GOOGLE_CLIENT_SECRET="your-secret" \
    ASPNETCORE_ENVIRONMENT="Production" \
    ASPNETCORE_URLS="http://0.0.0.0:8080" \
    CORS_ORIGINS="https://your-frontend.com"
```

## Deploy Code

```bash
# Build and publish
dotnet publish src/ActivoosCRM.API/ActivoosCRM.API.csproj -c Release -o ./publish

# Create deployment package
Compress-Archive -Path ./publish/* -DestinationPath ./app.zip -Force

# Deploy to Azure
az webapp deployment source config-zip \
  --name YOUR-APP-NAME \
  --resource-group YOUR-RESOURCE-GROUP \
  --src ./app.zip
```

## Verify Deployment

```bash
# Check health
curl https://YOUR-APP-NAME.azurewebsites.net/api/health

# View Swagger
# Open: https://YOUR-APP-NAME.azurewebsites.net/swagger

# Stream logs
az webapp log tail --name YOUR-APP-NAME --resource-group YOUR-RESOURCE-GROUP
```

## Common Issues

### ❌ Error: "JWT Secret not configured"
Add `JWT_SECRET_KEY` to App Settings (min 32 characters)

### ❌ Error: "Database connection failed"
- Check `DATABASE_CONNECTION_STRING` is correct
- Verify firewall allows Azure services
- Add SSL: `SslMode=Require` at end of connection string

### ❌ Error: "Application failed to start"
Run: `az webapp log tail` to see detailed error logs

## Security Checklist

- [ ] Changed all default passwords
- [ ] JWT secret is 32+ characters
- [ ] Using `SslMode=Require` for database
- [ ] CORS restricted to your domain only
- [ ] Firewall rules configured
- [ ] Application Insights enabled

## Quick Links

- [Full Deployment Guide](../DEPLOYMENT.md)
- [Environment Variables Guide](./ENVIRONMENT_VARIABLES.md)
- [Setup Script](../scripts/azure-setup-env.ps1)

---

💡 **Tip**: Use the interactive setup script for easier configuration:
```powershell
.\scripts\azure-setup-env.ps1
```
