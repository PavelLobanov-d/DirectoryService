using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.PositionsMatrix;

namespace DirectoryService.Core.Database;

public interface IReadDbContext
{
    IQueryable<Department> DepartmentsRead { get; }
    IQueryable<Department> DepartmentsFromSql(FormattableString sql);
    IQueryable<Location> LocationsRead { get; }
    IQueryable<DepartmentLocation> DepartmentLocationsRead { get; }
    IQueryable<PositionMatrix> PositionsRead { get; }
}
