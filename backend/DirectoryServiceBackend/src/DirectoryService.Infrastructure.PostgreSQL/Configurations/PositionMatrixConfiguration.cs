using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Infrastructure.PostgreSQL.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations;

internal class PositionMatrixConfiguration : IEntityTypeConfiguration<PositionMatrix>
{
    public void Configure(EntityTypeBuilder<PositionMatrix> builder)
    {
        builder.ToTable("positionsmatrix");

        builder.HasKey(v => v.Id).HasName("PK_positionmatrix");
        builder.Property(v => v.Id)
            .HasConversion<PositionMatrixIdConverter>()
            .HasColumnName("id");

        builder.Property(v => v.Name)
            .HasConversion<PositionNameConverter>()
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
            .HasConversion<PositionMatrixIdConverter>()
            .HasColumnName("parentid")
            .IsRequired(false);

        builder.HasMany(v => v.Childs)
            .WithOne(v => v.Parent)
            .HasForeignKey(v => v.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Navigation(v => v.Childs)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(pm => pm.DepartmentChiefPositions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(pm => pm.DepartmentPositions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
