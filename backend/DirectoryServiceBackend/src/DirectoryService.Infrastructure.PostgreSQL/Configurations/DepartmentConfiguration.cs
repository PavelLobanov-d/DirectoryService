using DirectoryService.Core.Departments;
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

        builder.Property(v => v.ParentId)
            .HasConversion<DepartmentIdConverter>()
            .HasColumnName("parentid")
            .IsRequired(false);

        builder.Property(v => v.DeletedAt)
            .HasColumnName("deleted_at");

        builder.HasQueryFilter(v => v.DeletedAt == null);

        builder.Property(v => v.Path)
            .HasConversion<DepartmentPathLTreeConverter>()
            .HasColumnType("ltree")
            .HasColumnName(DepartmentPathIndex.ColumnName)
            .IsRequired();

        builder.HasIndex(v => v.Path)
            .HasDatabaseName(DepartmentPathIndex.IndexName)
            .HasMethod("gist");

        builder.HasMany(v => v.Childs)
            .WithOne(v => v.Parent)
            .HasForeignKey(v => v.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Navigation(v => v.Childs)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(v => v.DepartmentPositions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(v => v.DepartmentLocations)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}