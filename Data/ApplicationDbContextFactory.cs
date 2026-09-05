using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CvManagementSystem.Data;

public class ApplicationDbContextFactory
    : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var basePath = Directory.GetCurrentDirectory();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile(
                $"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}.json",
                optional: true)
            .AddUserSecrets<ApplicationDbContextFactory>(optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection was not found.");
        }

        var npgsqlBuilder =
            new NpgsqlConnectionStringBuilder(connectionString);

        Console.WriteLine();
        Console.WriteLine("=== EF DATABASE CONFIGURATION ===");
        Console.WriteLine($"Host:     {npgsqlBuilder.Host}");
        Console.WriteLine($"Port:     {npgsqlBuilder.Port}");
        Console.WriteLine($"Database: {npgsqlBuilder.Database}");
        Console.WriteLine($"Username: {npgsqlBuilder.Username}");
        Console.WriteLine("Password: [hidden]");
        Console.WriteLine("=================================");
        Console.WriteLine();

        var optionsBuilder =
            new DbContextOptionsBuilder<ApplicationDbContext>();

        optionsBuilder.UseNpgsql(connectionString);

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}