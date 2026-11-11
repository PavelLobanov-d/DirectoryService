namespace DirectoryService.Contracts.PositionsMatrix;

/// <summary>
/// Плоское представление должности для чтения. Не содержит навигаций,
/// поэтому сериализация не уходит в цикл parent/childs.
/// </summary>
public record PositionMatrixDto(
    Guid Id,
    string Name,
    string Slug,
    string? PathSlug,
    Guid? ParentId);
