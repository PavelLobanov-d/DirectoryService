using CSharpFunctionalExtensions;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.shared;

namespace DirectoryService.Core.DepartmentPositions;

public interface IDepartmentPositionsRepository
{
    Task<Result<Guid, Error>> AddAsync(DepartmentPosition obj, CancellationToken cancellationToken = default);
    Task<Result<DepartmentPosition?, Error>> GetByIdAsync(Guid departmentPositionId, CancellationToken cancellationToken = default);
    Task<Result<List<DepartmentPosition>, Error>> GetPositionsByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> DeleteAsync(Guid departmentPositionId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> DeleteAsync(DepartmentPosition departmentPosition, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default);
}
