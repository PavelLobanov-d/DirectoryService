using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Commands.Create;

public record CreateDepartmentCommand(CreateDepartmentDto DepartmentDto) : ICommand;
