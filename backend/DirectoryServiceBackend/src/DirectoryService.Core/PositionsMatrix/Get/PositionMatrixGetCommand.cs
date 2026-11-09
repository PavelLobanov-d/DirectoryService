using DirectoryService.Contracts;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.Get;

public record PositionMatrixGetCommand(SelectDto Request) : ICommand;
