using ConferenceRoomAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomAPI.Persistence.Configurations;

public class BookingExtraServiceConfiguration : IEntityTypeConfiguration<BookingExtraService>
{
    public void Configure(EntityTypeBuilder<BookingExtraService> builder)
    {
        builder.ToTable("BookingExtraServices", tableBuilder =>
            tableBuilder.HasCheckConstraint(
                "CK_BookingExtraServices_PriceSnapshot",
                "CAST(\"PriceSnapshot\" AS NUMERIC) >= 0"));

        builder.HasKey(bookingExtraService =>
            new { bookingExtraService.BookingId, bookingExtraService.ExtraServiceId });
        builder.Property(bookingExtraService => bookingExtraService.PriceSnapshot).HasPrecision(18, 2);

        builder.HasOne(bookingExtraService => bookingExtraService.Booking)
            .WithMany(booking => booking.ExtraServices)
            .HasForeignKey(bookingExtraService => bookingExtraService.BookingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(bookingExtraService => bookingExtraService.ExtraService)
            .WithMany(extraService => extraService.Bookings)
            .HasForeignKey(bookingExtraService => bookingExtraService.ExtraServiceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
