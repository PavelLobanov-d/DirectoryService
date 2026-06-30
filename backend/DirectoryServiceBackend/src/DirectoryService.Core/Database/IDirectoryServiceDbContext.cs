using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Statistics;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Core.Database;

public interface IDirectoryServiceDbContext
{
    public DbSet<Location> Locations { get; }
    public DbSet<Statistica> Statistics { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
