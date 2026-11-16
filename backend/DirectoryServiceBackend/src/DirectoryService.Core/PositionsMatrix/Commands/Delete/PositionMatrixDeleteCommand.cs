using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.Commands.Delete;

public record PositionMatrixDeleteCommand(Guid PositionMatrixId) : ICommand;
