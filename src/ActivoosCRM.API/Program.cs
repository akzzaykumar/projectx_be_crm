using ActivoosCRM.API.Configuration;
using ActivoosCRM.Application;
using ActivoosCRM.Infrastructure;
using ActivoosCRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to listen on port 8080 for Azure App Service
builder.WebHost.ConfigureKestrel(options =>
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    options.ListenAnyIP(int.Parse(port));
});

// Add environment variables support
builder.Configuration.AddEnvironmentVariables();

// Validate configuration early
try
{
    ConfigurationValidator.ValidateConfiguration(builder.Configuration);
    Log.Information("Configuration validation passed");
}
catch (Exception ex)
{
    Log.Error(ex, "Configuration validation failed");
    // Don't throw in production, just log the error
    if (builder.Environment.IsDevelopment())
    {
        throw;
    }
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// Add Application layer services
builder.Services.AddApplication();

// Add Infrastructure layer services
builder.Services.AddInfrastructure(builder.Configuration);

// Add Authentication
// Environment variables take precedence over appsettings
var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException("JWT Secret not configured. Set JWT_SECRET_KEY environment variable.");
var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? builder.Configuration["Jwt:Issuer"]
    ?? "ActivoosCRM";
var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? builder.Configuration["Jwt:Audience"]
    ?? "ActivoosCRM";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
})
.AddGoogle(options =>
{
    var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
        ?? builder.Configuration["Authentication:Google:ClientId"];
    var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
        ?? builder.Configuration["Authentication:Google:ClientSecret"];

    // Only configure Google OAuth if credentials are provided
    if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret) &&
        googleClientId != "your-google-client-id.apps.googleusercontent.com" &&
        googleClientSecret != "your-google-client-secret")
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;

        // Configure callback path
        options.CallbackPath = "/signin-google";

        // Request additional user information scopes
        options.Scope.Add("profile");
        options.Scope.Add("email");

        // Save tokens for later use
        options.SaveTokens = true;
    }
    else
    {
        // Log warning that Google OAuth is not configured
        Log.Warning("Google OAuth is not properly configured. Google authentication will not be available.");
    }
});

builder.Services.AddAuthorization();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ActivoosCRM API",
        Version = "v1.0.0",
        Description = "Production-ready CRM API for activity booking and customer management",
        Contact = new OpenApiContact
        {
            Name = "ActivoosCRM Support",
            Email = "support@activooscrm.com"
        }
    });


    // Add JWT Authentication to Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                },
                Scheme = "oauth2",
                Name = "Bearer",
                In = ParameterLocation.Header
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ActivoosCRM API v1");
        options.RoutePrefix = string.Empty; // Swagger at root
    });
}

// Use HTTPS redirection only if HTTPS URLs are configured
var httpsUrl = builder.Configuration["ASPNETCORE_HTTPS_PORT"] ??
               builder.Configuration["Https:Port"] ??
               builder.Configuration.GetSection("Kestrel:Endpoints:Https").Value;

if (!string.IsNullOrEmpty(httpsUrl))
{
    app.UseHttpsRedirection();
}

app.UseSerilogRequestLogging();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/api/health");

// Apply database migrations with proper error handling
try
{
    await ApplyDatabaseMigrationsAsync(app);
    Log.Information("Database migrations completed successfully");
}
catch (Exception ex)
{
    Log.Error(ex, "Database migration failed, but continuing startup");
    // Don't fail startup due to DB issues in production
}

Log.Information("Starting ActivoosCRM API");

try
{
    Log.Information("About to call app.Run()");
    app.Run();
    Log.Information("app.Run() completed normally");
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application startup failed");
    throw;
}

// Method to handle database migrations
static async Task ApplyDatabaseMigrationsAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();

        // Check if database can be connected
        var canConnect = await context.Database.CanConnectAsync();
        if (!canConnect)
        {
            logger.LogError("Cannot connect to the database. Please ensure the database server is running.");
            throw new InvalidOperationException("Database connection failed");
        }

        // Get pending migrations
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        var pendingMigrationsList = pendingMigrations.ToList();

        if (pendingMigrationsList.Any())
        {
            logger.LogInformation("Found {Count} pending migrations. Applying...", pendingMigrationsList.Count);

            foreach (var migration in pendingMigrationsList)
            {
                logger.LogInformation("Pending migration: {Migration}", migration);
            }

            // Apply migrations
            await context.Database.MigrateAsync();

            logger.LogInformation("Database migrations applied successfully");
        }
        else
        {
            logger.LogInformation("Database is up to date. No pending migrations.");
        }

        // Log applied migrations
        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        logger.LogInformation("Total applied migrations: {Count}", appliedMigrations.Count());
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating the database. Application startup will continue, but database operations may fail.");

        if (app.Environment.IsDevelopment())
        {
            logger.LogWarning("To manually apply migrations, run: dotnet ef database update --project src/ActivoosCRM.Infrastructure");
        }

        // In production, you might want to stop the application if migrations fail
        if (!app.Environment.IsDevelopment())
        {
            // In production, just log but don't throw to allow app to start
            logger.LogError("Database migration failed in production environment. Manual intervention may be required.");
        }
    }
}
