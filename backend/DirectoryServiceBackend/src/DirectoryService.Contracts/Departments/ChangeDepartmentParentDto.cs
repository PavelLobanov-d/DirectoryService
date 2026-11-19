namespace DirectoryService.Contracts.Departments;

public record ChangeDepartmentParentDto(Guid? ParentId);

public record ChangeDepartmentParentResponse(
    Guid Id,
    Guid? ParentId,
    string Path,
    int Depth,
    DateTime UpdatedAt);
