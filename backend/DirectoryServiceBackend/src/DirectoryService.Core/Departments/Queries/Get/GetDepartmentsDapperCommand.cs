using DirectoryService.Contracts;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Queries.Get;

public record GetDepartmentsDapperCommand(SelectDto Request) : ICommand;
