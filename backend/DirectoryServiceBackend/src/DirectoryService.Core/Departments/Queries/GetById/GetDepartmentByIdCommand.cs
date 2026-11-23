
namespace DirectoryService.Core.Departments.Queries.GetById;

public record GetDepartmentByIdCommand(Guid DepartmentId): ICommand;
