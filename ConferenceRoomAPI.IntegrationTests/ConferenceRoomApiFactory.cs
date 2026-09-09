using ConferenceRoomAPI.Persistence;
using ConferenceRoomAPI.Persistence.Seeding;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ConferenceRoomAPI.IntegrationTests;

public class ConferenceRoomApiFactory : WebApplicationFactory<Program>
{
    // A SQLite in memory db exists only while this connection remains open
    private readonly SqliteConnection _dbConn = new("Data Source=:memory:");

    public async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Reapply migrations instead of using EnsureCreated so tests also verify
        // that the committed migration chain can build the database
        await db.Database.EnsureDeletedAsync();
        await db.Database.MigrateAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Avoid Windows Event Log permission errors from breaking the tests
        builder.ConfigureLogging(logging => logging.ClearProviders());

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<AppDbContext>();
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<AppDbContext>>();

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_dbConn)
                    .UseSeeding(DbSeeder.Seed)
                    .UseAsyncSeeding(DbSeeder.SeedAsync));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        _dbConn.Open();
        var host = base.CreateHost(builder);

        try
        {
            using var scope = host.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.Migrate();
            return host;
        }
        catch
        {
            host.Dispose();
            _dbConn.Dispose();
            throw;
        }
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _dbConn.Dispose();
        }
    }
}
