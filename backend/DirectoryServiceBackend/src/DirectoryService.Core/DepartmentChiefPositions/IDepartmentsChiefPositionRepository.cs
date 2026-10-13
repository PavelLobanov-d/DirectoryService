using CSharpFunctionalExtensions;
using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Shared;

namespace DirectoryService.Core.DepartmentChiefPositions;

public interface IDepartmentsChiefPositionRepository
{
    Task<Result<Guid, Error>> AddAsync(DepartmentChiefPosition obj, CancellationToken cancellationToken = default);
    Task<Result<DepartmentChiefPosition?, Error>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> DeleteAsync(DepartmentChiefPosition departmentChiefPosition, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default);
}
