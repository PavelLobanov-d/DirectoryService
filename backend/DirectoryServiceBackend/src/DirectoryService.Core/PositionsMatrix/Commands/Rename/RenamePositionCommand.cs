using DirectoryService.Contracts.PositionsMatrix;

namespace DirectoryService.Core.PositionsMatrix.Commands.Rename;

public record RenamePositionCommand(Guid PositionMatrixId, RenamePositionDto PositionDto) : ICommand;
