using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Delete;

public record DeleteDepartmentCommand(Guid DepartmentId): ICommand;
