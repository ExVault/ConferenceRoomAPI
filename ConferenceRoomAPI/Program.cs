using ConferenceRoomAPI.Persistence;
using ConferenceRoomAPI.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);

var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
                   ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connStr)
        .UseSeeding(DbSeeder.Seed)
        .UseAsyncSeeding(DbSeeder.SeedAsync));

builder.Services.AddOpenApi();

var app = builder.Build();

// Apply pending migrations on startup, convenient for this project's scope.
// Production migrations should be deployed separately using a bundle or reviewed SQL script.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();
