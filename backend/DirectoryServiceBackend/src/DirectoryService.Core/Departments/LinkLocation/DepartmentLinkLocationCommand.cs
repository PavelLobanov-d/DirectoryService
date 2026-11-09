using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.LinkLocation;

public record DepartmentLinkLocationCommand(Guid DepartmentId, Guid LocationId): ICommand;
