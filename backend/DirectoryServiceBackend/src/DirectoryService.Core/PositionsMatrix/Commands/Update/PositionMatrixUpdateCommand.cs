using DirectoryService.Contracts.PositionsMatrix;

namespace DirectoryService.Core.PositionsMatrix.Commands.Update;

public record PositionMatrixUpdateCommand(Guid PositionMatrixId, UpdatePositionMatrixDto? PositionDto) : ICommand;
