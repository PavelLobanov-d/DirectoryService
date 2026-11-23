using DirectoryService.Contracts;

namespace DirectoryService.Core.Departments.Queries.Get;

public record GetDepartmentsDapperCommand(SelectDto Request) : ICommand;
