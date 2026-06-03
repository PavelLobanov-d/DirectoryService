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
    Location location = db.Locations.Add(Location.Create(LocationName.Create("Локация1"), Address.Create("На деревню, Дедушке"))).Entity;
    PositionMatrix posCEO = db.PositionsMatrix.Add(PositionMatrix.Create(PositionName.Create("Ген. директор"), Slug.Create("ceo"), parent: null)).Entity;
    Department departmentCEO = db.Departments.Add(Department.Create(id: null, DepartmentName.Create("Аппарат ген.дира"), Slug.Create("director"), parent: null, posCEO)).Entity;
    DepartmentLocation departmentLocation = db.DepartmentLocations.Add(DepartmentLocation.Create(departmentCEO, location)).Entity;

    PositionMatrix posDep11 = db.PositionsMatrix.Add(PositionMatrix.Create(PositionName.Create("Начальник службы безопасности"), Slug.Create("chiefgbr"), posCEO)).Entity;
    Department department11 = db.Departments.Add(Department.Create(id: null, DepartmentName.Create("Служба безопасности"), Slug.Create("gbr"), departmentCEO, posDep11)).Entity;
    departmentLocation = db.DepartmentLocations.Add(DepartmentLocation.Create(department11, location)).Entity;

    await db.SaveChangesAsync().ConfigureAwait(true);
});

app.Run();
