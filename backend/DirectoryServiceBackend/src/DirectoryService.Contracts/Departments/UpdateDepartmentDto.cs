namespace DirectoryService.Contracts.Departments;

public record UpdateDepartmentDto(string? NewName, string? NewSlug, Guid? NewChiefPositionMatrixId);