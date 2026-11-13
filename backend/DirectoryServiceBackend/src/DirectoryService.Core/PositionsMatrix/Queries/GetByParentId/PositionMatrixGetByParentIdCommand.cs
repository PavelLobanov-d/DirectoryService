using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.Queries.GetByParentId;

public record PositionMatrixGetByParentIdCommand(Guid ParentPositionMatrixId) : ICommand;
