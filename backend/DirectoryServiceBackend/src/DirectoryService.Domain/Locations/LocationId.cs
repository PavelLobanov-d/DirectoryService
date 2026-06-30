using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Domain.Locations;

public record LocationId(Guid Value)
{
    public override string ToString() => Value.ToString();
}
