using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Update;

public record UpdateDepartmentCommand(Guid DepartmentId, UpdateDepartmentDto? DepartmentDto) : ICommand;
