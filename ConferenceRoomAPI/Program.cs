using ConferenceRoomAPI.Common.OpenApi;
using ConferenceRoomAPI.Features.Rooms;
using ConferenceRoomAPI.Persistence;
using ConferenceRoomAPI.Persistence.Seeding;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true);

var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
              ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(connStr)
        .UseSeeding(DbSeeder.Seed) // EF CLI tooling uses synchronous delegate
        .UseAsyncSeeding(DbSeeder.SeedAsync));

builder.Services.AddOpenApi(options => options.AddSchemaTransformer<StrictDateTimeOffsetTransformer>());
builder.Services.AddProblemDetails();

builder.Services.AddRoomFeatures();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
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

app.Run();
