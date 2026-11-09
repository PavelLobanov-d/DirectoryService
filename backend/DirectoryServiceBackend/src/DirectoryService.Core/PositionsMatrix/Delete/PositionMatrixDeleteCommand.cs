using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.Delete;

public record PositionMatrixDeleteCommand(Guid PositionMatrixId) : ICommand;
