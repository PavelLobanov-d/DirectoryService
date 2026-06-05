using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.PositionsMatrix;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Runtime.Intrinsics.Arm;
using System.Text;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations;

internal class DepartmentPositionConfiguration : IEntityTypeConfiguration<DepartmentPosition>
{
    public void Configure(EntityTypeBuilder<DepartmentPosition> builder)
    {
        builder.ToTable("department_positions");

        builder.HasKey(v => v.Id).HasName("PK_department_positions");
        builder.Property(v => v.Id)
            .HasConversion(v => v.Value, id => new DepartmentPositionId(id))
            .HasColumnName("id");

        builder.Property(v => v.DepartmentId)
            .HasConversion(v => v.Value, departmentId => new DepartmentId(departmentId))
            .HasColumnName("department_id")
            .IsRequired();

        builder.Property(v => v.PositionMatrixId)
            .HasConversion(v => v.Value, positionMatrixId => new PositionMatrixId(positionMatrixId))
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
