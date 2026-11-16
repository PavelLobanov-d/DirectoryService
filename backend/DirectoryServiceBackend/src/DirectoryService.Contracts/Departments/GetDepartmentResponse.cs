
namespace DirectoryService.Contracts.Departments;

public record GetDepartmentResponse(
    Guid Id,
    Guid? ChiefPositionId,
    string PathSlugFull,
    string Name,
    Guid? ParentId,
    List<Guid>? ChildsIds,
    List<Guid>? LocationsIds,
    List<Guid>? PositionsIds);