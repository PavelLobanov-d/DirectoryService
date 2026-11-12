
namespace DirectoryService.Contracts.Locations;

public record GetLocationResponse(
    Guid Id,
    string Name,
    string Address,
    List<Guid> DepartmentIds);
