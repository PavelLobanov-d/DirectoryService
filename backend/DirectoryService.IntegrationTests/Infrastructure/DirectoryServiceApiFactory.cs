using DirectoryService.Core.Database;
using DirectoryService.Infrastructure.PostgreSQL;
using DirectoryService.Infrastructure.PostgreSQL.Database;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DirectoryService.IntegrationTests.Infrastructure;

public sealed class DirectoryServiceApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public DirectoryServiceApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<DirectoryServiceDbContext>>();
            services.RemoveAll<DirectoryServiceDbContext>();
            services.AddDbContext<DirectoryServiceDbContext>(options =>
                options.UseNpgsql(_connectionString));

            services.RemoveAll<IDbConnectionFactory>();
            services.AddSingleton<IDbConnectionFactory>(_ => new NpgsqlConnectionFactory(_connectionString));
        });
    }
}

