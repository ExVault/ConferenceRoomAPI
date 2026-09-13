using ConferenceRoomAPI.Configuration;
using ConferenceRoomAPI.Features.Bookings;
using ConferenceRoomAPI.Features.Reports;
using ConferenceRoomAPI.Features.Rooms;
using ConferenceRoomAPI.Persistence;
using ConferenceRoomAPI.Persistence.Seeding;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);

var dbPath = builder.Configuration.GetValue<string>("SQLiteDatabasePath")
             ?? throw new InvalidOperationException("'SQLiteDatabasePath' is not configured.");

var connStr = new SqliteConnectionStringBuilder { DataSource = dbPath }.ToString();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connStr)
        .UseSeeding(DbSeeder.Seed) // EF CLI tooling uses synchronous delegate
        .UseAsyncSeeding(DbSeeder.SeedAsync));

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddSingleton(builder.Configuration.LoadBookingRules());

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.RespectNullableAnnotations = true;
    options.SerializerOptions.RespectRequiredConstructorParameters = true;
});

builder.Services.AddRoomFeatures();
builder.Services.AddBookingFeatures();
builder.Services.AddReportFeatures();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    // Automatically create database file and apply migrations in dev environment, for convenience.
    // Production migrations should be deployed separately using a bundle or reviewed SQL script.
    SqliteDatabaseFile.EnsureCreated(dbPath, builder.Environment.ContentRootPath);
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();


    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "Conference Room API - Swagger UI";
        options.SwaggerEndpoint("/openapi/v1.json", "Conference Room API v1");
    });

    app.MapScalarApiReference(options => { options.Title = "Conference Room API - Scalar"; });
}
else
{
    app.UseExceptionHandler();
}

app.UseHttpsRedirection();

app.MapRoomEndpoints();
app.MapBookingEndpoints();
app.MapReportEndpoints();

app.Run();