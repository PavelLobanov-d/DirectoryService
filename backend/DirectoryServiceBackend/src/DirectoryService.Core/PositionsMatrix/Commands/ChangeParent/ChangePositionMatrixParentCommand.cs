
namespace DirectoryService.Core.PositionsMatrix.Commands.ChangeParent;

public record ChangePositionMatrixParentCommand(Guid PositionMatrixId, Guid? ParentId) : ICommand;
