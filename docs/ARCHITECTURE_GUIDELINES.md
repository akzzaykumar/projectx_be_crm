# FUNBOOKR CLEAN ARCHITECTURE GUIDELINES
## Clean Architecture with CQRS, Unit of Work, and Domain-Driven Design

---

## **TABLE OF CONTENTS**

1. [Architecture Overview](#architecture-overview)
2. [Project Structure](#project-structure)
3. [Layer Responsibilities](#layer-responsibilities)
4. [CQRS Pattern Implementation](#cqrs-pattern-implementation)
5. [Domain Layer Guidelines](#domain-layer-guidelines)
6. [Application Layer Guidelines](#application-layer-guidelines)
7. [Infrastructure Layer Guidelines](#infrastructure-layer-guidelines)
8. [API Layer Guidelines](#api-layer-guidelines)
9. [Naming Conventions](#naming-conventions)
10. [Development Workflow](#development-workflow)
11. [Testing Strategy](#testing-strategy)
12. [Common Patterns & Examples](#common-patterns--examples)

---

## **ARCHITECTURE OVERVIEW**

### Core Principles

```
┌─────────────────────────────────────────────────────────────┐
│                         API Layer                            │
│              (Controllers, Middleware, Filters)              │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                    Application Layer                         │
│        (Commands, Queries, DTOs, Handlers, Validators)       │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                      Domain Layer                            │
│         (Entities, Value Objects, Domain Events,             │
│          Domain Services, Interfaces, Exceptions)            │
└──────────────────────────┬──────────────────────────────────┘
                           │
┌──────────────────────────▼──────────────────────────────────┐
│                  Infrastructure Layer                        │
│      (DbContext, Repositories, External Services,            │
│       Email, SMS, Payment Gateway Implementations)           │
└─────────────────────────────────────────────────────────────┘
```

### Dependency Rules

✅ **ALLOWED:**
- API → Application
- Application → Domain
- Infrastructure → Domain
- Infrastructure → Application

❌ **FORBIDDEN:**
- Domain → Application
- Domain → Infrastructure
- Domain → API
- Application → Infrastructure (only through interfaces)

---

## **PROJECT STRUCTURE**

```
ActivoosCRM/
│
├── src/
│   ├── ActivoosCRM.Domain/
│   │   ├── Entities/
│   │   │   ├── User.cs
│   │   │   ├── Activity.cs
│   │   │   ├── Booking.cs
│   │   │   └── ...
│   │   ├── ValueObjects/
│   │   │   ├── Money.cs
│   │   │   ├── Address.cs
│   │   │   ├── Email.cs
│   │   │   └── PhoneNumber.cs
│   │   ├── Enums/
│   │   │   ├── BookingStatus.cs
│   │   │   ├── UserRole.cs
│   │   │   └── PaymentStatus.cs
│   │   ├── Events/
│   │   │   ├── BookingCreatedEvent.cs
│   │   │   ├── PaymentCompletedEvent.cs
│   │   │   └── ...
│   │   ├── Exceptions/
│   │   │   ├── DomainException.cs
│   │   │   ├── BookingNotFoundException.cs
│   │   │   └── ...
│   │   ├── Interfaces/
│   │   │   ├── IRepository.cs
│   │   │   ├── IUnitOfWork.cs
│   │   │   └── IDomainEventDispatcher.cs
│   │   ├── Services/
│   │   │   └── (Domain services only)
│   │   └── Common/
│   │       ├── BaseEntity.cs
│   │       └── AuditableEntity.cs
│   │
│   ├── ActivoosCRM.Application/
│   │   ├── Common/
│   │   │   ├── Interfaces/
│   │   │   │   ├── IApplicationDbContext.cs
│   │   │   │   ├── IDateTime.cs
│   │   │   │   ├── IEmailService.cs
│   │   │   │   ├── ISmsService.cs
│   │   │   │   └── IPaymentGateway.cs
│   │   │   ├── Models/
│   │   │   │   ├── Result.cs
│   │   │   │   ├── PaginatedList.cs
│   │   │   │   └── ApiResponse.cs
│   │   │   ├── Behaviors/
│   │   │   │   ├── ValidationBehavior.cs
│   │   │   │   ├── LoggingBehavior.cs
│   │   │   │   └── TransactionBehavior.cs
│   │   │   └── Exceptions/
│   │   │       ├── ValidationException.cs
│   │   │       └── NotFoundException.cs
│   │   ├── Features/
│   │   │   ├── Bookings/
│   │   │   │   ├── Commands/
│   │   │   │   │   ├── CreateBooking/
│   │   │   │   │   │   ├── CreateBookingCommand.cs
│   │   │   │   │   │   ├── CreateBookingCommandHandler.cs
│   │   │   │   │   │   ├── CreateBookingCommandValidator.cs
│   │   │   │   │   │   └── CreateBookingDto.cs
│   │   │   │   │   ├── CancelBooking/
│   │   │   │   │   │   ├── CancelBookingCommand.cs
│   │   │   │   │   │   └── CancelBookingCommandHandler.cs
│   │   │   │   │   └── ConfirmBooking/
│   │   │   │   ├── Queries/
│   │   │   │   │   ├── GetBookingById/
│   │   │   │   │   │   ├── GetBookingByIdQuery.cs
│   │   │   │   │   │   ├── GetBookingByIdQueryHandler.cs
│   │   │   │   │   │   └── BookingDto.cs
│   │   │   │   │   ├── GetBookingsList/
│   │   │   │   │   └── GetCustomerBookings/
│   │   │   │   └── EventHandlers/
│   │   │   │       └── BookingCreatedEventHandler.cs
│   │   │   ├── Activities/
│   │   │   ├── Users/
│   │   │   ├── Payments/
│   │   │   └── Reviews/
│   │   └── DependencyInjection.cs
│   │
│   ├── ActivoosCRM.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── ApplicationDbContext.cs
│   │   │   ├── Configurations/
│   │   │   │   ├── BookingConfiguration.cs
│   │   │   │   ├── ActivityConfiguration.cs
│   │   │   │   └── UserConfiguration.cs
│   │   │   ├── Migrations/
│   │   │   └── Interceptors/
│   │   │       └── AuditableEntityInterceptor.cs
│   │   ├── Repositories/
│   │   │   ├── Repository.cs
│   │   │   ├── UnitOfWork.cs
│   │   │   └── (Specific repositories if needed)
│   │   ├── Services/
│   │   │   ├── DateTimeService.cs
│   │   │   ├── EmailService.cs
│   │   │   ├── SmsService.cs
│   │   │   └── PaymentGatewayService.cs
│   │   ├── Identity/
│   │   │   ├── AuthService.cs
│   │   │   └── TokenService.cs
│   │   └── DependencyInjection.cs
│   │
│   └── ActivoosCRM.API/
│       ├── Controllers/
│       │   ├── BookingsController.cs
│       │   ├── ActivitiesController.cs
│       │   ├── AuthController.cs
│       │   └── ...
│       ├── Middleware/
│       │   ├── ExceptionHandlingMiddleware.cs
│       │   └── RequestLoggingMiddleware.cs
│       ├── Filters/
│       │   ├── ApiExceptionFilterAttribute.cs
│       │   └── ValidationFilterAttribute.cs
│       ├── Extensions/
│       │   └── ServiceCollectionExtensions.cs
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    ├── ActivoosCRM.Domain.Tests/
    ├── ActivoosCRM.Application.Tests/
    ├── ActivoosCRM.Infrastructure.Tests/
    └── ActivoosCRM.API.Tests/
```

---

## **LAYER RESPONSIBILITIES**

### **1. Domain Layer** (Core Business Logic)

**Purpose:** Contains enterprise business rules and domain logic

**Responsibilities:**
- Define entities with business logic methods
- Define value objects for domain concepts
- Define domain events
- Define repository interfaces
- Define domain exceptions
- NO dependencies on other layers
- NO infrastructure concerns (no EF, no HTTP, no JSON)

**What Goes Here:**
```csharp
✅ Business rules validation
✅ Entity state management
✅ Domain events
✅ Value objects
✅ Domain services (complex business logic)
✅ Interfaces for infrastructure

❌ Database queries
❌ HTTP calls
❌ Email sending
❌ File I/O
❌ External API calls
```

---

### **2. Application Layer** (Use Cases)

**Purpose:** Implements application use cases and orchestrates domain objects

**Responsibilities:**
- Define application use cases (Commands & Queries)
- Coordinate domain objects to fulfill use cases
- Define DTOs for external communication
- Define application interfaces (IEmailService, etc.)
- Implement validation logic
- Handle transactions (Unit of Work)

**What Goes Here:**
```csharp
✅ CQRS Commands and Queries
✅ Command/Query Handlers
✅ DTOs (Data Transfer Objects)
✅ Validators (FluentValidation)
✅ Mapping configurations (AutoMapper)
✅ Application exceptions
✅ Interfaces for external services

❌ Direct database access
❌ Infrastructure implementations
❌ HTTP request/response handling
```

---

### **3. Infrastructure Layer** (External Concerns)

**Purpose:** Implements interfaces defined in Domain/Application layers

**Responsibilities:**
- Implement repository pattern
- Implement Unit of Work
- EF Core DbContext and configurations
- External service implementations (Email, SMS, Payment)
- File system access
- Third-party API integrations
- Caching implementations

**What Goes Here:**
```csharp
✅ EF Core DbContext
✅ Repository implementations
✅ Unit of Work implementation
✅ External service implementations
✅ Database migrations
✅ Entity configurations (Fluent API)
✅ Third-party integrations

❌ Business logic
❌ Controllers
❌ HTTP-specific code
```

---

### **4. API Layer** (Presentation)

**Purpose:** Exposes application functionality via HTTP APIs

**Responsibilities:**
- Define API endpoints (Controllers)
- Handle HTTP requests/responses
- Authentication & Authorization
- Request validation
- Exception handling middleware
- API documentation (Swagger)
- CORS configuration

**What Goes Here:**
```csharp
✅ Controllers
✅ Middleware
✅ Filters
✅ API models (if different from DTOs)
✅ Authentication setup
✅ Dependency injection configuration

❌ Business logic
❌ Database access
❌ Direct entity manipulation
```

---

## **CQRS PATTERN IMPLEMENTATION**

### Command vs Query Separation

```
┌─────────────────────────────────────────────────────────────┐
│                         CQRS                                 │
├─────────────────────────────┬───────────────────────────────┤
│          COMMANDS           │          QUERIES              │
│   (State-Changing Actions)  │    (Read-Only Operations)     │
├─────────────────────────────┼───────────────────────────────┤
│ • Create booking            │ • Get booking by ID           │
│ • Update activity           │ • Get activities list         │
│ • Cancel booking            │ • Search activities           │
│ • Process payment           │ • Get user profile            │
│ • Create user               │ • Get booking history         │
├─────────────────────────────┼───────────────────────────────┤
│ Returns: Success/Failure    │ Returns: Data/DTO             │
│ Uses: Unit of Work          │ Uses: Read-only DbContext     │
│ Writes to DB                │ No DB modifications           │
│ Can trigger events          │ No side effects               │
└─────────────────────────────┴───────────────────────────────┘
```

### Command Structure

```csharp
// Command (Request)
public class CreateBookingCommand : IRequest<Result<Guid>>
{
    public Guid CustomerId { get; set; }
    public Guid ActivityId { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan BookingTime { get; set; }
    public int NumberOfParticipants { get; set; }
    public string? SpecialRequests { get; set; }
}

// Command Handler
public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, Result<Guid>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRepository<Booking> _bookingRepository;
    private readonly IRepository<Activity> _activityRepository;
    private readonly IEmailService _emailService;

    public CreateBookingCommandHandler(
        IUnitOfWork unitOfWork,
        IRepository<Booking> bookingRepository,
        IRepository<Activity> activityRepository,
        IEmailService emailService)
    {
        _unitOfWork = unitOfWork;
        _bookingRepository = bookingRepository;
        _activityRepository = activityRepository;
        _emailService = emailService;
    }

    public async Task<Result<Guid>> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate business rules
        var activity = await _activityRepository.GetByIdAsync(request.ActivityId, cancellationToken);
        if (activity == null)
            return Result<Guid>.Failure("Activity not found");

        if (!activity.IsAvailable(request.BookingDate))
            return Result<Guid>.Failure("Activity not available on selected date");

        // 2. Create domain entity
        var booking = Booking.Create(
            request.CustomerId,
            request.ActivityId,
            request.BookingDate,
            request.BookingTime,
            request.NumberOfParticipants,
            activity.Price);

        // 3. Add special requests if provided
        if (!string.IsNullOrWhiteSpace(request.SpecialRequests))
            booking.AddSpecialRequests(request.SpecialRequests);

        // 4. Persist to database
        await _bookingRepository.AddAsync(booking, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Send confirmation email (async, fire-and-forget)
        _ = _emailService.SendBookingConfirmationAsync(booking.Id);

        return Result<Guid>.Success(booking.Id);
    }
}

// Command Validator
public class CreateBookingCommandValidator : AbstractValidator<CreateBookingCommand>
{
    public CreateBookingCommandValidator()
    {
        RuleFor(x => x.CustomerId)
            .NotEmpty().WithMessage("Customer ID is required");

        RuleFor(x => x.ActivityId)
            .NotEmpty().WithMessage("Activity ID is required");

        RuleFor(x => x.BookingDate)
            .GreaterThanOrEqualTo(DateTime.Today).WithMessage("Booking date must be today or in the future");

        RuleFor(x => x.NumberOfParticipants)
            .GreaterThan(0).WithMessage("Number of participants must be at least 1")
            .LessThanOrEqualTo(50).WithMessage("Number of participants cannot exceed 50");
    }
}
```

### Query Structure

```csharp
// Query (Request)
public class GetBookingByIdQuery : IRequest<Result<BookingDto>>
{
    public Guid BookingId { get; set; }
}

// Query Handler
public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, Result<BookingDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetBookingByIdQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<BookingDto>> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var booking = await _context.Bookings
            .Include(b => b.Activity)
                .ThenInclude(a => a.Provider)
            .Include(b => b.Customer)
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Id == request.BookingId, cancellationToken);

        if (booking == null)
            return Result<BookingDto>.Failure("Booking not found");

        var dto = _mapper.Map<BookingDto>(booking);
        return Result<BookingDto>.Success(dto);
    }
}

// DTO
public class BookingDto
{
    public Guid Id { get; set; }
    public string BookingReference { get; set; }
    public DateTime BookingDate { get; set; }
    public TimeSpan BookingTime { get; set; }
    public int NumberOfParticipants { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; }
    public ActivityDto Activity { get; set; }
    public CustomerDto Customer { get; set; }
}
```

---

## **DOMAIN LAYER GUIDELINES**

### Entity Design

```csharp
// ✅ GOOD: Rich domain entity with behavior
public class Booking : AuditableEntity
{
    private Booking() { } // Private constructor for EF Core

    // Properties
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid ActivityId { get; private set; }
    public string BookingReference { get; private set; }
    public DateTime BookingDate { get; private set; }
    public TimeSpan BookingTime { get; private set; }
    public int NumberOfParticipants { get; private set; }
    public Money TotalAmount { get; private set; }
    public BookingStatus Status { get; private set; }
    public string? SpecialRequests { get; private set; }
    public string? CancellationReason { get; private set; }

    // Navigation properties
    public virtual User Customer { get; private set; }
    public virtual Activity Activity { get; private set; }
    public virtual ICollection<BookingParticipant> Participants { get; private set; } = new List<BookingParticipant>();

    // Factory method
    public static Booking Create(
        Guid customerId,
        Guid activityId,
        DateTime bookingDate,
        TimeSpan bookingTime,
        int numberOfParticipants,
        Money pricePerParticipant)
    {
        // Validation
        if (customerId == Guid.Empty)
            throw new DomainException("Customer ID cannot be empty");

        if (activityId == Guid.Empty)
            throw new DomainException("Activity ID cannot be empty");

        if (bookingDate < DateTime.Today)
            throw new DomainException("Booking date cannot be in the past");

        if (numberOfParticipants <= 0)
            throw new DomainException("Number of participants must be at least 1");

        // Create entity
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            ActivityId = activityId,
            BookingReference = GenerateBookingReference(),
            BookingDate = bookingDate,
            BookingTime = bookingTime,
            NumberOfParticipants = numberOfParticipants,
            TotalAmount = pricePerParticipant * numberOfParticipants,
            Status = BookingStatus.Pending
        };

        // Raise domain event
        booking.AddDomainEvent(new BookingCreatedEvent(booking.Id));

        return booking;
    }

    // Business methods
    public void Confirm()
    {
        if (Status != BookingStatus.Pending)
            throw new DomainException("Only pending bookings can be confirmed");

        Status = BookingStatus.Confirmed;
        AddDomainEvent(new BookingConfirmedEvent(Id));
    }

    public void Cancel(string reason)
    {
        if (Status == BookingStatus.Completed)
            throw new DomainException("Completed bookings cannot be cancelled");

        if (Status == BookingStatus.Cancelled)
            throw new DomainException("Booking is already cancelled");

        Status = BookingStatus.Cancelled;
        CancellationReason = reason;
        AddDomainEvent(new BookingCancelledEvent(Id, reason));
    }

    public void AddSpecialRequests(string requests)
    {
        if (string.IsNullOrWhiteSpace(requests))
            throw new DomainException("Special requests cannot be empty");

        SpecialRequests = requests;
    }

    public bool CanBeCancelled()
    {
        return Status == BookingStatus.Pending || Status == BookingStatus.Confirmed;
    }

    public bool IsInPast()
    {
        return BookingDate.Date < DateTime.Today;
    }

    private static string GenerateBookingReference()
    {
        return $"FB{DateTime.Now:yyyyMMdd}{new Random().Next(1000, 9999)}";
    }
}
```

### Value Objects

```csharp
// Value object for Money
public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; }

    private Money() { }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new DomainException("Amount cannot be negative");

        if (string.IsNullOrWhiteSpace(currency))
            throw new DomainException("Currency is required");

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    public static Money operator +(Money a, Money b)
    {
        if (a.Currency != b.Currency)
            throw new DomainException("Cannot add money with different currencies");

        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator *(Money money, int multiplier)
    {
        return new Money(money.Amount * multiplier, money.Currency);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";
}

// Base class for value objects
public abstract class ValueObject
{
    protected abstract IEnumerable<object> GetEqualityComponents();

    public override bool Equals(object obj)
    {
        if (obj == null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate((x, y) => x ^ y);
    }
}
```

### Domain Events

```csharp
// Domain event
public class BookingCreatedEvent : DomainEvent
{
    public Guid BookingId { get; }

    public BookingCreatedEvent(Guid bookingId)
    {
        BookingId = bookingId;
    }
}

// Base domain event
public abstract class DomainEvent
{
    public DateTime OccurredOn { get; protected set; }

    protected DomainEvent()
    {
        OccurredOn = DateTime.UtcNow;
    }
}

// Entity base with domain events
public abstract class BaseEntity
{
    private readonly List<DomainEvent> _domainEvents = new();

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

---

## **APPLICATION LAYER GUIDELINES**

### MediatR Pipeline Behaviors

```csharp
// Validation Behavior
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
                throw new ValidationException(failures);
        }

        return await next();
    }
}

// Logging Behavior
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        _logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next();

        _logger.LogInformation("Handled {RequestName}", requestName);

        return response;
    }
}

// Transaction Behavior
public class TransactionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public TransactionBehavior(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Skip for queries
        if (request is IQuery<TResponse>)
            return await next();

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return response;
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }
}
```

### Result Pattern

```csharp
public class Result<T>
{
    public bool IsSuccess { get; private set; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; private set; }
    public string Error { get; private set; }
    public string[] Errors { get; private set; }

    protected Result(bool isSuccess, T value, string error, string[] errors = null)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Errors = errors ?? Array.Empty<string>();
    }

    public static Result<T> Success(T value) => new(true, value, null);
    public static Result<T> Failure(string error) => new(false, default, error);
    public static Result<T> Failure(string[] errors) => new(false, default, null, errors);

    public TResult Match<TResult>(
        Func<T, TResult> onSuccess,
        Func<string, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Error);
    }
}
```

---

## **INFRASTRUCTURE LAYER GUIDELINES**

### Repository Pattern

```csharp
// Generic Repository Interface (Domain)
public interface IRepository<T> where T : BaseEntity
{
    Task<T> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default);
    Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task DeleteAsync(T entity, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> CountAsync(CancellationToken cancellationToken = default);
}

// Generic Repository Implementation (Infrastructure)
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(ApplicationDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetPagedAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        return entity;
    }

    public virtual Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        _dbSet.Remove(entity);
        return Task.CompletedTask;
    }

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(e => e.Id == id, cancellationToken);
    }

    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.CountAsync(cancellationToken);
    }
}
```

### Unit of Work Pattern

```csharp
// Interface (Domain)
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

// Implementation (Infrastructure)
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction _transaction;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving
        await DispatchDomainEventsAsync(cancellationToken);
        
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction?.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _transaction?.RollbackAsync(cancellationToken);
        }
        finally
        {
            _transaction?.Dispose();
            _transaction = null;
        }
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEntities = _context.ChangeTracker
            .Entries<BaseEntity>()
            .Where(x => x.Entity.DomainEvents.Any())
            .Select(x => x.Entity)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(x => x.DomainEvents)
            .ToList();

        domainEntities.ForEach(entity => entity.ClearDomainEvents());

        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context?.Dispose();
    }
}
```

### DbContext Configuration

```csharp
public class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly IDateTime _dateTime;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        IDateTime dateTime) : base(options)
    {
        _dateTime = dateTime;
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Activity> Activities { get; set; }
    public DbSet<Booking> Bookings { get; set; }
    public DbSet<Payment> Payments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        base.OnModelCreating(modelBuilder);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Update audit fields
        foreach (var entry in ChangeTracker.Entries<AuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = _dateTime.Now;
                    entry.Entity.UpdatedAt = _dateTime.Now;
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = _dateTime.Now;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}

// Entity Configuration Example
public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("bookings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.BookingReference)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(b => b.BookingDate)
            .IsRequired();

        builder.Property(b => b.NumberOfParticipants)
            .IsRequired();

        builder.OwnsOne(b => b.TotalAmount, money =>
        {
            money.Property(m => m.Amount)
                .HasColumnName("total_amount")
                .HasColumnType("decimal(10,2)")
                .IsRequired();

            money.Property(m => m.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Property(b => b.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(b => b.Customer)
            .WithMany()
            .HasForeignKey(b => b.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(b => b.Activity)
            .WithMany()
            .HasForeignKey(b => b.ActivityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(b => b.BookingReference)
            .IsUnique();

        builder.HasIndex(b => b.BookingDate);
        builder.HasIndex(b => b.Status);
    }
}
```

---

## **API LAYER GUIDELINES**

### Controller Structure

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BookingsController> _logger;

    public BookingsController(IMediator mediator, ILogger<BookingsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Create a new booking
    /// </summary>
    /// <param name="command">Booking details</param>
    /// <returns>Created booking ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<Guid>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateBooking([FromBody] CreateBookingCommand command)
    {
        var result = await _mediator.Send(command);

        return result.Match(
            onSuccess: id => CreatedAtAction(nameof(GetBookingById), new { id }, ApiResponse<Guid>.Success(id, "Booking created successfully")),
            onFailure: error => BadRequest(ApiResponse.Failure(error))
        );
    }

    /// <summary>
    /// Get booking by ID
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <returns>Booking details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<BookingDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBookingById(Guid id)
    {
        var query = new GetBookingByIdQuery { BookingId = id };
        var result = await _mediator.Send(query);

        return result.Match(
            onSuccess: booking => Ok(ApiResponse<BookingDto>.Success(booking)),
            onFailure: error => NotFound(ApiResponse.Failure(error))
        );
    }

    /// <summary>
    /// Get user's bookings
    /// </summary>
    /// <param name="pageNumber">Page number</param>
    /// <param name="pageSize">Page size</param>
    /// <returns>Paginated list of bookings</returns>
    [HttpGet("my-bookings")]
    [ProducesResponseType(typeof(ApiResponse<PaginatedList<BookingDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyBookings([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        var query = new GetCustomerBookingsQuery 
        { 
            CustomerId = Guid.Parse(userId),
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _mediator.Send(query);

        return result.Match(
            onSuccess: bookings => Ok(ApiResponse<PaginatedList<BookingDto>>.Success(bookings)),
            onFailure: error => BadRequest(ApiResponse.Failure(error))
        );
    }

    /// <summary>
    /// Cancel a booking
    /// </summary>
    /// <param name="id">Booking ID</param>
    /// <param name="command">Cancellation details</param>
    /// <returns>Success response</returns>
    [HttpPut("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelBooking(Guid id, [FromBody] CancelBookingCommand command)
    {
        command.BookingId = id;
        var result = await _mediator.Send(command);

        return result.Match(
            onSuccess: _ => Ok(ApiResponse.Success("Booking cancelled successfully")),
            onFailure: error => BadRequest(ApiResponse.Failure(error))
        );
    }
}
```

### Exception Handling Middleware

```csharp
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = exception switch
        {
            ValidationException validationEx => CreateValidationErrorResponse(context, validationEx),
            DomainException domainEx => CreateDomainErrorResponse(context, domainEx),
            NotFoundException notFoundEx => CreateNotFoundResponse(context, notFoundEx),
            UnauthorizedAccessException => CreateUnauthorizedResponse(context),
            _ => CreateInternalErrorResponse(context, exception)
        };

        await context.Response.WriteAsJsonAsync(response);
    }

    private static ApiResponse CreateValidationErrorResponse(HttpContext context, ValidationException exception)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        
        var errors = exception.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return ApiResponse.Failure("Validation failed", errors);
    }

    private static ApiResponse CreateDomainErrorResponse(HttpContext context, DomainException exception)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return ApiResponse.Failure(exception.Message);
    }

    private static ApiResponse CreateNotFoundResponse(HttpContext context, NotFoundException exception)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return ApiResponse.Failure(exception.Message);
    }

    private static ApiResponse CreateUnauthorizedResponse(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return ApiResponse.Failure("Unauthorized access");
    }

    private static ApiResponse CreateInternalErrorResponse(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        return ApiResponse.Failure("An internal server error occurred");
    }
}
```

---

## **NAMING CONVENTIONS**

### General Rules

```
✅ DO use PascalCase for classes, methods, properties, constants
✅ DO use camelCase for local variables, parameters, private fields
✅ DO use _camelCase for private fields (with underscore prefix)
✅ DO use SCREAMING_SNAKE_CASE for const values
✅ DO use descriptive, meaningful names
✅ DO use nouns for classes and properties
✅ DO use verbs for methods
✅ DON'T use Hungarian notation
✅ DON'T use abbreviations (except widely known ones like ID, URL)
```

### File Naming

```
✅ Entities: User.cs, Activity.cs, Booking.cs
✅ Commands: CreateBookingCommand.cs, CancelBookingCommand.cs
✅ Command Handlers: CreateBookingCommandHandler.cs
✅ Queries: GetBookingByIdQuery.cs, GetActivitiesListQuery.cs
✅ Query Handlers: GetBookingByIdQueryHandler.cs
✅ DTOs: BookingDto.cs, ActivityDto.cs
✅ Validators: CreateBookingCommandValidator.cs
✅ Interfaces: IRepository.cs, IUnitOfWork.cs, IEmailService.cs
✅ Implementations: Repository.cs, UnitOfWork.cs, EmailService.cs
✅ Controllers: BookingsController.cs (plural)
✅ Configurations: BookingConfiguration.cs
```

### Folder Structure

```
✅ Features/Bookings/Commands/CreateBooking/
✅ Features/Bookings/Queries/GetBookingById/
✅ Entities/ (not Entity/ or Models/)
✅ Interfaces/ (not Contracts/ or Abstractions/)
✅ Services/ (not Helpers/ or Utilities/)
```

---

## **DEVELOPMENT WORKFLOW**

### Adding a New Feature

**Step 1: Define Domain Entity**
```
Location: ActivoosCRM.Domain/Entities/
- Create entity with business logic
- Add value objects if needed
- Define domain events
- Add repository interface
```

**Step 2: Create Command/Query**
```
Location: ActivoosCRM.Application/Features/{FeatureName}/Commands|Queries/
- Create command/query class
- Create handler class
- Create validator class
- Create DTO class
```

**Step 3: Implement Infrastructure**
```
Location: ActivoosCRM.Infrastructure/
- Add entity configuration
- Implement repository if custom logic needed
- Create migration
```

**Step 4: Create API Endpoint**
```
Location: ActivoosCRM.API/Controllers/
- Add controller action
- Add XML documentation comments
- Configure route and authorization
```

**Step 5: Test**
```
- Write unit tests for domain logic
- Write unit tests for handlers
- Write integration tests for API
```

### Adding a New Query

1. Create query class in `Application/Features/{Feature}/Queries/{QueryName}/`
2. Create DTO class
3. Create handler that uses `IApplicationDbContext` directly (no UnitOfWork)
4. Use `.AsNoTracking()` for read-only queries
5. Add mapping configuration
6. Add controller endpoint
7. Test

### Adding a New Command

1. Create command class in `Application/Features/{Feature}/Commands/{CommandName}/`
2. Create validator class
3. Create handler that uses `IUnitOfWork` and repositories
4. Implement business logic in domain entities
5. Raise domain events if needed
6. Add controller endpoint
7. Test

---

## **TESTING STRATEGY**

### Unit Tests (Domain Layer)

```csharp
public class BookingTests
{
    [Fact]
    public void Create_WithValidData_ShouldCreateBooking()
    {
        // Arrange
        var customerId = Guid.NewGuid();
        var activityId = Guid.NewGuid();
        var bookingDate = DateTime.Today.AddDays(7);
        var bookingTime = TimeSpan.FromHours(10);
        var participants = 2;
        var price = new Money(1000, "INR");

        // Act
        var booking = Booking.Create(customerId, activityId, bookingDate, bookingTime, participants, price);

        // Assert
        Assert.NotNull(booking);
        Assert.Equal(BookingStatus.Pending, booking.Status);
        Assert.Equal(participants, booking.NumberOfParticipants);
        Assert.NotNull(booking.BookingReference);
        Assert.Single(booking.DomainEvents);
    }

    [Fact]
    public void Confirm_WhenPending_ShouldConfirmBooking()
    {
        // Arrange
        var booking = CreateValidBooking();

        // Act
        booking.Confirm();

        // Assert
        Assert.Equal(BookingStatus.Confirmed, booking.Status);
        Assert.Contains(booking.DomainEvents, e => e is BookingConfirmedEvent);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldThrowDomainException()
    {
        // Arrange
        var booking = CreateValidBooking();
        booking.Confirm();

        // Act & Assert
        Assert.Throws<DomainException>(() => booking.Confirm());
    }
}
```

### Unit Tests (Application Layer)

```csharp
public class CreateBookingCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IRepository<Booking>> _mockBookingRepository;
    private readonly Mock<IRepository<Activity>> _mockActivityRepository;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly CreateBookingCommandHandler _handler;

    public CreateBookingCommandHandlerTests()
    {
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockBookingRepository = new Mock<IRepository<Booking>>();
        _mockActivityRepository = new Mock<IRepository<Activity>>();
        _mockEmailService = new Mock<IEmailService>();

        _handler = new CreateBookingCommandHandler(
            _mockUnitOfWork.Object,
            _mockBookingRepository.Object,
            _mockActivityRepository.Object,
            _mockEmailService.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateBooking()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            CustomerId = Guid.NewGuid(),
            ActivityId = Guid.NewGuid(),
            BookingDate = DateTime.Today.AddDays(7),
            BookingTime = TimeSpan.FromHours(10),
            NumberOfParticipants = 2
        };

        var activity = Activity.Create(/* ... */);
        _mockActivityRepository
            .Setup(x => x.GetByIdAsync(command.ActivityId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(activity);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockBookingRepository.Verify(x => x.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

### Integration Tests (API Layer)

```csharp
public class BookingsControllerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;
    private readonly WebApplicationFactory<Program> _factory;

    public BookingsControllerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateBooking_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var command = new CreateBookingCommand
        {
            CustomerId = Guid.NewGuid(),
            ActivityId = Guid.NewGuid(),
            BookingDate = DateTime.Today.AddDays(7),
            BookingTime = TimeSpan.FromHours(10),
            NumberOfParticipants = 2
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/bookings", command);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        
        var result = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        Assert.True(result.Success);
        Assert.NotEqual(Guid.Empty, result.Data);
    }
}
```

---

## **COMMON PATTERNS & EXAMPLES**

### Pagination Pattern

```csharp
// Query
public class GetActivitiesListQuery : IRequest<Result<PaginatedList<ActivityDto>>>
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SearchTerm { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? LocationId { get; set; }
}

// Handler
public class GetActivitiesListQueryHandler : IRequestHandler<GetActivitiesListQuery, Result<PaginatedList<ActivityDto>>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMapper _mapper;

    public GetActivitiesListQueryHandler(IApplicationDbContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    public async Task<Result<PaginatedList<ActivityDto>>> Handle(GetActivitiesListQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Activities
            .Include(a => a.Provider)
            .Include(a => a.Category)
            .Include(a => a.Location)
            .AsNoTracking();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(a => a.Title.Contains(request.SearchTerm) || 
                                      a.Description.Contains(request.SearchTerm));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(a => a.CategoryId == request.CategoryId.Value);
        }

        if (request.LocationId.HasValue)
        {
            query = query.Where(a => a.LocationId == request.LocationId.Value);
        }

        // Get total count
        var totalCount = await query.CountAsync(cancellationToken);

        // Apply pagination
        var items = await query
            .OrderByDescending(a => a.Rating)
            .Skip((request.PageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = _mapper.Map<List<ActivityDto>>(items);

        var paginatedList = new PaginatedList<ActivityDto>(
            dtos,
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Result<PaginatedList<ActivityDto>>.Success(paginatedList);
    }
}

// PaginatedList class
public class PaginatedList<T>
{
    public List<T> Items { get; }
    public int PageNumber { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public PaginatedList(List<T> items, int count, int pageNumber, int pageSize)
    {
        Items = items;
        TotalCount = count;
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
    }
}
```

### Search Pattern with Specifications

```csharp
// Specification pattern
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
    List<string> IncludeStrings { get; }
}

public abstract class BaseSpecification<T> : ISpecification<T>
{
    public Expression<Func<T, bool>> Criteria { get; private set; }
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();

    protected void AddInclude(Expression<Func<T, object>> includeExpression)
    {
        Includes.Add(includeExpression);
    }

    protected void AddInclude(string includeString)
    {
        IncludeStrings.Add(includeString);
    }
}

// Specific specification
public class ActivitySearchSpecification : BaseSpecification<Activity>
{
    public ActivitySearchSpecification(string searchTerm, Guid? categoryId, Guid? locationId)
    {
        AddInclude(a => a.Provider);
        AddInclude(a => a.Category);
        AddInclude(a => a.Location);

        Criteria = BuildCriteria(searchTerm, categoryId, locationId);
    }

    private Expression<Func<Activity, bool>> BuildCriteria(string searchTerm, Guid? categoryId, Guid? locationId)
    {
        Expression<Func<Activity, bool>> predicate = a => a.Status == ActivityStatus.Active;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            predicate = predicate.And(a => a.Title.Contains(searchTerm) || a.Description.Contains(searchTerm));
        }

        if (categoryId.HasValue)
        {
            predicate = predicate.And(a => a.CategoryId == categoryId.Value);
        }

        if (locationId.HasValue)
        {
            predicate = predicate.And(a => a.LocationId == locationId.Value);
        }

        return predicate;
    }
}
```

---

## **CHECKLIST FOR NEW FEATURES**

### Before Starting
- [ ] Feature is well-defined and understood
- [ ] Dependencies identified
- [ ] Database changes planned
- [ ] Security requirements identified
- [ ] Performance considerations discussed

### Domain Layer
- [ ] Entity created with proper encapsulation
- [ ] Business logic in entity methods
- [ ] Value objects created for complex types
- [ ] Domain events defined
- [ ] Domain exceptions created
- [ ] Repository interface defined (if needed)

### Application Layer
- [ ] Command/Query created
- [ ] Handler implemented
- [ ] Validator created
- [ ] DTOs defined
- [ ] Mapping configured
- [ ] Unit tests written

### Infrastructure Layer
- [ ] Entity configuration created
- [ ] Migration generated
- [ ] Repository implemented (if custom needed)
- [ ] External services implemented
- [ ] Integration tests written

### API Layer
- [ ] Controller endpoint created
- [ ] Authorization configured
- [ ] XML documentation added
- [ ] Response types documented
- [ ] API tests written

### Documentation
- [ ] Code comments added
- [ ] XML documentation complete
- [ ] README updated (if needed)
- [ ] API documentation generated

---

## **BEST PRACTICES SUMMARY**

### ✅ DO

- Use meaningful names for everything
- Keep methods small and focused (Single Responsibility)
- Write unit tests for business logic
- Use async/await consistently
- Handle exceptions at appropriate levels
- Log important events and errors
- Use dependency injection
- Follow SOLID principles
- Keep controllers thin
- Use DTOs for API communication
- Validate input early
- Use domain events for cross-aggregate communication
- Make entities responsible for their own invariants
- Use value objects for domain concepts
- Keep transaction boundaries small
- Use read models for queries
- Separate write and read concerns (CQRS)

### ❌ DON'T

- Put business logic in controllers
- Use EF Core entities directly in API responses
- Access DbContext directly from controllers
- Use static methods for business logic
- Bypass validation
- Catch exceptions just to log them
- Use magic strings or numbers
- Create God classes
- Mix concerns between layers
- Return null (use Result pattern or Maybe monad)
- Use lazy loading in production
- Query database in loops
- Ignore warnings
- Skip tests

---

## **RESOURCES**

- Clean Architecture by Robert C. Martin
- Domain-Driven Design by Eric Evans
- Implementing Domain-Driven Design by Vaughn Vernon
- CQRS Pattern - Martin Fowler
- MediatR Documentation
- FluentValidation Documentation
- Entity Framework Core Documentation

---

**Remember:** This architecture is designed to be maintainable, testable, and scalable. Always question if a change violates these principles before implementing it.
