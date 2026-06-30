using DirectoryService.Core;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Infrastructure.PostgreSQL;
using System.Runtime.CompilerServices;

namespace DirectoryService.Web;

public static class DependencyInjection
{
    public static IServiceCollection AddProgramDependencies(this IServiceCollection services)
    {
        return services
            .AddWebDependencies()
            .AddCore()
            .AddInfrastructure();
    }

    private static IServiceCollection AddWebDependencies(this IServiceCollection services)
    {
        services.AddOpenApi();
        services.AddControllers();
        services.AddHealthChecks();

        return services;
    }
}
