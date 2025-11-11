# FunBookr Production Deployment Guide

**Version:** 1.0.0  
**Last Updated:** November 2025  
**Status:** Production Ready

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Environment Setup](#environment-setup)
4. [Database Deployment](#database-deployment)
5. [Application Deployment](#application-deployment)
6. [Post-Deployment Verification](#post-deployment-verification)
7. [Monitoring & Maintenance](#monitoring--maintenance)
8. [Rollback Procedures](#rollback-procedures)

---

## Overview

This guide provides step-by-step instructions for deploying FunBookr with all production features including:

- ✅ Dynamic Pricing Engine
- ✅ Gift Cards & Vouchers
- ✅ Loyalty Rewards Program
- ✅ Provider Subscriptions
- ✅ QR Code Check-in System
- ✅ Recommendation Engine
- ✅ Activity Add-ons Marketplace
- ✅ Customer Photo Gallery
- ✅ Referral Program
- ✅ Weather Integration
- ✅ Advanced Analytics

---

## Prerequisites

### Required Software

```bash
# Database
PostgreSQL 15.x or higher
Redis 7.x (for caching)

# Runtime
.NET 8.0 SDK
Node.js 18.x (for admin dashboard)

# Tools
Docker & Docker Compose (recommended)
Azure CLI (if deploying to Azure)
```

### Required Services

- **Payment Gateway:** Razorpay account configured
- **Email Service:** SMTP server or SendGrid
- **Storage:** Azure Blob Storage or AWS S3
- **Monitoring:** Application Insights or equivalent

### Access Requirements

- Database admin credentials
- Cloud platform access (Azure/AWS)
- Domain name and SSL certificates
- API keys for third-party services

---

## Environment Setup

### 1. Clone Repository

```bash
git clone https://github.com/your-org/funbookr-be.git
cd funbookr-be
```

### 2. Configure Environment Variables

Create `.env` file in project root:

```bash
# Database
DB_HOST=your-db-host
DB_PORT=5432
DB_NAME=funbookr_prod
DB_USER=funbookr_app
DB_PASSWORD=your-secure-password

# Redis
REDIS_CONNECTION=your-redis-host:6379

# Security
JWT_SECRET=your-jwt-secret-min-32-chars
JWT_ISSUER=https://api.funbookr.com
JWT_AUDIENCE=https://funbookr.com
QR_CODE_SECRET=your-qr-secret-min-32-chars
ENCRYPTION_KEY=your-encryption-key

# Razorpay
RAZORPAY_KEY_ID=your_key_id
RAZORPAY_KEY_SECRET=your_key_secret

# Email
SMTP_HOST=smtp.gmail.com
SMTP_PORT=587
SMTP_USER=noreply@funbookr.com
SMTP_PASSWORD=your-smtp-password
FROM_EMAIL=noreply@funbookr.com
FROM_NAME=FunBookr

# Storage
AZURE_STORAGE_CONNECTION=your-azure-storage-connection
AZURE_STORAGE_CONTAINER=funbookr-images

# External APIs
WEATHER_API_KEY=your-weather-api-key
GOOGLE_PLACES_API_KEY=your-google-api-key

# Application
ASPNETCORE_ENVIRONMENT=Production
ALLOWED_ORIGINS=https://funbookr.com,https://www.funbookr.com
API_BASE_URL=https://api.funbookr.com

# Monitoring
APPLICATIONINSIGHTS_CONNECTION_STRING=your-app-insights-connection

# Feature Flags
ENABLE_DYNAMIC_PRICING=true
ENABLE_RECOMMENDATIONS=true
ENABLE_LOYALTY_PROGRAM=true
ENABLE_GIFT_CARDS=true
ENABLE_QR_CHECKIN=true
ENABLE_WEATHER_CANCELLATION=true
```

### 3. Update appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "${DB_CONNECTION_STRING}",
    "Redis": "${REDIS_CONNECTION}"
  },
  "JwtSettings": {
    "Secret": "${JWT_SECRET}",
    "Issuer": "${JWT_ISSUER}",
    "Audience": "${JWT_AUDIENCE}",
    "ExpiryMinutes": 1440,
    "RefreshTokenExpiryDays": 7
  },
  "Security": {
    "QRCodeSecret": "${QR_CODE_SECRET}",
    "EncryptionKey": "${ENCRYPTION_KEY}"
  },
  "RazorpaySettings": {
    "KeyId": "${RAZORPAY_KEY_ID}",
    "KeySecret": "${RAZORPAY_KEY_SECRET}",
    "WebhookSecret": "${RAZORPAY_WEBHOOK_SECRET}"
  },
  "EmailSettings": {
    "SmtpHost": "${SMTP_HOST}",
    "SmtpPort": 587,
    "SmtpUser": "${SMTP_USER}",
    "SmtpPassword": "${SMTP_PASSWORD}",
    "FromEmail": "${FROM_EMAIL}",
    "FromName": "${FROM_NAME}",
    "EnableSsl": true
  },
  "StorageSettings": {
    "Provider": "Azure",
    "ConnectionString": "${AZURE_STORAGE_CONNECTION}",
    "ContainerName": "${AZURE_STORAGE_CONTAINER}"
  },
  "Features": {
    "DynamicPricing": {
      "Enabled": true,
      "CacheDurationMinutes": 15,
      "MaxRulesPerActivity": 10
    },
    "Recommendations": {
      "Enabled": true,
      "CacheExpiryHours": 1,
      "MaxRecommendations": 20,
      "MinSimilarityScore": 0.5
    },
    "Loyalty": {
      "Enabled": true,
      "PointsPerRupee": 1,
      "PointsExpiryDays": 365,
      "MinRedemptionPoints": 100
    },
    "GiftCards": {
      "Enabled": true,
      "DefaultValidityDays": 365,
      "AllowPartialRedemption": true,
      "MinAmount": 500,
      "MaxAmount": 50000
    },
    "QRCheckIn": {
      "Enabled": true,
      "ExpiryMinutes": 5,
      "MaxScansPerBooking": 1
    },
    "Weather": {
      "Enabled": true,
      "CheckInterval": "1.00:00:00",
      "ApiKey": "${WEATHER_API_KEY}",
      "Provider": "OpenWeatherMap"
    }
  },
  "RateLimit": {
    "EnableRateLimiting": true,
    "PermitLimit": 100,
    "WindowMinutes": 1,
    "QueueLimit": 10
  },
  "Cors": {
    "AllowedOrigins": "${ALLOWED_ORIGINS}",
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "PATCH"],
    "AllowedHeaders": ["*"],
    "AllowCredentials": true
  }
}
```

---

## Database Deployment

### Step 1: Backup Existing Database (if applicable)

```bash
# Create backup
pg_dump -h $DB_HOST -U $DB_USER -d funbookr_prod > backup_$(date +%Y%m%d_%H%M%S).sql

# Verify backup
ls -lh backup_*.sql
```

### Step 2: Run Base Schema

```bash
# Connect to database
psql -h $DB_HOST -U $DB_USER -d funbookr_prod

# Run base schema (if fresh install)
\i docs/db/funbookr_schema.sql

# Run fixes
\i docs/db/funbookr_schema_fixes.sql
```

### Step 3: Apply Production Features Schema

```bash
# Run production features schema
psql -h $DB_HOST -U $DB_USER -d funbookr_prod -f docs/db/production_features_schema.sql

# Verify tables created
psql -h $DB_HOST -U $DB_USER -d funbookr_prod -c "\dt"
```

### Step 4: Insert Sample/Initial Data

```bash
# Insert loyalty tiers (already in production_features_schema.sql)
# Insert system settings
psql -h $DB_HOST -U $DB_USER -d funbookr_prod <<EOF
INSERT INTO system_settings (key, value, description) VALUES
('platform_commission', '10', 'Default platform commission percentage'),
('booking_advance_days', '90', 'Maximum days in advance for booking'),
('loyalty_signup_bonus', '500', 'Points awarded on signup'),
('referral_reward_referrer', '500', 'Points for referrer'),
('referral_reward_referred', '500', 'Points for referred user'),
('gift_card_photo_reward', '50', 'Points for uploading photo'),
('review_reward_points', '100', 'Points for leaving review')
ON CONFLICT (key) DO UPDATE SET value = EXCLUDED.value;
EOF
```

### Step 5: Verify Database

```bash
# Check table counts
psql -h $DB_HOST -U $DB_USER -d funbookr_prod <<EOF
SELECT 
    schemaname,
    tablename,
    pg_size_pretty(pg_total_relation_size(schemaname||'.'||tablename)) AS size
FROM pg_tables
WHERE schemaname = 'public'
ORDER BY pg_total_relation_size(schemaname||'.'||tablename) DESC;
EOF

# Check indexes
psql -h $DB_HOST -U $DB_USER -d funbookr_prod -c "\di"

# Check functions
psql -h $DB_HOST -U $DB_USER -d funbookr_prod -c "\df"
```

---

## Application Deployment

### Option 1: Docker Deployment (Recommended)

#### 1. Build Docker Image

```bash
# Build image
docker build -t funbookr-api:latest .

# Tag for registry
docker tag funbookr-api:latest your-registry/funbookr-api:v1.0.0
docker tag funbookr-api:latest your-registry/funbookr-api:latest

# Push to registry
docker push your-registry/funbookr-api:v1.0.0
docker push your-registry/funbookr-api:latest
```

#### 2. Create docker-compose.yml

```yaml
version: '3.8'

services:
  api:
    image: your-registry/funbookr-api:latest
    container_name: funbookr-api
    restart: unless-stopped
    ports:
      - "5000:8080"
    environment:
      - ASPNETCORE_ENVIRONMENT=Production
      - ASPNETCORE_URLS=http://+:8080
    env_file:
      - .env
    depends_on:
      - redis
    networks:
      - funbookr-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s

  redis:
    image: redis:7-alpine
    container_name: funbookr-redis
    restart: unless-stopped
    ports:
      - "6379:6379"
    volumes:
      - redis-data:/data
    networks:
      - funbookr-network
    command: redis-server --appendonly yes

  nginx:
    image: nginx:alpine
    container_name: funbookr-nginx
    restart: unless-stopped
    ports:
      - "80:80"
      - "443:443"
    volumes:
      - ./nginx.conf:/etc/nginx/nginx.conf:ro
      - ./ssl:/etc/nginx/ssl:ro
    depends_on:
      - api
    networks:
      - funbookr-network

volumes:
  redis-data:

networks:
  funbookr-network:
    driver: bridge
```

#### 3. Deploy with Docker Compose

```bash
# Start services
docker-compose up -d

# Check status
docker-compose ps

# View logs
docker-compose logs -f api
```

### Option 2: Azure App Service Deployment

```bash
# Login to Azure
az login

# Create resource group
az group create --name funbookr-rg --location eastus

# Create App Service plan
az appservice plan create \
  --name funbookr-plan \
  --resource-group funbookr-rg \
  --sku P1V2 \
  --is-linux

# Create web app
az webapp create \
  --name funbookr-api \
  --resource-group funbookr-rg \
  --plan funbookr-plan \
  --runtime "DOTNETCORE:8.0"

# Configure app settings
az webapp config appsettings set \
  --name funbookr-api \
  --resource-group funbookr-rg \
  --settings @appsettings.json

# Deploy application
az webapp deployment source config-zip \
  --name funbookr-api \
  --resource-group funbookr-rg \
  --src ./publish.zip

# Configure custom domain
az webapp config hostname add \
  --webapp-name funbookr-api \
  --resource-group funbookr-rg \
  --hostname api.funbookr.com

# Enable HTTPS
az webapp update \
  --name funbookr-api \
  --resource-group funbookr-rg \
  --https-only true
```

### Option 3: Manual Deployment

```bash
# Build application
dotnet publish src/ActivoosCRM.API/ActivoosCRM.API.csproj \
  -c Release \
  -o ./publish \
  --runtime linux-x64 \
  --self-contained false

# Copy to server
scp -r ./publish/* user@server:/var/www/funbookr-api/

# On server, create systemd service
sudo nano /etc/systemd/system/funbookr-api.service
```

**funbookr-api.service:**

```ini
[Unit]
Description=FunBookr API Service
After=network.target

[Service]
Type=notify
User=www-data
WorkingDirectory=/var/www/funbookr-api
ExecStart=/usr/bin/dotnet /var/www/funbookr-api/ActivoosCRM.API.dll
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=funbookr-api
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=DOTNET_PRINT_TELEMETRY_MESSAGE=false

[Install]
WantedBy=multi-user.target
```

```bash
# Enable and start service
sudo systemctl enable funbookr-api
sudo systemctl start funbookr-api
sudo systemctl status funbookr-api
```

---

## Post-Deployment Verification

### 1. Health Check

```bash
# Check application health
curl https://api.funbookr.com/health

# Expected response:
# {
#   "status": "Healthy",
#   "checks": {
#     "database": "Healthy",
#     "redis": "Healthy"
#   }
# }
```

### 2. API Endpoints Testing

```bash
# Test authentication
curl -X POST https://api.funbookr.com/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@example.com","password":"password"}'

# Test dynamic pricing
curl "https://api.funbookr.com/api/dynamicpricing/calculate?activityId=xxx&bookingDate=2025-12-15&bookingTime=10:00:00&participants=2"

# Test recommendations
curl https://api.funbookr.com/api/recommendations/trending?count=10

# Test gift card validation
curl https://api.funbookr.com/api/giftcards/validate/FB-1234-5678-9012
```

### 3. Database Connection Test

```bash
# From application server
psql -h $DB_HOST -U $DB_USER -d funbookr_prod -c "SELECT COUNT(*) FROM users;"
```

### 4. Performance Tests

```bash
# Install k6
curl https://github.com/grafana/k6/releases/download/v0.47.0/k6-v0.47.0-linux-amd64.tar.gz -L | tar xvz

# Run load test
./k6 run tests/LoadTests/load-test.js
```

### 5. Monitoring Setup

```bash
# Verify Application Insights
curl https://api.funbookr.com/api/diagnostics/telemetry

# Check logs
docker-compose logs -f api | grep ERROR
# OR on server
sudo journalctl -u funbookr-api -f
```

---

## Monitoring & Maintenance

### Application Monitoring

**Key Metrics to Track:**

1. **API Performance**
   - Request duration (p50, p95, p99)
   - Requests per second
   - Error rate
   - Success rate

2. **Database Performance**
   - Query duration
   - Connection pool usage
   - Slow queries
   - Deadlocks

3. **Business Metrics**
   - Bookings per hour
   - Revenue per hour
   - Active users
   - Conversion rate

4. **System Metrics**
   - CPU usage
   - Memory usage
   - Disk I/O
   - Network I/O

### Alert Configuration

```yaml
alerts:
  - name: High Error Rate
    condition: error_rate > 5%
    duration: 5m
    action: email, sms
    
  - name: High Response Time
    condition: p95_duration > 1000ms
    duration: 5m
    action: email
    
  - name: Low Success Rate
    condition: success_rate < 95%
    duration: 5m
    action: email, pagerduty
    
  - name: Database Connection Issues
    condition: db_connection_errors > 10
    duration: 1m
    action: email, sms, pagerduty
```

### Backup Strategy

```bash
# Automate daily backups
cat > /usr/local/bin/backup-funbookr.sh <<'EOF'
#!/bin/bash
BACKUP_DIR=/backups/funbookr
DATE=$(date +%Y%m%d_%H%M%S)

# Database backup
pg_dump -h $DB_HOST -U $DB_USER -d funbookr_prod | gzip > $BACKUP_DIR/db_$DATE.sql.gz

# Keep last 7 days
find $BACKUP_DIR -name "db_*.sql.gz" -mtime +7 -delete

# Upload to cloud storage
az storage blob upload \
  --account-name funbookrbackups \
  --container-name database-backups \
  --name db_$DATE.sql.gz \
  --file $BACKUP_DIR/db_$DATE.sql.gz
EOF

chmod +x /usr/local/bin/backup-funbookr.sh

# Add to crontab (daily at 2 AM)
crontab -e
# Add:
# 0 2 * * * /usr/local/bin/backup-funbookr.sh
```

### Update Procedures

```bash
# Pull latest code
git pull origin main

# Build new image
docker build -t funbookr-api:v1.0.1 .

# Run database migrations if any
psql -h $DB_HOST -U $DB_USER -d funbookr_prod -f migrations/v1.0.1.sql

# Update service
docker-compose pull
docker-compose up -d

# Verify
docker-compose ps
curl https://api.funbookr.com/health
```

---

## Rollback Procedures

### Emergency Rollback

If critical issues occur after deployment:

```bash
# 1. Stop current version
docker-compose down

# 2. Restore database backup
gunzip < /backups/funbookr/db_YYYYMMDD_HHMMSS.sql.gz | \
  psql -h $DB_HOST -U $DB_USER -d funbookr_prod

# 3. Deploy previous version
docker-compose -f docker-compose.previous.yml up -d

# 4. Verify
curl https://api.funbookr.com/health

# 5. Notify team
```

### Partial Rollback (Feature Flags)

```bash
# Disable specific features without full rollback
az webapp config appsettings set \
  --name funbookr-api \
  --resource-group funbookr-rg \
  --settings \
    Features__DynamicPricing__Enabled=false \
    Features__Recommendations__Enabled=false

# Restart app
az webapp restart --name funbookr-api --resource-group funbookr-rg
```

---

## Troubleshooting

### Common Issues

#### 1. Database Connection Failures

```bash
# Check database accessibility
nc -zv $DB_HOST 5432

# Check credentials
psql -h $DB_HOST -U $DB_USER -d funbookr_prod -c "SELECT 1;"

# Check firewall rules
az postgres flexible-server firewall-rule list \
  --resource-group funbookr-rg \
  --server-name funbookr-db
```

#### 2. High Memory Usage

```bash
# Check memory usage
docker stats

# Restart services
docker-compose restart api

# Scale if needed
docker-compose up -d --scale api=3
```

#### 3. Slow API Responses

```bash
# Check database queries
psql -h $DB_HOST -U $DB_USER -d funbookr_prod <<EOF
SELECT query, calls, total_exec_time, mean_exec_time
FROM pg_stat_statements
ORDER BY mean_exec_time DESC
LIMIT 10;
EOF

# Check cache
redis-cli -h $REDIS_HOST
> INFO stats
> SLOWLOG GET 10
```

---

## Security Checklist

- [ ] HTTPS enabled with valid SSL certificate
- [ ] Environment variables secured (not in code)
- [ ] Database credentials rotated
- [ ] API keys secured in Azure Key Vault
- [ ] Rate limiting enabled
- [ ] CORS properly configured
- [ ] SQL injection protection
- [ ] XSS protection headers
- [ ] CSRF protection
- [ ] Input validation on all endpoints
- [ ] Sensitive data encrypted
- [ ] Audit logging enabled
- [ ] Regular security scans scheduled

---

## Performance Optimization

### Caching Strategy

```csharp
// Redis cache configuration
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration["Redis:Connection"];
    options.InstanceName = "funbookr:";
});

// Cache duration by feature
- Activity details: 5 minutes
- Pricing calculations: 15 minutes
- Recommendations: 1 hour
- Static content: 24 hours
```

### Database Optimization

```sql
-- Create additional indexes for common queries
CREATE INDEX CONCURRENTLY idx_bookings_customer_status_date 
ON bookings(customer_id, status, booking_date DESC);

CREATE INDEX CONCURRENTLY idx_activities_category_location_active
ON activities(category_id, location_id, is_active) WHERE status = 'active';

-- Vacuum and analyze
VACUUM ANALYZE;
```

### CDN Configuration

```nginx
# nginx.conf
location ~* \.(jpg|jpeg|png|gif|ico|css|js)$ {
    expires 1y;
    add_header Cache-Control "public, immutable";
}

location /api/ {
    proxy_cache api_cache;
    proxy_cache_valid 200 5m;
    proxy_cache_use_stale error timeout updating;
}
```

---

## Success Criteria

✅ **Application Health**
- API returns HTTP 200 for /health
- Response time < 200ms for health check
- Zero critical errors in logs

✅ **Database**
- All tables created successfully
- Indexes in place
- Sample data loaded
- Backup automated

✅ **Features**
- Dynamic pricing returns correct calculations
- Gift cards can be purchased and redeemed
- Loyalty points awarded on bookings
- QR codes generate and validate
- Recommendations returned

✅ **Performance**
- API p95 response time < 500ms
- Database queries < 50ms
- Cache hit rate > 80%
- Error rate < 1%

✅ **Security**
- HTTPS enforced
- Authentication working
- Rate limiting active
- CORS configured

---

## Support & Escalation

### Support Contacts

- **Technical Lead:** tech-lead@funbookr.com
- **DevOps:** devops@funbookr.com
- **Database Admin:** dba@funbookr.com
- **On-Call:** +91-XXX-XXX-XXXX

### Escalation Matrix

| Issue Severity | Response Time | Escalation |
|----------------|---------------|------------|
| P0 - Critical (Site Down) | 15 minutes | CTO, DevOps Lead |
| P1 - High (Major Feature Down) | 1 hour | Tech Lead, DevOps |
| P2 - Medium (Performance Issues) | 4 hours | Tech Lead |
| P3 - Low (Minor Issues) | Next business day | Support Team |

---

## Conclusion

You now have a fully documented, production-ready deployment of FunBookr with all advanced features. Follow this guide for consistent, reliable deployments.

**Next Steps:**
1. Schedule deployment date/time
2. Notify stakeholders
3. Prepare rollback plan
4. Execute deployment
5. Monitor closely for 24-48 hours
6. Gather feedback and optimize

**Good luck with your deployment! 🚀**
