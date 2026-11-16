using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Commands.Delete;

public record DeleteDepartmentCommand(Guid DepartmentId): ICommand;
