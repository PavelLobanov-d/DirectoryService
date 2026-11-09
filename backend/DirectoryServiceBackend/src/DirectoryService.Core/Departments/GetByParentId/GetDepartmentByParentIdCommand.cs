using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.GetByParentId;

public record GetDepartmentByParentIdCommand(Guid ParentDepartmentId): ICommand;
