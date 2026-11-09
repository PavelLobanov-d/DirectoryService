using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.GetById;

public record GetDepartmentByIdCommand(Guid DepartmentId): ICommand;
