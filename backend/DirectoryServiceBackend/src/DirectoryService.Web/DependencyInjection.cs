using DirectoryService.Core;
using DirectoryService.Infrastructure.PostgreSQL;
using DirectoryService.Infrastructure.PostgreSQL.SoftDelete;
using Shared.Framework.Hosting;
using Shared.Framework.SoftDelete;

namespace DirectoryService.Web;

public static class DependencyInjection
{
    public static IServiceCollection AddProgramDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        return services
            .AddSoftDeleteCleanup(configuration, DirectoryServiceSoftDeleteCleanupPlan.Create())
            .AddSharedSerilog(configuration, "DirectoryServiceBackend")
            .AddSharedWebDefaults()
            .AddCore()
            .AddInfrastructure();
    }
}
