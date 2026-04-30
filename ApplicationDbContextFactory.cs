using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace TEMApps.Data;
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Set up the configuration for the DbContext
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

        var configuration = new ConfigurationBuilder()
       .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "..", "TEMApps"))
       .AddJsonFile("appsettings.Development.json", optional: false)
       .Build();

        // Use the connection string from your configuration
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        // Configure the DbContext with the correct provider (e.g., Npgsql for PostgreSQL)
        // optionsBuilder.UseNpgsql(connectionString);
        optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Database=tkmtemdevdatabase;Username=postgres;Password=prapti@1610");

        return new ApplicationDbContext(optionsBuilder.Options);
    }
    //public ApplicationDbContext CreateDbContext(string[] args)
    //{
    //    //// Check current environment (Development / Production)
    //    //var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
    //    //                  ?? "Development";

    //    //var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

    //    //// Pointing to main project folder (TEMApps)
    //    //var basePath = Path.Combine(Directory.GetCurrentDirectory(), "..", "TEMApps");

    //    //// Load the correct appsettings based on environment
    //    //var configuration = new ConfigurationBuilder()
    //    //   .SetBasePath(basePath)
    //    //   .AddJsonFile($"appsettings.{environment}.json", optional: false)
    //    //   .Build();

    //    //var connectionString = configuration.GetConnectionString("DefaultConnection");

    //    //optionsBuilder.UseNpgsql(connectionString);

    //    //return new ApplicationDbContext(optionsBuilder.Options);

    //}
}