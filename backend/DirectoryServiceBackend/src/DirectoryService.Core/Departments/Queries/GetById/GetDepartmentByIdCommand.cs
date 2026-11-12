using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Queries.GetById;

public record GetDepartmentByIdCommand(Guid DepartmentId): ICommand;
