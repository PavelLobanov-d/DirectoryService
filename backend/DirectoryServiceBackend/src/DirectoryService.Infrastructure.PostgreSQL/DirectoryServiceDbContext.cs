using DirectoryService.Core.Database;
using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.Statistics;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;


namespace DirectoryService.Infrastructure.PostgreSQL;

public class DirectoryServiceDbContext : DbContext, IDirectoryServiceDbContext
{
    
    private readonly string _connectionString = null!;
    public DirectoryServiceDbContext(DbContextOptions<DirectoryServiceDbContext> options)
    : base(options)
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        base.OnConfiguring(optionsBuilder);

        if (!optionsBuilder.IsConfigured && !string.IsNullOrEmpty(_connectionString))
        {
            optionsBuilder.UseNpgsql(_connectionString);
        }

        if (!optionsBuilder.IsConfigured)
        {
            dotenv.net.DotEnv.Load();
            string? envConnectionString = Environment.GetEnvironmentVariable("DIRECTORY_SERVICE_CONNECTIONSTRING");

            if (!string.IsNullOrEmpty(envConnectionString))
            {
                optionsBuilder.UseNpgsql(envConnectionString);
            }
        }

        if (!optionsBuilder.IsConfigured)
        {
            var connectionString = "User Id=postgres;Password=postgres;Host=localhost;Port=5454;Database=directory_service_db;";
            optionsBuilder.UseNpgsql(connectionString);
        }
        
        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.LogTo(Console.WriteLine);
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
