using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.Commands.Create;

public record PositionMatrixCreateCommand(CreatePositionMatrixDto PositionMatrixDto) : ICommand;
