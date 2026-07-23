using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Infrastructure.PostgreSQL.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations;

internal class DepartmentConfiguration: IEntityTypeConfiguration <Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(v => v.Id).HasName("PK_department");
        builder.Property(v => v.Id)
            .HasConversion<DepartmentIdConverter>()
            .HasColumnName("id");

        builder.Property(v => v.Name)
            .HasConversion<DepartmentNameConverter>()
            .HasMaxLength(100)
            .HasColumnName("name")
            .IsRequired();
        
        builder.Property(v => v.Slug)
            .HasConversion<SlugConverter>()
            .HasMaxLength(50)
            .HasColumnName("slug")
            .IsRequired();

        builder.Property(v => v.PathSlug)
            .HasConversion<PathSlugConverter>()
            .HasMaxLength(100)
            .HasColumnName("pathslug")
            .IsRequired(false);

        builder.Property(v => v.ParentId)
            .HasConversion<DepartmentIdConverter>()
            .HasColumnName("parentid")
            .IsRequired(false);

        builder.HasMany(v => v.Childs)
            .WithOne(v => v.Parent)
            .HasForeignKey(v => v.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
        /*
        builder.HasMany(v => v.DepartmentPositions)
            .WithOne(dp => dp.Department)
            .HasForeignKey(v => v.DepartmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);
        */
        builder.HasMany(v => v.DepartmentLocations)
            .WithOne(dl => dl.Department)
            .HasForeignKey(dl => dl.DepartmentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dcp => dcp.DepartmentChiefPosition)
            .WithOne(v => v.Department)
            .HasForeignKey<DepartmentChiefPosition>(v => v.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(v => v.Childs)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(v => v.DepartmentPositions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(v => v.DepartmentLocations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}