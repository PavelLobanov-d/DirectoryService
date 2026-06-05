using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations;


internal class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("locations");

        builder.HasKey(v => v.Id).HasName("PK_locations");
        builder.Property(v => v.Id)
            .HasConversion(v => v.Value, id => new LocationId(id))
            .HasColumnName("id");

        builder.Property(v => v.Name)
            .HasConversion(v => v.Value, Name => LocationName.Create(Name))
            .HasMaxLength(100)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(v => v.Address)
            .HasConversion(v => v.Value, address => Address.Create(address))
            .HasMaxLength(200)
            .HasColumnName("address")
            .IsRequired();
    }
}