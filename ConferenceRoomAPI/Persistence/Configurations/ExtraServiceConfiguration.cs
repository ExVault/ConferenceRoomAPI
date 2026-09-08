using ConferenceRoomAPI.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ConferenceRoomAPI.Persistence.Configurations;

public class ExtraServiceConfiguration : IEntityTypeConfiguration<ExtraService>
{
    public void Configure(EntityTypeBuilder<ExtraService> builder)
    {
        builder.ToTable("ExtraServices", tableBuilder =>
            tableBuilder.HasCheckConstraint("CK_ExtraServices_Price", "CAST(\"Price\" AS NUMERIC) >= 0"));

        builder.HasKey(extraService => extraService.Id);
        builder.Property(extraService => extraService.Name).HasMaxLength(100).IsRequired();
        builder.HasIndex(extraService => extraService.Name).IsUnique();
        
        builder.Property(extraService => extraService.Price).HasPrecision(18, 2);
    }
}
