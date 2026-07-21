using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.DepartmentLocations;
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
    public DbSet<PositionMatrix> PositionsMatrix { get; }
    public DbSet<Statistica> Statistics { get; }
    public DbSet<Department> Departments { get; }
    public DbSet<DepartmentPosition> DepartmentPositions { get; }
    public DbSet<DepartmentLocation> DepartmentLocations { get; }
    public DbSet<DepartmentChiefPosition> DepartmentChiefPositions { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
