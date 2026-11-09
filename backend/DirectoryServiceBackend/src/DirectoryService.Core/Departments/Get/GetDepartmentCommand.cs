using DirectoryService.Contracts;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Get;

public record GetDepartmentCommand(SelectDto Request): ICommand;
