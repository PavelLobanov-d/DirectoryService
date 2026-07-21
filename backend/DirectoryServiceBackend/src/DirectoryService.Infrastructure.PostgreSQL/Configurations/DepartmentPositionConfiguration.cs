using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Infrastructure.PostgreSQL.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations;

internal class DepartmentPositionConfiguration : IEntityTypeConfiguration<DepartmentPosition>
{
    public void Configure(EntityTypeBuilder<DepartmentPosition> builder)
    {
        builder.ToTable("department_positions");

        builder.HasKey(v => v.Id).HasName("PK_department_positions");
        builder.Property(v => v.Id)
            .HasConversion<DepartmentPositionIdConverter>()
            .HasColumnName("id");

        builder.Property(v => v.DepartmentId)
            .HasConversion<DepartmentIdConverter>()
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(v => v.PositionMatrixId)
            .HasConversion<PositionMatrixIdConverter>()
            .HasColumnName("position_id")
            .IsRequired();

        builder.HasOne(dp => dp.Department)
            .WithMany(v => v.DepartmentPositions)
            .HasForeignKey(v => v.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne(dp => dp.PositionMatrix)
            .WithMany(v => v.DepartmentPositions)
            .HasForeignKey(v => v.PositionMatrixId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
