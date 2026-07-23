using DirectoryService.Core.Locations;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Core.Departments;
using DirectoryService.Core.Statistics;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        return services
            .AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly)
            .AddScoped<ILocationsService, LocationsService>()
            .AddScoped<IPositionMatrixService, PositionMatrixService>()
            .AddScoped<IDepartmentsService, DepartmentsService>()
            .AddScoped<IStatisticsService, StatisticsService>();
    }
}
