

namespace DirectoryService.Contracts.PositionsMatrix;

public record GetPositionMatrixResponse
(
    Guid Id,
    string Name,
    string? PathSlugFull,
    Guid? ParentId,
    string? ParentPathSlug,
    List<Guid> ChildsIds
);
