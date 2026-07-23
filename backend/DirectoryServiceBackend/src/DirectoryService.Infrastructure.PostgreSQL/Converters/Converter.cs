using CSharpFunctionalExtensions;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

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
    public PositionNameConverter() : base(
        name => name.Value,
        str => UnwrapResult(PositionName.Create(str)))
    { }
    private static PositionName UnwrapResult(Result<PositionName, Error> result)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }
        throw new InvalidOperationException($"Ошибка загрузки PositionName из базы данных: {result.Error}");
    }
}

public class DepartmentNameConverter : ValueConverter<DepartmentName, string>
{
    public DepartmentNameConverter() : base(
        name => name.Value,
        str => UnwrapResult(DepartmentName.Create(str)))
    { }
    private static DepartmentName UnwrapResult(Result<DepartmentName, Error> result)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }
        throw new InvalidOperationException($"Ошибка загрузки DepartmentName из базы данных: {result.Error}");
    }
}

public class LocationNameConverter : ValueConverter<LocationName, string>
{
    public LocationNameConverter() : base(
        name => name.Value,
        str => UnwrapResult(LocationName.Create(str)))
    { }
    private static LocationName UnwrapResult(Result<LocationName, Error> result)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }
        throw new InvalidOperationException($"Ошибка загрузки LocationName из базы данных: {result.Error}");
    }
}

public class AddressConverter : ValueConverter<Address, string>
{
    public AddressConverter() : base(
        name => name.Value,
        str => UnwrapResult(Address.Create(str)))
    { }
    private static Address UnwrapResult(Result<Address, Error> result)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }
        throw new InvalidOperationException($"Ошибка загрузки Address из базы данных: {result.Error}");
    }
}

public class SlugConverter : ValueConverter<Slug, string>
{
    public SlugConverter() : base(
        slug => slug.Value,
        str => UnwrapResult(Slug.Create(str)))
    { }
    private static Slug UnwrapResult(Result<Slug, Error> result)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }
        throw new InvalidOperationException($"Ошибка загрузки Slug из базы данных: {result.Error}");
    }
}

public class PathSlugConverter : ValueConverter<PathSlug, string>
{
    public PathSlugConverter() : base(
        path => path.Value,
        str => UnwrapResult(PathSlug.Create(str)))
        { }
    private static PathSlug UnwrapResult(Result<PathSlug, Error> result)
    {
        if (result.IsSuccess)
        {
            return result.Value;
        }
        throw new InvalidOperationException($"Ошибка загрузки PathSlug из базы данных: {result.Error}");
    }
}

public class DateTimeUtcConverter : ValueConverter<DateTime, DateTime>
{
    public DateTimeUtcConverter() : base(
        v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
    {
    }
}
