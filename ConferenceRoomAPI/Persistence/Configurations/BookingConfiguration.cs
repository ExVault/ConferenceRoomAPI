using ConferenceRoomAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomAPI.Persistence.Configurations;

public class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> builder)
    {
        builder.ToTable("Bookings", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Bookings_TimeRange", "\"EndUtc\" > \"StartUtc\"");

            tableBuilder.HasCheckConstraint(
                "CK_Bookings_HourlyRateSnapshot",
                "CAST(\"HourlyRateSnapshot\" AS NUMERIC) >= 0");

            tableBuilder.HasCheckConstraint("CK_Bookings_TotalPrice", "CAST(\"TotalPrice\" AS NUMERIC) >= 0");
        });

        builder.HasKey(booking => booking.Id);
        builder.Property(booking => booking.HourlyRateSnapshot).HasPrecision(18, 2);
        builder.Property(booking => booking.TotalPrice).HasPrecision(18, 2);

        // Availability checks will often search bookings by room and time range.
        builder.HasIndex(booking => new { booking.RoomId, booking.StartUtc, booking.EndUtc });

        builder.HasOne(booking => booking.Room)
            .WithMany(room => room.Bookings)
            .HasForeignKey(booking => booking.RoomId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}