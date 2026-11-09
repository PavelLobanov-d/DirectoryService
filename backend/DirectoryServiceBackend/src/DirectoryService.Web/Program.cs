using DirectoryService.Core.Database;
using DirectoryService.Domain.shared;
using DirectoryService.Infrastructure.PostgreSQL;
using DirectoryService.Infrastructure.PostgreSQL.Database;
using DirectoryService.Web;
using DirectoryService.Web.Middlewares;
using dotenv.net;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.EntityFrameworkCore;
//using Microsoft.OpenApi; // Используется корневое пространство вместо .Models и .Any
using Scalar.AspNetCore;
using Serilog;
using Serilog.Configuration;
using Serilog.Exceptions;
using System.Globalization;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: CultureInfo.InvariantCulture)
    .CreateLogger();

Serilog.Debugging.SelfLog.Enable(msg => Console.WriteLine($"[SERILOG ERROR] {msg}"));

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

    builder.Services.AddOpenApi();
    builder.Services.AddEndpointsApiExplorer();

    WebApplication app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseExceptionMiddleware();

    app.MapGet("/", () => "Yellow Submarine");

    app.MapControllers();

    app.MapHealthChecks("/health");


    if (!app.Environment.IsProduction())
    {
        app.MapOpenApi();
        app.MapScalarApiReference();
    }


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

