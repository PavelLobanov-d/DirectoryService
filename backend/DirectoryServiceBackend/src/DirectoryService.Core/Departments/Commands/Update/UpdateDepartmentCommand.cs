using DirectoryService.Contracts.Departments;

namespace DirectoryService.Core.Departments.Commands.Update;

public record UpdateDepartmentCommand(Guid DepartmentId, UpdateDepartmentDto? DepartmentDto) : ICommand;
