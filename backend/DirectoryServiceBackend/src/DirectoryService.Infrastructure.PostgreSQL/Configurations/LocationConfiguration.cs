using DirectoryService.Domain.Locations;
using DirectoryService.Infrastructure.PostgreSQL.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations;


internal class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");

        builder.HasKey(v => v.Id).HasName("PK_locations");
        builder.Property(v => v.Id)
            .HasConversion<LocationIdConverter>()
            .HasColumnName("id");

        builder.Property(v => v.Name)
            .HasConversion<LocationNameConverter>()
            .HasMaxLength(100)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(v => v.Address)
            .HasConversion<AddressConverter>()
            .HasMaxLength(200)
            .HasColumnName("address")
            .IsRequired();
    }
}