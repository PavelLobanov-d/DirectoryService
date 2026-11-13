using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.PositionsMatrix;

namespace DirectoryService.Core.Database;

public interface IReadDbContext
{
    IQueryable<Department> DepartmentsRead { get; }
    IQueryable<Location> LocationsRead { get; }
    IQueryable<PositionMatrix> PositionsRead { get; }
}
