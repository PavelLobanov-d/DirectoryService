namespace DirectoryService.Contracts.Departments;

public record DepartmentTreeItemResponse(
    Guid Id,
    string Name,
    string Path,
    int Depth,
    bool HasChildren,
    int ChildrenCount);

public record DepartmentAncestorResponse(
    Guid Id,
    string Name,
    string Path,
    int Depth);

public record DepartmentTreeSearchItemResponse(
    Guid Id,
    string Name,
    string Path,
    int Depth,
    bool HasChildren,
    int ChildrenCount,
    List<DepartmentAncestorResponse> Ancestors);
