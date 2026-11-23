using DirectoryService.Core.Database;
using DirectoryService.Domain.shared;
using DirectoryService.Infrastructure.PostgreSQL;
using DirectoryService.Infrastructure.PostgreSQL.Database;
using DirectoryService.Web;
using dotenv.net;
using Microsoft.EntityFrameworkCore;
using Serilog;

Log.Logger = SharedHosting.CreateBootstrapLogger();
SharedHosting.EnableSerilogSelfLog();

try
{
    Log.Information("Starting web application");

    var builder = WebApplication.CreateBuilder(args);
    builder.Configuration
        .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

    DotEnv.Load();
    string? connectionString = Environment.GetEnvironmentVariable("DIRECTORY_SERVICE_CONNECTIONSTRING");

    if (connectionString == null)
        throw new DSException("Не указана строка подключения");

    builder.Services.AddProgramDependencies(builder.Configuration);

    builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>(_ => new NpgsqlConnectionFactory(connectionString));
    builder.Services.AddDbContext<DirectoryServiceDbContext>(options =>
        options.UseNpgsql(connectionString));
    builder.Services.AddScoped<IDirectoryServiceDbContext>(provider =>
        provider.GetRequiredService<DirectoryServiceDbContext>());
    builder.Services.AddScoped<IReadDbContext>(provider =>
        provider.GetRequiredService<DirectoryServiceDbContext>());

    WebApplication app = builder.Build();

    app.UseSharedSerilogRequestLogging();

    app.UseExceptionMiddleware();

    app.MapGet("/", () => "Yellow Submarine");

    app.MapControllers();

    app.MapSharedHealthChecks();

    app.MapSharedApiDocs();

    app.MapGet("/guid", () => $"{Guid.CreateVersion7()}");

    await app.RunAsync().ConfigureAwait(false);
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync().ConfigureAwait(false);
}


public partial class Program { }
