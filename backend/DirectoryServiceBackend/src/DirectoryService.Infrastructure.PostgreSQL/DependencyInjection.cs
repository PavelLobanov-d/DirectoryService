using DirectoryService.Core.DepartmentChiefPositions;
using DirectoryService.Core.DepartmentLocations;
using DirectoryService.Core.DepartmentPositions;
using DirectoryService.Core.Departments;
using DirectoryService.Core.Locations;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Core.Statistics;
using DirectoryService.Infrastructure.PostgreSQL.Database;
using DirectoryService.Infrastructure.PostgreSQL.Repositories;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Infrastructure.PostgreSQL;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        return services
            .AddScoped<ILocationsRepository, LocationsRepositoryDapper>()
            //.AddScoped<ILocationsRepository, LocationsRepository>()
            .AddScoped<IPositionMatrixRepository, PositionMatrixRepository>()
            .AddScoped<IDepartmentsChiefPositionRepository, DepartmentsChiefPositionRepository>()
            .AddScoped<IDepartmentsRepository, DepartmentsRepository>()
            .AddScoped<IDepartmentPositionsRepository, DepartmentPositionsRepository>()
            .AddScoped<IDepartmentLocationsRepository, DepartmentLocationsRepository>()
            .AddScoped<IStatisticsRepository, StatisticsRepository>();
    }
}
