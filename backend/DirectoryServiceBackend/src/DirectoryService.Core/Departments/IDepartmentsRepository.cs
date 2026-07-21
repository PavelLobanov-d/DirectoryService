using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.shared;

namespace DirectoryService.Core.Departments;

public interface IDepartmentsRepository
{
    Task<Result<Guid, Error>> AddAsync(Department department, CancellationToken cancellationToken = default);
    Task<Result<List<Department>, Error>> GetAsync(SelectDto request, CancellationToken cancellationToken = default);
    Task<Result<Department?, Error>> GetByIdAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<Result<List<Department>, Error>> GetByParentIdAsync(Guid? parentDepartmentId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> HasNameSlugAsync(string name, string slug, Guid? parentId, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> UpdateAsync(Department department, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> DeleteAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> DeleteAsync(Department department, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default);
}
