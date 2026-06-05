using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Domain.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;


namespace DirectoryService.Infrastructure.PostgreSQL;

public class DirectoryServiceDbContext : DbContext
{
    private readonly string _connectionString;
    public DirectoryServiceDbContext(string connectionString)
    {
        _connectionString = connectionString;
    }
    public DirectoryServiceDbContext(DbContextOptions<DirectoryServiceDbContext> options)
    : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);
        optionsBuilder.UseNpgsql(_connectionString);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(DirectoryServiceDbContext).Assembly);
    }

    public DbSet<PositionMatrix> PositionsMatrix => Set<PositionMatrix>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<DepartmentPosition> DepartmentPositions => Set<DepartmentPosition>();
    public DbSet<DepartmentChiefPosition> DepartmentChiefPositions => Set<DepartmentChiefPosition>();
    public DbSet<DepartmentLocation> DepartmentLocations => Set<DepartmentLocation>();
    public DbSet<Statistica> Statistics => Set<Statistica>();
}
