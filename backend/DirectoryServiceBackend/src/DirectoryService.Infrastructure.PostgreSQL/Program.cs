using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using DirectoryService.Infrastructure.PostgreSQL;
using dotenv.net;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql.EntityFrameworkCore.PostgreSQL;


DotEnv.Load();
string? connectionString = Environment.GetEnvironmentVariable("DIRECTORY_SERVICE_CONNECTIONSTRING");
Console.WriteLine($"connectionString: {connectionString}");

if (connectionString == null)
{
    Console.WriteLine("Не указана строка подключения");
    return;
}


var builder = WebApplication.CreateBuilder(args);
builder.Services.AddDbContext<DirectoryServiceDbContext>(options =>
    options.UseNpgsql(connectionString));

var app = builder.Build();

app.MapPost("/starttest", async (DirectoryServiceDbContext db) =>
{
    //локация
    Location location = db.Locations.Add(Location.Create(LocationName.Create("Локация1"), Address.Create("На деревню, Дедушке"))).Entity;
 
    //головная должность
    PositionMatrix posCEO = db.PositionsMatrix.Add(PositionMatrix.Create(PositionName.Create("Ген. директор"), Slug.Create("ceo"), parent: null)).Entity;
    //головной департамент
    Department departmentCEO = db.Departments.Add(Department.Create(id: null, DepartmentName.Create("Аппарат ген.дира"), Slug.Create("director"), parent: null, posCEO)).Entity;
    //при создании департамента автоматом создаётся связка с должностью начальника
    db.DepartmentPositions.Add(departmentCEO.ChiefDepartmentPosition);
    db.DepartmentLocations.Add(departmentCEO.LinkLocation(location));

    //второстепенная должность
    PositionMatrix posAssist = db.PositionsMatrix.Add(PositionMatrix.Create(PositionName.Create("Помощник"), Slug.Create("assist"), posCEO)).Entity;
    //количество одинаковых должностей в департаменте не ограничено (Помощник по общим, Помощник по орг ...)
    db.DepartmentPositions.Add(departmentCEO.LinkPosition(posAssist));
    db.DepartmentPositions.Add(departmentCEO.LinkPosition(posAssist));

    //должность нач.второстепенного департамента
    PositionMatrix posDep11 = db.PositionsMatrix.Add(PositionMatrix.Create(PositionName.Create("Начальник службы безопасности"), Slug.Create("chiefgbr"), posCEO)).Entity;
    //второстепенный департамент
    Department department11 = db.Departments.Add(Department.Create(id: null, DepartmentName.Create("Служба безопасности"), Slug.Create("gbr"), departmentCEO, posDep11)).Entity;
    db.DepartmentPositions.Add(department11.ChiefDepartmentPosition);
    db.DepartmentLocations.Add(department11.LinkLocation(location));

    //третьестепенные должности
    PositionMatrix posGuard = db.PositionsMatrix.Add(PositionMatrix.Create(PositionName.Create("Охранник"), Slug.Create("guard"), posDep11)).Entity;
    db.DepartmentPositions.Add(department11.LinkPosition(posGuard));
    db.DepartmentPositions.Add(department11.LinkPosition(posGuard));
    db.DepartmentPositions.Add(department11.LinkPosition(posGuard));

    await db.SaveChangesAsync().ConfigureAwait(true);
});

app.Run();
