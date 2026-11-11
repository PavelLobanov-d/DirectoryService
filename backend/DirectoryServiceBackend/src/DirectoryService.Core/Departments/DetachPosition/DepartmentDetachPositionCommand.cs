using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Departments.DetachPosition;

public record DepartmentDetachPositionCommand(Guid DepartmentId, Guid PositionMatrixId) : ICommand;
