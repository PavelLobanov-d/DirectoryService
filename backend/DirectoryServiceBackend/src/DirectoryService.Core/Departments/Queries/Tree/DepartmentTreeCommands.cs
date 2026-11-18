using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Queries.Tree;

public record GetDepartmentTreeCommand : ICommand;

public record GetDepartmentChildrenCommand(Guid DepartmentId) : ICommand;

public record GetDepartmentAncestorsCommand(Guid DepartmentId) : ICommand;

public record SearchDepartmentTreeCommand(string Query) : ICommand;
