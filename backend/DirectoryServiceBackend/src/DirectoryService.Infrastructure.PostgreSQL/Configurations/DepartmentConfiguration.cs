using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations;

internal class DepartmentConfiguration: IEntityTypeConfiguration <Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(v => v.Id).HasName("PK_department");
        builder.Property(v => v.Id)
            .HasConversion(v => v.Value, id => new DepartmentId(id))
            .HasColumnName("id");

        builder.Property(v => v.Name)
            .HasConversion(v => v.Value, Name => DepartmentName.Create(Name))
            .HasMaxLength(100)
            .HasColumnName("name")
            .IsRequired();
        
        builder.Property(v => v.Slug)
            .HasConversion(v => v.Value, slug => Slug.Create(slug))
            .HasMaxLength(50)
            .HasColumnName("slug")
            .IsRequired();

        builder.Property(v => v.PathSlug)
            .HasConversion(v => v.Value, pathslug => PathSlug.Create(Slug.Create(pathslug)))
            .HasMaxLength(100)
            .HasColumnName("pathslug")
            .IsRequired(false);

        builder.Property(v => v.ParentId)
            .HasConversion(v => v.Value, parentid => new DepartmentId(parentid))
            .HasColumnName("parentid")
            .IsRequired(false);

        builder.HasMany(v => v.Childs)
            .WithOne(v => v.Parent)
            .HasForeignKey(v => v.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(v => v.DepartmentChiefPosition)
            .WithOne(dp => dp.Department)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(v => v.DepartmentLocations)
            .WithOne(dl => dl.Department)
            .HasForeignKey(dl => dl.DepartmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
