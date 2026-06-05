using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations;

internal class PositionMatrixConfiguration : IEntityTypeConfiguration<PositionMatrix>
{
    public void Configure(EntityTypeBuilder<PositionMatrix> builder)
    {
        builder.ToTable("positionsmatrix");

        builder.HasKey(v => v.Id).HasName("PK_positionmatrix");
        builder.Property(v => v.Id)
            .HasConversion(v => v.Value, id => new PositionMatrixId(id))
            .HasColumnName("id");

        builder.Property(v => v.Name)
            .HasConversion(v => v.Value, Name => PositionName.Create(Name))
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
            .HasConversion(v => v.Value, parentid => new PositionMatrixId(parentid))
            .HasColumnName("parentid")
            .IsRequired(false);

        builder.HasMany(v => v.Childs)
            .WithOne(v => v.Parent)
            .HasForeignKey(v => v.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
