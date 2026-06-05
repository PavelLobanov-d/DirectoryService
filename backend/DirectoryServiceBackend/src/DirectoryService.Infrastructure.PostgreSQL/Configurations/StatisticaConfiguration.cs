using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations
{
    internal class StatisticaConfiguration : IEntityTypeConfiguration<Statistica>
    {
        public void Configure(EntityTypeBuilder<Statistica> builder)
        {
            builder.ToTable("statistics");

            builder.HasKey(v => v.Id).HasName("PK_statistics");

            builder.Property(v => v.dateTime)
                .HasConversion(v => v.ToUniversalTime(), v => v);
        }
    }
}
