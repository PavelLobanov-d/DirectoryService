using DirectoryService.Contracts.Departments;

namespace DirectoryService.Core.Departments.Commands.Create;

public record CreateDepartmentCommand(CreateDepartmentDto DepartmentDto) : ICommand;
