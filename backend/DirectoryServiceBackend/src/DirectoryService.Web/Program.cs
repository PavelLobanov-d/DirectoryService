using DirectoryService.Domain.Departments;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using DirectoryService.Infrastructure.PostgreSQL;
using dotenv.net;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;
using System.Runtime.CompilerServices;


DotEnv.Load();
string? connectionString = Environment.GetEnvironmentVariable("DIRECTORY_SERVICE_CONNECTIONSTRING");
Console.WriteLine($"connectionString: {connectionString}");

if (connectionString == null)
{
    throw new DSException("Не указана строка подключения");
}

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
builder.Services.AddControllers();
builder.Services.AddHealthChecks();
builder.Services.AddScoped<GlobalStatistics>();
builder.Services.AddScoped<DirectoryServiceDbContext>(_ => new DirectoryServiceDbContext(connectionString));
//builder.Services.AddDbContext<DirectoryServiceDbContext>(options =>
//    options.UseNpgsql(connectionString));

WebApplication app = builder.Build();

app.MapGet("/", () => "Yellow Submarine");

app.MapControllers();

app.MapHealthChecks("/health");

if(!app.Environment.IsProduction())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapGet("/starttest", async (DirectoryServiceDbContext db, GlobalStatistics globalStatistics) =>
{
    //локация
    Location location = db.Locations.Add(Location.Create(LocationName.Create("Локация1"), Address.Create("На деревню, Дедушке"), globalStatistics)).Entity;

    //головная должность
    PositionMatrix posCEO = db.PositionsMatrix.Add(PositionMatrix.Create(PositionName.Create("Ген. директор"), Slug.Create("ceo"), parent: null, globalStatistics)).Entity;
    //головной департамент
    Department departmentCEO = db.Departments.Add(Department.Create(DepartmentName.Create("Аппарат ген.дира"), Slug.Create("director"), parent: null, posCEO, globalStatistics)).Entity;
    //при создании департамента автоматом создаётся связка с должностью начальника
    db.DepartmentChiefPositions.Add(departmentCEO.DepartmentChiefPosition);
    db.DepartmentLocations.Add(departmentCEO.LinkLocation(location, globalStatistics));

    //второстепенная должность
    PositionMatrix posAssist = db.PositionsMatrix.Add(PositionMatrix.Create(PositionName.Create("Помощник"), Slug.Create("assist"), posCEO, globalStatistics)).Entity;
    //количество одинаковых должностей в департаменте не ограничено (Помощник по общим, Помощник по орг ...)
    db.DepartmentPositions.Add(departmentCEO.LinkPosition(posAssist, globalStatistics));
    db.DepartmentPositions.Add(departmentCEO.LinkPosition(posAssist, globalStatistics));

    //должность нач.второстепенного департамента
    PositionMatrix posDep11 = db.PositionsMatrix.Add(PositionMatrix.Create(PositionName.Create("Начальник службы безопасности"), Slug.Create("chiefgbr"), posCEO, globalStatistics)).Entity;
    //второстепенный департамент
    Department department11 = db.Departments.Add(Department.Create(DepartmentName.Create("Служба безопасности"), Slug.Create("gbr"), departmentCEO, posDep11, globalStatistics)).Entity;
    db.DepartmentChiefPositions.Add(department11.DepartmentChiefPosition);
    db.DepartmentLocations.Add(department11.LinkLocation(location, globalStatistics));

    //третьестепенные должности
    PositionMatrix posGuard = db.PositionsMatrix.Add(PositionMatrix.Create(PositionName.Create("Охранник"), Slug.Create("guard"), posDep11, globalStatistics)).Entity;
    db.DepartmentPositions.Add(department11.LinkPosition(posGuard, globalStatistics));
    db.DepartmentPositions.Add(department11.LinkPosition(posGuard, globalStatistics));
    db.DepartmentPositions.Add(department11.LinkPosition(posGuard, globalStatistics));

    db.Statistics.AddRange(globalStatistics.Stats);

    await db.SaveChangesAsync().ConfigureAwait(true);
});

await app.RunAsync().ConfigureAwait(false);
//app.Run();
