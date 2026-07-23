using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Infrastructure.PostgreSQL.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations;

internal class DepartmentChiefPositionConfiguration : IEntityTypeConfiguration<DepartmentChiefPosition>
{
    public void Configure(EntityTypeBuilder<DepartmentChiefPosition> builder)
    {
        builder.ToTable("department_chiefpositions");

        builder.HasKey(v => v.DepartmentId).HasName("PK_department_chiefpositions");

        builder.Property(v => v.DepartmentId)
            .HasConversion<DepartmentIdConverter>()
            .IsRequired()
            .HasColumnName("department_id");

        builder.Property(v => v.PositionMatrixId)
            .HasConversion<PositionMatrixIdConverter>()
            .IsRequired()
            .HasColumnName("positionmatrix_id");
        
        builder.HasOne(dcp => dcp.Department)
            .WithOne(d => d.DepartmentChiefPosition)
            .HasForeignKey<DepartmentChiefPosition>(dcp => dcp.DepartmentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dcp => dcp.PositionMatrix)
            .WithMany(pm => pm.DepartmentChiefPositions)
            .HasForeignKey(v => v.PositionMatrixId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
