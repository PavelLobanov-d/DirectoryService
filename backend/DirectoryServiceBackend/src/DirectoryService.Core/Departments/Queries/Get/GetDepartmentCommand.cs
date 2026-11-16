using DirectoryService.Contracts;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Queries.Get;

public record GetDepartmentCommand(SelectDto Request): ICommand;
