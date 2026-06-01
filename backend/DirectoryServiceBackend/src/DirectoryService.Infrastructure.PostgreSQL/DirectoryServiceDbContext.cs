using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions;

namespace DirectoryService.Infrastructure.PostgreSQL;

public class DirectoryServiceDbContext : DbContext
{
    private readonly string _connectionString;
    public DirectoryServiceDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseNpgsql(_connectionString);
    }

    public DbSet<PositionMatrix> PositionsMatrix => Set<PositionMatrix>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<DepartmentPosition> DepartmentPositions => Set<DepartmentPosition>();
    public DbSet<DepartmentLocation> DepartmentLocations => Set<DepartmentLocation>();
    public DbSet<Statistica> Statistics => Set<Statistica>();
}
