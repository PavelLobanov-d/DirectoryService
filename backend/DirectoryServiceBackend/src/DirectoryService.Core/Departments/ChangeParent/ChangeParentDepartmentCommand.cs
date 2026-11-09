using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.ChangeParent;

public record ChangeParentDepartmentCommand(Guid DepartmentId, Guid NewParentDepartmentId) : ICommand;
