using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Infrastructure.PostgreSQL.Converters;

public class PositionMatrixIdConverter : ValueConverter<PositionMatrixId, Guid>
{
    public PositionMatrixIdConverter() : base(id => id.Value, guid => new PositionMatrixId(guid)) { }
}

public class DepartmentIdConverter : ValueConverter<DepartmentId, Guid>
{
    public DepartmentIdConverter() : base(id => id.Value, guid => new DepartmentId(guid)) { }
}
public class LocationIdConverter : ValueConverter<LocationId, Guid>
{
    public LocationIdConverter() : base(id => id.Value, guid => new LocationId(guid)) { }
}
public class DepartmentLocationIdConverter : ValueConverter<DepartmentLocationId, Guid>
{
    public DepartmentLocationIdConverter() : base(id => id.Value, guid => new DepartmentLocationId(guid)) { }
}
public class DepartmentPositionIdConverter : ValueConverter<DepartmentPositionId, Guid>
{
    public DepartmentPositionIdConverter() : base(id => id.Value, guid => new DepartmentPositionId(guid)) { }
}
public class PositionNameConverter : ValueConverter<PositionName, string>
{
    public PositionNameConverter() : base(name => name.Value, str => PositionName.Create(str).Value) { }
}

public class DepartmentNameConverter : ValueConverter<DepartmentName, string>
{
    public DepartmentNameConverter() : base(name => name.Value, str => DepartmentName.Create(str).Value) { }
}
public class LocationNameConverter : ValueConverter<LocationName, string>
{
    public LocationNameConverter() : base(name => name.Value, str => LocationName.Create(str).Value) { }
}

public class AddressConverter : ValueConverter<Address, string>
{
    public AddressConverter() : base(name => name.Value, str => Address.Create(str).Value) { }
}
public class SlugConverter : ValueConverter<Slug, string>
{
    public SlugConverter() : base(slug => slug.Value, str => Slug.Create(str).Value) { }
}

public class PathSlugConverter : ValueConverter<PathSlug, string>
{
    public PathSlugConverter() : base(path => path.Value, str => PathSlug.Create(str).Value) { }
}
public class DateTimeUtcConverter : ValueConverter<DateTime, DateTime>
{
    public DateTimeUtcConverter() : base(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
