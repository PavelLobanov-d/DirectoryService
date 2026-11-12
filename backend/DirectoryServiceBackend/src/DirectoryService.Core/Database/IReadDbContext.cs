using DirectoryService.Domain.Departments;

namespace DirectoryService.Core.Database;

public interface IReadDbContext
{
    IQueryable<Department> DepartmentsRead { get; }
}
