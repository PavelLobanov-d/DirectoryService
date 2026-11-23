
namespace DirectoryService.Core.Departments.Commands.ChangeParent;

public record ChangeParentDepartmentCommand(Guid DepartmentId, Guid NewParentDepartmentId) : ICommand;
