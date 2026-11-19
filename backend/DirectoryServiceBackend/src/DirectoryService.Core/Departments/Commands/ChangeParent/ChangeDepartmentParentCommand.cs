using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Commands.ChangeParent;

public record ChangeDepartmentParentCommand(Guid DepartmentId, Guid? ParentId) : ICommand;
