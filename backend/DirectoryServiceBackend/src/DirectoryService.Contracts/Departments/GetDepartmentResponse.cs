using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DirectoryService.Contracts.Departments;

public record GetDepartmentResponse(
    Guid Id,
    Guid ChiefPositionId,
    string PathSlugFull,
    string Name,
    Guid? ParentId,
    List<Guid>? ChildsIds,
    List<Guid>? LocationsIds,
    List<Guid>? PositionsIds);