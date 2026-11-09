using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.Update;

public record PositionMatrixUpdateCommand(Guid PositionMatrixId, UpdatePositionMatrixDto? PositionDto) : ICommand;
