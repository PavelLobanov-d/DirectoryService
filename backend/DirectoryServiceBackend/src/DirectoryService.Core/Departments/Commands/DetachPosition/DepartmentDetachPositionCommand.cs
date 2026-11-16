using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.Commands.DetachPosition;

public record DepartmentDetachPositionCommand(Guid DepartmentId, Guid PositionMatrixId) : ICommand;
