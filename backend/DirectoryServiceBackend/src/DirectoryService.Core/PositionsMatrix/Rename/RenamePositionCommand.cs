using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.Rename;

public record RenamePositionCommand(Guid PositionMatrixId, RenamePositionDto PositionDto) : ICommand;
