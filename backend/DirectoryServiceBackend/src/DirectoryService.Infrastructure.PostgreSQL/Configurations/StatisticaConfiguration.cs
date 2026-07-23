using DirectoryService.Domain.Statistics;
using DirectoryService.Infrastructure.PostgreSQL.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DirectoryService.Infrastructure.PostgreSQL.Configurations
{
    internal class StatisticaConfiguration : IEntityTypeConfiguration<Statistica>
    {
        public void Configure(EntityTypeBuilder<Statistica> builder)
        {
            builder.ToTable("statistics");

            builder.HasKey(v => v.Id).HasName("PK_statistics");

            builder.Property(v => v.dateTime)
                .HasConversion<DateTimeUtcConverter>();
        }
    }
}
