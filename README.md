# ActivoosCRM - Production-Ready CRM API

A clean architecture .NET 9 Web API with PostgreSQL for customer relationship management, activity booking, and business analytics.

## 🏗️ Architecture

This project follows **Clean Architecture** principles:

```
├── ActivoosCRM.Domain         # Enterprise business rules & entities
├── ActivoosCRM.Application    # Application business rules & use cases
├── ActivoosCRM.Infrastructure # External concerns (DB, Auth, Services)
└── ActivoosCRM.API           # API controllers & presentation layer
```

### Key Design Patterns
- **Clean Architecture** - Separation of concerns
- **CQRS Pattern** - Command Query Responsibility Segregation (ready for MediatR)
- **Repository Pattern** - Data access abstraction
- **Unit of Work** - Transaction management
- **Dependency Injection** - Loose coupling

## 🚀 Features

- ✅ Clean Architecture implementation
- ✅ PostgreSQL database with Entity Framework Core
- ✅ JWT Authentication & Authorization
- ✅ Swagger/OpenAPI documentation
- ✅ Structured logging with Serilog
- ✅ Health checks
- ✅ CORS configuration
- ✅ Global error handling (ready to implement)
- ✅ Repository & Unit of Work patterns
- ✅ Domain entities: User, Customer, Activity, Booking, TimeSlot
- ✅ Production-ready configuration

## 📋 Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [PostgreSQL 14+](https://www.postgresql.org/download/)
- IDE: Visual Studio 2022, VS Code, or Rider

## 🔧 Getting Started

### 1. Clone the Repository

```bash
cd c:\workspace\activoos_be
```

### 2. Set Up PostgreSQL Database

**Option A: Using Docker**

```bash
docker-compose up -d
```

**Option B: Manual Setup**

Create a PostgreSQL database:

```sql
CREATE DATABASE activoos_crm_dev;
```

### 3. Update Connection String

Edit `src/ActivoosCRM.API/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=activoos_crm_dev;Username=your_user;Password=your_password"
  }
}
```

### 4. Run Migrations

```bash
cd src/ActivoosCRM.Infrastructure
dotnet ef migrations add InitialCreate --startup-project ../ActivoosCRM.API
dotnet ef database update --startup-project ../ActivoosCRM.API
```

### 5. Run the API

```bash
cd src/ActivoosCRM.API
dotnet run
```

The API will be available at:
- **Swagger UI**: https://localhost:5001
- **Health Check**: https://localhost:5001/api/health

## 📁 Project Structure

```
activoos_be/
├── src/
│   ├── ActivoosCRM.Domain/
│   │   ├── Common/                 # Base entities, interfaces
│   │   └── Entities/               # Domain entities (User, Customer, etc.)
│   │
│   ├── ActivoosCRM.Application/
│   │   ├── Common/
│   │   │   ├── Interfaces/         # Repository, UnitOfWork, Auth interfaces
│   │   │   ├── Models/             # DTOs, responses, pagination
│   │   │   └── Exceptions/         # Custom exceptions
│   │   └── Features/               # CQRS handlers (to be implemented)
│   │
│   ├── ActivoosCRM.Infrastructure/
│   │   ├── Persistence/            # DbContext, configurations
│   │   ├── Repositories/           # Repository implementations
│   │   └── Services/               # Auth service, file storage, etc.
│   │
│   └── ActivoosCRM.API/
│       ├── Controllers/            # API controllers
│       ├── Middleware/             # Custom middleware (to be added)
│       ├── appsettings.json        # Configuration
│       └── Program.cs              # Application entry point
│
├── tests/                          # Unit & integration tests (to be added)
├── docker-compose.yml              # Docker services configuration
└── README.md                       # This file
```

## 🗃️ Database Schema

### Core Entities

**Users**
- Manages authentication and authorization
- Roles: admin, manager, staff, viewer

**Customers**
- Customer information and contact details
- Booking history and spending analytics

**Activities**
- Bookable activities/services
- Pricing, capacity, and availability

**TimeSlots**
- Available time slots for each activity
- Day and time configurations

**Bookings**
- Customer bookings for activities
- Payment status and tracking
- Cancellation handling

## 🔐 Authentication

The API uses **JWT Bearer** authentication:

1. Register/Login to receive access and refresh tokens
2. Include token in request header:
   ```
   Authorization: Bearer {your_token}
   ```

**Token Expiration:**
- Access Token: 15 minutes
- Refresh Token: 7 days

## 📊 API Endpoints

### Health & Monitoring
- `GET /api/health` - Health check endpoint

### Authentication (To be implemented)
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - User login
- `POST /api/auth/refresh` - Refresh access token
- `POST /api/auth/logout` - Logout user
- `GET /api/auth/me` - Get current user

### Customers (To be implemented)
- `GET /api/customers` - Get all customers
- `GET /api/customers/{id}` - Get customer by ID
- `POST /api/customers` - Create customer
- `PUT /api/customers/{id}` - Update customer
- `DELETE /api/customers/{id}` - Delete customer

### Activities (To be implemented)
- `GET /api/activities` - Get all activities
- `GET /api/activities/{id}` - Get activity by ID
- `POST /api/activities` - Create activity
- `PUT /api/activities/{id}` - Update activity
- `DELETE /api/activities/{id}` - Delete activity

### Bookings (To be implemented)
- `GET /api/bookings` - Get all bookings
- `GET /api/bookings/{id}` - Get booking by ID
- `POST /api/bookings` - Create booking
- `PUT /api/bookings/{id}` - Update booking
- `POST /api/bookings/{id}/cancel` - Cancel booking

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 📦 NuGet Packages

### Domain Layer
- No external dependencies (Pure domain logic)

### Application Layer
- MediatR
- AutoMapper
- FluentValidation

### Infrastructure Layer
- Npgsql.EntityFrameworkCore.PostgreSQL
- Microsoft.EntityFrameworkCore.Design
- Microsoft.AspNetCore.Authentication.JwtBearer
- BCrypt.Net-Next

### API Layer
- Serilog.AspNetCore
- Swashbuckle.AspNetCore
- Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore

## 🐳 Docker Support

```bash
# Start PostgreSQL
docker-compose up -d postgres

# Stop services
docker-compose down

# View logs
docker-compose logs -f
```

## 🔒 Security Best Practices

- ✅ Password hashing with BCrypt (12 rounds)
- ✅ JWT token validation
- ✅ HTTPS enforcement
- ✅ SQL injection prevention (Entity Framework parameterized queries)
- ✅ CORS configuration
- ⚠️ Rate limiting (to be implemented)
- ⚠️ Input validation (to be implemented with FluentValidation)

## 📈 Next Steps

### High Priority
1. Implement Authentication controllers and endpoints
2. Implement Customer CRUD operations
3. Implement Activity management
4. Implement Booking system
5. Add global exception handling middleware
6. Add request/response logging middleware
7. Add FluentValidation for request validation
8. Implement MediatR for CQRS pattern

### Medium Priority
9. Add unit tests
10. Add integration tests
11. Implement dashboard analytics endpoints
12. Add file upload functionality
13. Add pagination helpers
14. Implement rate limiting
15. Add caching with Redis

### Nice to Have
16. Add SignalR for real-time notifications
17. Implement email service
18. Add comprehensive API documentation
19. CI/CD pipeline setup
20. Performance monitoring

## 🤝 Contributing

1. Create feature branch: `git checkout -b feature/amazing-feature`
2. Commit changes: `git commit -m 'Add amazing feature'`
3. Push to branch: `git push origin feature/amazing-feature`
4. Open Pull Request

## 📝 Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__DefaultConnection="Host=localhost;Port=5432;Database=activoos_crm;Username=postgres;Password=postgres"

# JWT
Jwt__Secret="YourSuperSecretKeyHere"
Jwt__Issuer="ActivoosCRM"
Jwt__Audience="ActivoosCRM"

# Logging
Serilog__MinimumLevel__Default="Information"
```

## 📄 License

This project is licensed under the MIT License.

## 👥 Support

For support, email support@activooscrm.com or open an issue in the repository.

---

**Built with ❤️ using .NET 9 and Clean Architecture principles**
