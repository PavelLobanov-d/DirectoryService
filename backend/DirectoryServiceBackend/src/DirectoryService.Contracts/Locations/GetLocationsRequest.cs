namespace DirectoryService.Contracts.Locations;

public record GetLocationsRequest(
    string? Search,
    int? MinDepartmentCount,
    string? SortBy,
    string? SortDir,
    int? Page,
    int? PageSize);
