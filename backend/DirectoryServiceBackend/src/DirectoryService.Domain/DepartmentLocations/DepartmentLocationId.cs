using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Domain.DepartmentLocations;

public record DepartmentLocationId(Guid Value)
{
    public override string ToString() => Value.ToString();
}
