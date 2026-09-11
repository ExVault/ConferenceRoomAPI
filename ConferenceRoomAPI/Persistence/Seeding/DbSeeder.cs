using ConferenceRoomAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomAPI.Persistence.Seeding;

public static class DbSeeder
{
    // Create fresh entities each time because seeding can run against different DbContext instances.
    private static Room[] InitialRooms =>
    [
        new() { Name = "Зал А", Capacity = 50, HourlyRate = 2000m },
        new() { Name = "Зал B", Capacity = 100, HourlyRate = 3500m },
        new() { Name = "Зал C", Capacity = 30, HourlyRate = 1500m }
    ];

    private static ExtraService[] InitialServices =>
    [
        new() { Name = "Проєктор", Price = 500m },
        new() { Name = "Wi-Fi", Price = 300m },
        new() { Name = "Звук", Price = 700m }
    ];

    public static void Seed(DbContext db, bool _)
    {
        if (!db.Set<Room>().Any())
        {
            db.Set<Room>().AddRange(InitialRooms);
        }

        if (!db.Set<ExtraService>().Any())
        {
            db.Set<ExtraService>().AddRange(InitialServices);
        }

        if (db.ChangeTracker.HasChanges())
        {
            db.SaveChanges();
        }
    }

    public static async Task SeedAsync(DbContext db, bool _, CancellationToken ct)
    {
        if (!await db.Set<Room>().AnyAsync(ct))
        {
            db.Set<Room>().AddRange(InitialRooms);
        }

        if (!await db.Set<ExtraService>().AnyAsync(ct))
        {
            db.Set<ExtraService>().AddRange(InitialServices);
        }

        if (db.ChangeTracker.HasChanges())
        {
            await db.SaveChangesAsync(ct);
        }
    }
}