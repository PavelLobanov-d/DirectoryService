
namespace DirectoryService.Core.PositionsMatrix.Queries.GetByParentId;

public record PositionMatrixGetByParentIdCommand(Guid ParentPositionMatrixId) : ICommand;
