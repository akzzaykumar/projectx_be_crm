# 🚀 Quick Start Guide - ActivoosCRM API

## Prerequisites Check

Before starting, ensure you have:

- ✅ .NET 9 SDK installed
- ✅ PostgreSQL 14+ installed (or Docker)
- ✅ Your favorite IDE (VS Code, Visual Studio, or Rider)

## Step-by-Step Setup

### Step 1: Start PostgreSQL Database

**Option A: Using Docker (Recommended)**

```powershell
# Navigate to project root
cd c:\workspace\activoos_be

# Start PostgreSQL
docker-compose up -d postgres

# Verify it's running
docker ps
```

**Option B: Use Existing PostgreSQL**

Ensure PostgreSQL is running on localhost:5432

### Step 2: Update Configuration (if needed)

Edit `src/ActivoosCRM.API/appsettings.Development.json` if your PostgreSQL credentials differ:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=activoos_crm_dev;Username=YOUR_USER;Password=YOUR_PASSWORD"
  }
}
```

### Step 3: Apply Database Migrations

```powershell
# Navigate to Infrastructure project
cd c:\workspace\activoos_be\src\ActivoosCRM.Infrastructure

# Apply migrations to create database schema
dotnet ef database update --startup-project ..\ActivoosCRM.API
```

You should see output like:
```
Build succeeded.
Applying migration '20251020_InitialCreate'.
Done.
```

### Step 4: Run the API

```powershell
# Navigate to API project
cd c:\workspace\activoos_be\src\ActivoosCRM.API

# Run the application
dotnet run
```

Expected output:
```
[21:59:00 INF] Starting ActivoosCRM API
[21:59:00 INF] Now listening on: https://localhost:5001
[21:59:00 INF] Now listening on: http://localhost:5000
```

### Step 5: Test the API

Open your browser and navigate to:

**Swagger UI (API Documentation)**
```
https://localhost:5001
```

**Health Check Endpoint**
```
GET https://localhost:5001/api/health
```

Expected response:
```json
{
  "status": "Healthy",
  "timestamp": "2025-10-20T21:59:00Z",
  "service": "ActivoosCRM API",
  "version": "1.0.0"
}
```

## 🎉 Success! Your API is Running

You now have a fully functional clean architecture .NET 9 Web API with:

- ✅ PostgreSQL database with Entity Framework Core
- ✅ JWT authentication infrastructure
- ✅ Swagger/OpenAPI documentation
- ✅ Health checks
- ✅ Structured logging with Serilog
- ✅ Clean architecture layers (Domain, Application, Infrastructure, API)
- ✅ Repository & Unit of Work patterns

## Next Steps

### 1. Explore the Code Structure

```
activoos_be/
├── src/
│   ├── ActivoosCRM.Domain/        # Domain entities
│   ├── ActivoosCRM.Application/   # Business logic interfaces
│   ├── ActivoosCRM.Infrastructure/ # Database & services
│   └── ActivoosCRM.API/           # API controllers
```

### 2. Implement Your First Endpoint

Create a new controller in `src/ActivoosCRM.API/Controllers/`:

```csharp
using Microsoft.AspNetCore.Mvc;
using ActivoosCRM.Application.Common.Interfaces;

namespace ActivoosCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly IUnitOfWork _unitOfWork;
    
    public CustomersController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var customers = await _unitOfWork.Customers.GetAllAsync();
        return Ok(new { success = true, data = customers });
    }
}
```

### 3. Add Sample Data (Optional)

Connect to your database and add test data:

**Using pgAdmin** (if running with docker-compose):
```
URL: http://localhost:5050
Email: admin@activooscrm.com
Password: admin
```

**Or use psql**:
```powershell
docker exec -it activoos_postgres psql -U postgres -d activoos_crm_dev

-- Insert sample user
INSERT INTO "Users" ("Email", "PasswordHash", "FirstName", "LastName", "CompanyName", "Role", "CreatedAt", "UpdatedAt") 
VALUES ('admin@test.com', '$2a$12$LQv3c1yqBWVHxkd0LHAkCOYz6TtxMQJqhN8/LewY5Y7', 'Admin', 'User', 'Test Company', 'admin', NOW(), NOW());
```

### 4. Test Authentication (Coming Soon)

Once you implement the Auth controller:

```bash
# Register
POST https://localhost:5001/api/auth/register

# Login
POST https://localhost:5001/api/auth/login

# Get profile
GET https://localhost:5001/api/auth/me
```

## 🔧 Common Issues & Solutions

### Issue: Port Already in Use

**Solution:**
```powershell
# Change ports in src/ActivoosCRM.API/Properties/launchSettings.json
```

### Issue: Cannot Connect to Database

**Solution:**
```powershell
# Check if PostgreSQL is running
docker ps

# Check connection string in appsettings.Development.json
# Restart Docker container
docker-compose restart postgres
```

### Issue: Migration Fails

**Solution:**
```powershell
# Remove existing migration
cd c:\workspace\activoos_be\src\ActivoosCRM.Infrastructure
dotnet ef migrations remove --startup-project ..\ActivoosCRM.API

# Recreate migration
dotnet ef migrations add InitialCreate --startup-project ..\ActivoosCRM.API --output-dir Persistence/Migrations

# Apply to database
dotnet ef database update --startup-project ..\ActivoosCRM.API
```

### Issue: Build Errors

**Solution:**
```powershell
# Clean and rebuild
cd c:\workspace\activoos_be
dotnet clean
dotnet build
```

## 📚 Learn More

- Review `README.md` for comprehensive documentation
- Check `docker-compose.yml` for container configuration
- Explore entity definitions in `src/ActivoosCRM.Domain/Entities/`
- Study repository pattern in `src/ActivoosCRM.Infrastructure/Repositories/`

## 🎯 Implementation Roadmap

Following the API requirements document, implement:

1. **Authentication APIs** (Priority 1)
   - Register, Login, Refresh Token, Logout

2. **Customer APIs** (Priority 2)
   - CRUD operations with pagination

3. **Activity APIs** (Priority 3)
   - Activity management with time slots

4. **Booking APIs** (Priority 4)
   - Booking creation and management

5. **Dashboard & Analytics** (Priority 5)
   - Revenue tracking and statistics

## 🤝 Need Help?

- Check the comprehensive `README.md`
- Review the API requirements document
- Check Swagger UI for endpoint documentation
- Review the clean architecture pattern implementation

---

**Happy Coding! 🎉**
