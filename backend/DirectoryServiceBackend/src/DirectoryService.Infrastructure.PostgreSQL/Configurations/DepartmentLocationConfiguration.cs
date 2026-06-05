using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations;

internal class DepartmentLocationConfiguration : IEntityTypeConfiguration<DepartmentLocation>
{
    public void Configure(EntityTypeBuilder<DepartmentLocation> builder)
    {
        builder.ToTable("department_locations");

        builder.HasKey(v => v.Id).HasName("PK_department_locations");
        builder.Property(v => v.Id)
            .HasConversion(v => v.Value, id => new DepartmentLocationId(id))
            .IsRequired()
            .HasColumnName("id");

        builder.Property(v => v.DepartmentId)
            .HasConversion(v => v.Value, departmentid => new DepartmentId(departmentid))
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(v => v.LocationId)
            .HasConversion(v => v.Value, locationid => new LocationId(locationid))
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
