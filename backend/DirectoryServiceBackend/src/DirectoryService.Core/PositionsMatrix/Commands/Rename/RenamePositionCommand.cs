using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.Commands.Rename;

public record RenamePositionCommand(Guid PositionMatrixId, RenamePositionDto PositionDto) : ICommand;
