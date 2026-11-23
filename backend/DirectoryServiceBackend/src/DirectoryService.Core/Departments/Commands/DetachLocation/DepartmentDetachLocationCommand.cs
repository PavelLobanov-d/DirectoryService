
namespace DirectoryService.Core.Departments.Commands.DetachLocation;

public record DepartmentDetachLocationCommand(Guid DepartmentId, Guid LocationId): ICommand;
