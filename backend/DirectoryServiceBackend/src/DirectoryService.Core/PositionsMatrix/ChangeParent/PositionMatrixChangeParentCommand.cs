using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.ChangeParent;

public record PositionMatrixChangeParentCommand(Guid PositionMatrixId, Guid NewParentPositionId) : ICommand;
