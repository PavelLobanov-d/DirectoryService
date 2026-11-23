using DirectoryService.Contracts;

namespace DirectoryService.Core.PositionsMatrix.Queries.Get;

public record PositionMatrixGetCommand(SelectDto Request) : ICommand;
