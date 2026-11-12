using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Queries.GetByParentId;

public record GetDepartmentByParentIdCommand(Guid ParentDepartmentId): ICommand;
