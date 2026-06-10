using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Domain.Statistics;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DirectoryService.Domain.Locations;

public sealed class Location
{
    private Location() { }
    private Location(
        LocationId id,
        LocationName name,
        Address address)
    {
        Id = id;
        Name = name;
        Address = address;
    }
    public LocationId Id { get; private set; } = null!;
    public LocationName Name { get; private set; } = null!;
    public Address Address { get; private set; } = null!;

    private readonly List<DepartmentLocation> _departmentLocations = [];
    public IReadOnlyList<DepartmentLocation> DepartmentLocations => _departmentLocations;

    public static Location Create(
        LocationName name, 
        Address address)
    {
        Location newObject = new(new LocationId(Guid.CreateVersion7()), name, address);
       
        return newObject;
    }

    /// <summary>
    /// изменить
    /// </summary>
    /// <param name="name"></param>
    /// <param name="address"></param>
    /// <returns>true, если были изменения</returns>
    public bool Update(
        LocationName? name, 
        Address? address,
        GlobalStatistics globalstats)
    {
        bool result = false;
        if (name != null && Name != name)
        {
            globalstats.AddStatistica(Id.Value,
                this.GetType().Name,
                Statistica.Level.FINE,
                Statistica.Action.UPDATE,
                $"Название изменено с {Name} на {name}");
            Name = name;
            result = true;
        }
        if (address != null && Address != address)
        {
            globalstats.AddStatistica(Id.Value,
                this.GetType().Name,
                Statistica.Level.FINE,
                Statistica.Action.UPDATE,
                $"Адрес изменен с {Address} на {address}");
            Address = address;

            result = true;
        }

        return result;
    }
}
