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


DotEnv.Load();
string? connectionString = Environment.GetEnvironmentVariable("DIRECTORY_SERVICE_CONNECTIONSTRING");
Console.WriteLine($"connectionString: {connectionString}");

if (connectionString == null)
{
    throw new DSException("Не указана строка подключения");
}


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IDbConnectionFactory, NpgsqlConnectionFactory>(_ => new NpgsqlConnectionFactory(connectionString));
builder.Services.AddDbContext<DirectoryServiceDbContext>(options =>
{
    options.UseNpgsql(connectionString);
});
builder.Services.AddScoped<IDirectoryServiceDbContext>(provider =>
    provider.GetRequiredService<DirectoryServiceDbContext>());

builder.Services.AddOpenApi();
//builder.Services.AddOpenApi(options =>
//{
// Отключаем встроенный генератор комментариев, который вызывает ошибку компиляции
//    options.OpenApiVersion = Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0;
//});
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddProgramDependencies();


WebApplication app = builder.Build();

app.UseExceptionMiddleware();

app.MapGet("/", () => "Yellow Submarine");

app.MapControllers();

app.MapHealthChecks("/health");


if(!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
    //app.MapScalarApiReference(options =>
    //{
    //    options.WithOpenApiRoutePattern("/openapi/v1.json");
    //});
}


app.MapGet("/guid", () => $"{(Guid.CreateVersion7())}");

await app.RunAsync().ConfigureAwait(false);
