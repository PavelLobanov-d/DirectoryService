using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Commands.LinkPosition;

public record DepartmentLinkPositionCommand(Guid DepartmentId, Guid PositionMatrixId): ICommand;
