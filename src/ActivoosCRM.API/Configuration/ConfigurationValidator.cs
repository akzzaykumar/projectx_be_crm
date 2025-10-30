namespace ActivoosCRM.API.Configuration;

/// <summary>
/// Validates required configuration settings on application startup
/// </summary>
public static class ConfigurationValidator
{
    /// <summary>
    /// Validates all required configuration values
    /// </summary>
    public static void ValidateConfiguration(IConfiguration configuration)
    {
        var errors = new List<string>();

        // Database
        var dbConnectionString = Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
            ?? BuildConnectionStringFromEnvironment()
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(dbConnectionString))
        {
            errors.Add("Database connection not configured. Set DATABASE_CONNECTION_STRING or individual DATABASE_* environment variables.");
        }

        // JWT
        var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? configuration["Jwt:SecretKey"];

        if (string.IsNullOrEmpty(jwtSecret))
        {
            errors.Add("JWT Secret Key not configured. Set JWT_SECRET_KEY environment variable.");
        }
        else if (jwtSecret.Length < 32)
        {
            errors.Add($"JWT Secret Key is too short ({jwtSecret.Length} characters). Minimum 32 characters required for security.");
        }

        // Google OAuth (warn only if not configured)
        var googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
            ?? configuration["Authentication:Google:ClientId"];
        var googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET")
            ?? configuration["Authentication:Google:ClientSecret"];

        if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(googleClientSecret))
        {
            Console.WriteLine("⚠️  WARNING: Google OAuth is not configured. Google Sign-In will not work.");
            Console.WriteLine("    Set GOOGLE_CLIENT_ID and GOOGLE_CLIENT_SECRET environment variables to enable it.");
        }

        // Throw if critical errors found
        if (errors.Any())
        {
            var errorMessage = "Configuration validation failed:\n" + string.Join("\n", errors.Select(e => $"  - {e}"));
            throw new InvalidOperationException(errorMessage);
        }

        Console.WriteLine("✅ Configuration validation passed");
    }

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
