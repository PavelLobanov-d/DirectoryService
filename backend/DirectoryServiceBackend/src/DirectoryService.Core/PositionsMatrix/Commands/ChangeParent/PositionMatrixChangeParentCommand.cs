using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.Commands.ChangeParent;

public record PositionMatrixChangeParentCommand(Guid PositionMatrixId, Guid NewParentPositionId) : ICommand;
