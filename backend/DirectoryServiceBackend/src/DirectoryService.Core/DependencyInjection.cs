using DirectoryService.Core.Departments;
using DirectoryService.Core.Locations;
using DirectoryService.Core.PositionsMatrix;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddCore(this IServiceCollection services)
    {
        services
            .AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly)
            .AddScoped<ILocationsService, LocationsService>()
            .AddScoped<IPositionMatrixService, PositionMatrixService>()
            .AddScoped<IDepartmentsService, DepartmentsService>()
            .AddScoped<IStatisticsService, StatisticsService>();

        services.AddScoped<IDepartmentsService, DepartmentsService>();

        // регистрация всех Handlers
        services.Scan(scan => scan.FromAssemblies(typeof(DependencyInjection).Assembly)
            .AddClasses(classes => classes
                .AssignableToAny(typeof(ICommandHandler<,>), typeof(ICommandHandler<>)))
            .AsSelfWithInterfaces()
            .WithScopedLifetime());
        return services;
    }
}
