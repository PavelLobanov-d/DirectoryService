using DirectoryService.Contracts;

namespace DirectoryService.Core.Departments.Queries.Get;

public record GetDepartmentCommand(SelectDto Request): ICommand;
