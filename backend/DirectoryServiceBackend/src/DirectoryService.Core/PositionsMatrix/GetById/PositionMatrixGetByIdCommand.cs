using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.GetById;

public record PositionMatrixGetByIdCommand(Guid PositionMatrixId) : ICommand;
