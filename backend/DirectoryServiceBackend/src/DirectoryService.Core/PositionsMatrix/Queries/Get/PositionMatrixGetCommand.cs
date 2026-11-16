using DirectoryService.Contracts;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.Queries.Get;

public record PositionMatrixGetCommand(SelectDto Request) : ICommand;
