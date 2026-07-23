using CSharpFunctionalExtensions;
using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;

namespace DirectoryService.Core.DepartmentChiefPositions;

public interface IDepartmentsChiefPositionRepository
{
    Task<Result<Guid, Error>> AddAsync(DepartmentChiefPosition obj, CancellationToken cancellationToken = default);
    Task<Result<DepartmentChiefPosition?, Error>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> DeleteAsync(DepartmentChiefPosition departmentChiefPosition, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default);
}
