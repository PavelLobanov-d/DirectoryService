using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.FindChildById;

public record PositionMatrixFindChildByIdCommand(PositionMatrix PositionMatrix, PositionMatrixId FindPositionId) : ICommand;
