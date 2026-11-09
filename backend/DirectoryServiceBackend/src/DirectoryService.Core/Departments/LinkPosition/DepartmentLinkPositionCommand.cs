using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.LinkPosition;

public record DepartmentLinkPositionCommand(Guid DepartmentId, Guid PositionMatrixId): ICommand;
