using CSharpFunctionalExtensions;
using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Shared;
using System.Linq.Expressions;

namespace DirectoryService.Core.DepartmentLocations;

public interface IDepartmentLocationsRepository
{
    Task<Result<Guid, Error>> AddAsync(DepartmentLocation obj, CancellationToken cancellationToken = default);
    Task<Result<List<DepartmentLocation>?, Error>> GetByAsync(Expression<Func<DepartmentLocation, bool>> predicate, CancellationToken cancellationToken = default);
    Task<Result<DepartmentLocation?, Error>> GetByIdAsync(Guid departmentLocationId, CancellationToken cancellationToken = default);
    Task<Result<List<DepartmentLocation>?, Error>> GetLocationsByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> DeleteAsync(Guid departmentLocationId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> DeleteAsync(DepartmentLocation departmentLocation, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default);
}
