using ConferenceRoomAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomAPI.Persistence.Configurations;

public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("CK_Rooms_Capacity", "\"Capacity\" > 0");

            // SQLite stores decimals as text, so cast them before comparing numeric values.
            tableBuilder.HasCheckConstraint("CK_Rooms_HourlyRate", "CAST(\"HourlyRate\" AS NUMERIC) >= 0");
        });

        builder.HasKey(room => room.Id);
        
        builder.Property(room => room.Name).HasMaxLength(100).IsRequired();
        //.UseCollation("NOCASE")
        // sqlite built-in NOCASE handles only ascii characters and does not provide
        // unicode case-insensitive comparison
        
        builder.HasIndex(room => room.Name).IsUnique();
        
        builder.Property(room => room.HourlyRate).HasPrecision(18, 2);
        builder.Property(room => room.IsActive).HasDefaultValue(true);
    }
}
