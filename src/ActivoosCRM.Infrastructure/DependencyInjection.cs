using ActivoosCRM.Application.Common.Interfaces;
using ActivoosCRM.Application.Common.Models;
using ActivoosCRM.Infrastructure.Persistence;
using ActivoosCRM.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ActivoosCRM.Infrastructure;

/// <summary>
/// Infrastructure dependency injection extensions
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Add infrastructure services to the container
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database - Environment variables take precedence
        var connectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            ?? BuildConnectionStringFromEnvironment()
            ?? configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Database connection string not configured. Set DATABASE_CONNECTION_STRING or individual DATABASE_* environment variables.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                connectionString,
                b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName)));

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // HTTP Context Accessor for current user tracking
        services.AddHttpContextAccessor();

        // Email Configuration
        services.Configure<EmailSettings>(configuration.GetSection("Email"));

        // Services
        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IEmailService, EmailService>();

        // Register payment gateway services
        services.AddHttpClient("Razorpay")
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(30);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            });
        services.AddScoped<IRazorpayService, RazorpayService>();

        return services;
    }

    /// <summary>
    /// Build connection string from individual environment variables
    /// </summary>
    private static string? BuildConnectionStringFromEnvironment()
    {
        var host = Environment.GetEnvironmentVariable("DATABASE_HOST");
        var port = Environment.GetEnvironmentVariable("DATABASE_PORT");
        var database = Environment.GetEnvironmentVariable("DATABASE_NAME");
        var username = Environment.GetEnvironmentVariable("DATABASE_USER");
        var password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");

        if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(database))
        {
            return null;
        }

        return $"Host={host};Port={port ?? "5432"};Database={database};Username={username};Password={password}";
    }
}