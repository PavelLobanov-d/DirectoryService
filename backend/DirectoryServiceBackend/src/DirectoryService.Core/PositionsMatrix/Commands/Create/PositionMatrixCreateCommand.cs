using DirectoryService.Contracts.PositionsMatrix;

namespace DirectoryService.Core.PositionsMatrix.Commands.Create;

public record PositionMatrixCreateCommand(CreatePositionMatrixDto PositionMatrixDto) : ICommand;
