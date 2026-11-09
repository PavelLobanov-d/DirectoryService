using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.DetachLocation;

public record DepartmentDetachLocationCommand(Guid DepartmentId, Guid LocationId): ICommand;
