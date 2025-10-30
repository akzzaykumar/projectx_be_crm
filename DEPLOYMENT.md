# Deployment Guide - Azure App Service

This guide explains how to deploy the ActivoosCRM API to Azure App Service using environment variables.

## Prerequisites

- Azure Account with an active subscription
- Azure CLI installed (or use Azure Portal)
- PostgreSQL Database (Azure Database for PostgreSQL or external)

## Environment Variables Configuration

The application is configured to read from environment variables first, then fall back to `appsettings.json`. This makes it ideal for cloud deployment.

### Required Environment Variables

#### Database Configuration

**Option 1: Full Connection String (Recommended)**
```bash
DATABASE_CONNECTION_STRING=Host=your-server.postgres.database.azure.com;Port=5432;Database=activoos_crm;Username=adminuser@your-server;Password=YourPassword;SslMode=Require
```

**Option 2: Individual Components**
```bash
DATABASE_HOST=your-server.postgres.database.azure.com
DATABASE_PORT=5432
DATABASE_NAME=activoos_crm
DATABASE_USER=adminuser@your-server
DATABASE_PASSWORD=YourSecurePassword
```

#### JWT Authentication (Required)
```bash
JWT_SECRET_KEY=YourSuperSecretKeyMinimum32CharactersLongForProduction123456789
JWT_ISSUER=ActivoosCRM
JWT_AUDIENCE=ActivoosCRM
```

#### Google OAuth (Required for Google Sign-In)
```bash
GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-client-secret
```

#### Email Configuration (Optional - for email features)
```bash
EMAIL_PROVIDER=SMTP
EMAIL_BASE_URL=https://your-app.azurewebsites.net
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_ENABLE_SSL=true
SMTP_USERNAME=your-email@gmail.com
SMTP_PASSWORD=your-app-password
EMAIL_FROM_EMAIL=noreply@yourdomain.com
EMAIL_FROM_NAME=ActivoosCRM
```

#### Payment Gateway - Razorpay (Optional)
```bash
RAZORPAY_KEY_ID=rzp_live_xxxxxxxxxx
RAZORPAY_KEY_SECRET=your-razorpay-secret-key
RAZORPAY_WEBHOOK_SECRET=your-webhook-secret
PAYMENT_GATEWAY_CALLBACK_URL=https://your-app.azurewebsites.net/api/bookings/webhook/payment
PAYMENT_GATEWAY_SUCCESS_URL=https://your-frontend.com/booking/success
PAYMENT_GATEWAY_FAILURE_URL=https://your-frontend.com/booking/failure
```

#### Application Settings
```bash
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://0.0.0.0:8080
CORS_ORIGINS=https://your-frontend.com
```

## Azure App Service Deployment

### Step 1: Create Azure Resources

#### 1.1 Create Resource Group
```bash
az group create --name rg-activooscrm --location eastus
```

#### 1.2 Create Azure Database for PostgreSQL
```bash
az postgres flexible-server create \
  --resource-group rg-activooscrm \
  --name activooscrm-db \
  --location eastus \
  --admin-user adminuser \
  --admin-password YourSecurePassword123! \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --version 16 \
  --storage-size 32
```

#### 1.3 Configure Firewall (Allow Azure Services)
```bash
az postgres flexible-server firewall-rule create \
  --resource-group rg-activooscrm \
  --name activooscrm-db \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0
```

#### 1.4 Create Database
```bash
az postgres flexible-server db create \
  --resource-group rg-activooscrm \
  --server-name activooscrm-db \
  --database-name activoos_crm
```

#### 1.5 Create App Service Plan
```bash
az appservice plan create \
  --name plan-activooscrm \
  --resource-group rg-activooscrm \
  --sku B1 \
  --is-linux
```

#### 1.6 Create Web App
```bash
az webapp create \
  --name activooscrm-api \
  --resource-group rg-activooscrm \
  --plan plan-activooscrm \
  --runtime "DOTNETCORE:8.0"
```

### Step 2: Configure Environment Variables in Azure

#### Using Azure CLI
```bash
# Database
az webapp config appsettings set \
  --name activooscrm-api \
  --resource-group rg-activooscrm \
  --settings \
    DATABASE_CONNECTION_STRING="Host=activooscrm-db.postgres.database.azure.com;Port=5432;Database=activoos_crm;Username=adminuser;Password=YourSecurePassword123!;SslMode=Require" \
    JWT_SECRET_KEY="YourSuperSecretKeyMinimum32CharactersLongForProduction123456789" \
    JWT_ISSUER="ActivoosCRM" \
    JWT_AUDIENCE="ActivoosCRM" \
    GOOGLE_CLIENT_ID="your-client-id.apps.googleusercontent.com" \
    GOOGLE_CLIENT_SECRET="your-client-secret" \
    ASPNETCORE_ENVIRONMENT="Production" \
    ASPNETCORE_URLS="http://0.0.0.0:8080" \
    CORS_ORIGINS="https://your-frontend.com"
```

#### Using Azure Portal
1. Navigate to your App Service
2. Go to **Configuration** → **Application settings**
3. Click **+ New application setting** for each variable
4. Add all required environment variables from the list above
5. Click **Save** at the top

### Step 3: Deploy Application

#### Option A: Using Azure CLI
```bash
# From project root directory
cd c:\workspace\funbookr_be\funbooker_be_crm

# Build and publish
dotnet publish src/ActivoosCRM.API/ActivoosCRM.API.csproj -c Release -o ./publish

# Create zip file
Compress-Archive -Path ./publish/* -DestinationPath ./app.zip -Force

# Deploy to Azure
az webapp deployment source config-zip \
  --name activooscrm-api \
  --resource-group rg-activooscrm \
  --src ./app.zip
```

#### Option B: Using GitHub Actions (CI/CD)
Create `.github/workflows/azure-deploy.yml`:

```yaml
name: Deploy to Azure App Service

on:
  push:
    branches: [ main ]

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release --no-restore
    
    - name: Publish
      run: dotnet publish src/ActivoosCRM.API/ActivoosCRM.API.csproj -c Release -o ./publish
    
    - name: Deploy to Azure
      uses: azure/webapps-deploy@v2
      with:
        app-name: 'activooscrm-api'
        publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
        package: ./publish
```

### Step 4: Verify Deployment

1. Check application logs:
```bash
az webapp log tail --name activooscrm-api --resource-group rg-activooscrm
```

2. Test health endpoint:
```bash
curl https://activooscrm-api.azurewebsites.net/api/health
```

3. Access Swagger UI:
```
https://activooscrm-api.azurewebsites.net/swagger
```

## Database Migrations

The application automatically applies pending migrations on startup. To manually run migrations:

```bash
# From your local machine with connection to Azure DB
dotnet ef database update \
  --project src/ActivoosCRM.Infrastructure \
  --startup-project src/ActivoosCRM.API \
  --connection "Host=activooscrm-db.postgres.database.azure.com;Port=5432;Database=activoos_crm;Username=adminuser;Password=YourSecurePassword123!;SslMode=Require"
```

## Security Checklist

- [ ] Change all default passwords
- [ ] Use strong JWT secret key (minimum 32 characters)
- [ ] Enable SSL/TLS for database connections
- [ ] Configure CORS to allow only your frontend domain
- [ ] Store sensitive values in Azure Key Vault (optional, advanced)
- [ ] Enable Application Insights for monitoring
- [ ] Configure custom domain with SSL certificate
- [ ] Set up firewall rules to restrict database access

## Monitoring

### Enable Application Insights
```bash
az monitor app-insights component create \
  --app activooscrm-insights \
  --location eastus \
  --resource-group rg-activooscrm \
  --application-type web

# Link to App Service
az webapp config appsettings set \
  --name activooscrm-api \
  --resource-group rg-activooscrm \
  --settings APPLICATIONINSIGHTS_CONNECTION_STRING="<connection-string>"
```

## Troubleshooting

### View Logs
```bash
# Stream logs
az webapp log tail --name activooscrm-api --resource-group rg-activooscrm

# Download logs
az webapp log download --name activooscrm-api --resource-group rg-activooscrm
```

### Common Issues

1. **Database Connection Failed**
   - Verify connection string is correct
   - Check firewall rules allow App Service IP
   - Ensure SSL mode is set correctly

2. **Application Won't Start**
   - Check environment variables are set
   - Review application logs
   - Verify .NET runtime version matches

3. **Google OAuth Not Working**
   - Add redirect URI in Google Console: `https://your-app.azurewebsites.net/signin-google`
   - Verify CLIENT_ID and CLIENT_SECRET are correct

## Cost Optimization

- Use **B1** tier for development/testing
- Scale to **S1** or higher for production
- Enable **Auto-scaling** based on CPU/Memory
- Use **PostgreSQL Burstable** tier for lower costs
- Enable **Application Insights sampling** to reduce costs

## Support

For issues or questions:
- Check application logs first
- Review Azure service health dashboard
- Contact support team

---

**Last Updated:** October 31, 2025
