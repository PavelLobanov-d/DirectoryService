using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Contracts.Departments;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.shared;

namespace DirectoryService.Core.Departments;

public interface IDepartmentsService
{
    public Task<Result<Guid, Errors>> CreateAsync(CreateDepartmentDto departmentDto, CancellationToken cancellationToken = default);
    public Task<Result<bool, Error>> DeleteAsync(Guid departmentId, CancellationToken cancellationToken = default);
    public Task<Result<Department?, Error>> GetByIdAsync(Guid departmentId, CancellationToken cancellationToken = default);
    public Task<Result<List<Department>, Error>> GetByParentIdAsync(Guid parentDepartmentId, CancellationToken cancellationToken = default);
    public Task<Result<List<Department>, Error>> GetAsync(SelectDto request, CancellationToken cancellationToken = default);
    public Task<Result<bool, Errors>> UpdateAsync(Guid departmentId, UpdateDepartmentDto? departmentDto, CancellationToken cancellationToken = default);
    public Task<Result<bool, Errors>> UpdateAsync(Department department, UpdateDepartmentDto? departmentDto, CancellationToken cancellationToken = default);
    public Task<Result<bool, Errors>> ChangeParentAsync(Guid departmentId, Guid newParentDepartmentId, CancellationToken cancellationToken = default);
    public Task<Result<bool, Error>> ChangeChiefPositionMatrixAsync(Guid departmentId, Guid newChiefPositionMatrixId, CancellationToken cancellationToken = default);
    public Task<Result<Guid, Error>> LinkPositionAsync(Guid departmentId, Guid positionMatrixId, CancellationToken cancellationToken = default);
    public Task<Result<bool, Error>> DetachPositionAsync(Guid departmentPositionId, CancellationToken cancellationToken = default);
    public Task<Result<Guid, Error>> LinkLocationAsync(Guid departmentId, Guid locationId, CancellationToken cancellationToken = default);
    public Task<Result<bool, Error>> DetachLocationAsync(Guid departmentId, Guid locationId, CancellationToken cancellationToken = default);
    public Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default);
}
