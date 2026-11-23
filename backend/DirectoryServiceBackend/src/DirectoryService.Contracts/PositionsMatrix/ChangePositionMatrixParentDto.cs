namespace DirectoryService.Contracts.PositionsMatrix;

public record ChangePositionMatrixParentDto(Guid? ParentId);

public record ChangePositionMatrixParentResponse(
    Guid Id,
    Guid? ParentId,
    string Path,
    int Depth,
    DateTime UpdatedAt);
