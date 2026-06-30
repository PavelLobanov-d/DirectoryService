using DirectoryService.Core.Database;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Domain.shared;
using DirectoryService.Infrastructure.PostgreSQL;
using DirectoryService.Infrastructure.PostgreSQL.Database;
using DirectoryService.Web;
using dotenv.net;
using Scalar.AspNetCore;


DotEnv.Load();
string? connectionString = Environment.GetEnvironmentVariable("DIRECTORY_SERVICE_CONNECTIONSTRING");
Console.WriteLine($"connectionString: {connectionString}");

if (connectionString == null)
{
    throw new DSException("Не указана строка подключения");
}


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>(_ => new NpgsqlConnectionFactory(connectionString));

builder.Services.AddProgramDependencies();
builder.Services.AddScoped<GlobalStatistics>();
builder.Services.AddScoped<IDirectoryServiceDbContext, DirectoryServiceDbContext>(_ => new DirectoryServiceDbContext(connectionString));

WebApplication app = builder.Build();

app.MapGet("/", () => "Yellow Submarine");

app.MapControllers();

app.MapHealthChecks("/health");

if(!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/guid", () => $"{(Guid.CreateVersion7())}");

await app.RunAsync().ConfigureAwait(false);
