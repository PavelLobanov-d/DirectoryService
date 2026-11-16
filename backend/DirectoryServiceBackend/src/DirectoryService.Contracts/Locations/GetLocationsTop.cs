namespace DirectoryService.Contracts.Locations;

public record GetLocationsByDepartmentsCount(
    Guid Id,
    string Name,
    string Address,
    int DepartmentCount);
