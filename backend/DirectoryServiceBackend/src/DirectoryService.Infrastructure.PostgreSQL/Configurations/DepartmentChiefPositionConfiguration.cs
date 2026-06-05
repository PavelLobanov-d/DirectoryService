using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.PositionsMatrix;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations;

internal class DepartmentChiefPositionConfiguration : IEntityTypeConfiguration<DepartmentChiefPosition>
{
    public void Configure(EntityTypeBuilder<DepartmentChiefPosition> builder)
    {
        builder.ToTable("department_chiefpositions");

        builder.HasKey(v => new { v.DepartmentId, v.PositionMatrixId }).HasName("PK_department_chiefpositions");

        builder.Property(v => v.DepartmentId)
            .HasConversion(v => v.Value, id => new DepartmentId(id))
            .IsRequired()
            .HasColumnName("department_id");

        builder.Property(v => v.PositionMatrixId)
            .HasConversion(v => v.Value, id => new PositionMatrixId(id))
            .IsRequired()
            .HasColumnName("positionmatrix_id");

        builder.HasOne<Department>()
            .WithOne(d => d.DepartmentChiefPosition)
            .HasForeignKey<DepartmentChiefPosition>(cp => cp.DepartmentId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<PositionMatrix>()
            .WithMany(p => p.DepartmentChiefPositions)
            .HasForeignKey(cp => cp.PositionMatrixId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}
