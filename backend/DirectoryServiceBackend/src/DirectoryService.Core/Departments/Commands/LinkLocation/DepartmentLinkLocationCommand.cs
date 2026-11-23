
namespace DirectoryService.Core.Departments.Commands.LinkLocation;

public record DepartmentLinkLocationCommand(Guid DepartmentId, Guid LocationId): ICommand;
