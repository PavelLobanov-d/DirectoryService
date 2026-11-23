
namespace DirectoryService.Core.PositionsMatrix.Queries.GetById;

public record PositionMatrixGetByIdCommand(Guid PositionMatrixId) : ICommand;
