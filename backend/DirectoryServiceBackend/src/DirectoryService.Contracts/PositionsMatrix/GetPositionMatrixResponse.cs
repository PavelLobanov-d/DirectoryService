

namespace DirectoryService.Contracts.PositionsMatrix;

public record GetPositionMatrixResponse
(
    Guid Id,
    string Name,
    string Path,
    Guid? ParentId,
    List<Guid> ChildsIds
);
