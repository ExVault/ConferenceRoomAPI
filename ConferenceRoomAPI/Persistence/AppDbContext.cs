using ConferenceRoomAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConferenceRoomAPI.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<ExtraService> ExtraServices => Set<ExtraService>();
    public DbSet<RoomExtraService> RoomExtraServices => Set<RoomExtraService>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingExtraService> BookingExtraServices => Set<BookingExtraService>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
