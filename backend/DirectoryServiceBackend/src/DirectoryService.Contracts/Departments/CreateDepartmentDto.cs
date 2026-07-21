namespace DirectoryService.Contracts.Departments;

public record CreateDepartmentDto(string Name,
    string Slug,
    Guid? ParentDepartmentId,
    Guid ChiefPositionMatrixId,
    List<Guid> LocationsId);