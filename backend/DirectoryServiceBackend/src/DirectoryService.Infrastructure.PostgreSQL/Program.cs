using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.shared;
using DirectoryService.Infrastructure.PostgreSQL;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

var confbuilder = new ConfigurationBuilder();
// установка пути к текущему каталогу
confbuilder.SetBasePath(Directory.GetCurrentDirectory());
// получаем конфигурацию из файла appsettings.json
confbuilder.AddJsonFile("appsettings.json");
IConfigurationRoot config = confbuilder.Build();
// получаем строку подключения
string? connectionString = config.GetConnectionString("PostgreSQL");
if (connectionString == null)
{
    Console.WriteLine("Не указана строка подключения в поле \"PostgreSQL\"");
    return;
}

Console.WriteLine($"connectionString: {connectionString}");

DirectoryServiceDbContext directoryServiceDbContext = new (connectionString);

Location location = directoryServiceDbContext.Locations.Add(Location.Create(LocationName.Create("Локация1"), Address.Create("На деревню, Дедушке"))).Entity;
PositionMatrix posCEO = directoryServiceDbContext.PositionsMatrix.Add(PositionMatrix.Create(PositionName.Create("Ген. директор"), Slug.Create("ceo"), parent: null)).Entity;
Department departmentCEO = directoryServiceDbContext.Departments.Add(Department.Create(id: null, DepartmentName.Create("Аппарат ген.дира"), Slug.Create("director"), parent: null, posCEO)).Entity;
DepartmentPosition depPos = directoryServiceDbContext.DepartmentPositions.Add(departmentCEO.ChiefDepartmentPosition).Entity;
DepartmentLocation departmentLocation = directoryServiceDbContext.DepartmentLocations.Add(DepartmentLocation.Create(departmentCEO, location)).Entity;

PositionMatrix posDep11 = directoryServiceDbContext.PositionsMatrix.Add(PositionMatrix.Create(PositionName.Create("Начальник службы безопасности"), Slug.Create("chiefgbr"), posCEO)).Entity;
Department department11 = directoryServiceDbContext.Departments.Add(Department.Create(id: null, DepartmentName.Create("Служба безопасности"), Slug.Create("gbr"), departmentCEO, posDep11)).Entity;
depPos = directoryServiceDbContext.DepartmentPositions.Add(department11.ChiefDepartmentPosition).Entity;
departmentLocation = directoryServiceDbContext.DepartmentLocations.Add(DepartmentLocation.Create(department11, location)).Entity;
