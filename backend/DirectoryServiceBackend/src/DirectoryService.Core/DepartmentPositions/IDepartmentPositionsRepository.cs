using CSharpFunctionalExtensions;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Shared;
using System.Linq.Expressions;

namespace DirectoryService.Core.DepartmentPositions;

public interface IDepartmentPositionsRepository
{
    Task<Result<Guid, Error>> AddAsync(DepartmentPosition obj, CancellationToken cancellationToken = default);
    Task<Result<List<DepartmentPosition>?, Error>> GetByAsync(Expression<Func<DepartmentPosition, bool>> predicate, CancellationToken cancellationToken = default);
    Task<Result<DepartmentPosition?, Error>> GetByIdAsync(Guid departmentPositionId, CancellationToken cancellationToken = default);
    Task<Result<List<DepartmentPosition>?, Error>> GetPositionsByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> DeleteAsync(Guid departmentPositionId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> DeleteAsync(DepartmentPosition departmentPosition, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default);
}
