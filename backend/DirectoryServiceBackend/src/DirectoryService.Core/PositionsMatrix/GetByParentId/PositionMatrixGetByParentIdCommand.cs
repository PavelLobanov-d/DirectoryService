using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.GetByParentId;

public record PositionMatrixGetByParentIdCommand(Guid ParentPositionMatrixId) : ICommand;
