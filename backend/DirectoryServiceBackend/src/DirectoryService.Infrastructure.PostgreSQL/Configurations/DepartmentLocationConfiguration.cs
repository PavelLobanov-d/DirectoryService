using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Infrastructure.PostgreSQL.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations;

internal class DepartmentLocationConfiguration : IEntityTypeConfiguration<DepartmentLocation>
{
    public void Configure(EntityTypeBuilder<DepartmentLocation> builder)
    {
        builder.ToTable("department_locations");

        builder.HasKey(v => v.Id).HasName("PK_department_locations");
        builder.Property(v => v.Id)
            .HasConversion<DepartmentLocationIdConverter>()
            .IsRequired()
            .HasColumnName("id");

        builder.Property(v => v.DepartmentId)
            .HasConversion<DepartmentIdConverter>()
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(v => v.LocationId)
            .HasConversion<LocationIdConverter>()
            .HasColumnName("location_id")
            .IsRequired();

        builder.HasOne(v => v.Department)
            .WithMany(v => v.DepartmentLocations)
            .HasForeignKey(v => v.DepartmentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(v => v.Location)
            .WithMany(v => v.DepartmentLocations)
            .HasForeignKey(v => v.LocationId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
