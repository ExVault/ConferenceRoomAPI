using ConferenceRoomAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomAPI.Persistence.Configurations;

public class RoomExtraServiceConfiguration : IEntityTypeConfiguration<RoomExtraService>
{
    public void Configure(EntityTypeBuilder<RoomExtraService> builder)
    {
        builder.ToTable("RoomExtraServices");
        builder.HasKey(roomExtraService => new { roomExtraService.RoomId, roomExtraService.ExtraServiceId });

        builder.HasOne(roomExtraService => roomExtraService.Room)
            .WithMany(room => room.ExtraServices)
            .HasForeignKey(roomExtraService => roomExtraService.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(roomExtraService => roomExtraService.ExtraService)
            .WithMany(extraService => extraService.Rooms)
            .HasForeignKey(roomExtraService => roomExtraService.ExtraServiceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}