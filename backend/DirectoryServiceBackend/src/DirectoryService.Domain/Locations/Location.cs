using DirectoryService.Domain.shared;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DirectoryService.Domain.Locations;

public sealed class Location
{
    private Location(
        LocationId id, 
        LocationName name, 
        Address address)
    {
        Id = id;
        Name = name;
        Address = address;
    }
    public LocationId Id { get; private set; }
    public LocationName Name { get; private set; }
    public Address Address { get; private set; }
    /// <summary>
    /// коллекция записей статистики для сохранения
    /// </summary>
    private readonly List<Statistica> _stats = [];

    public static Location Create(
        LocationName name, 
        Address address)
    {
        Location newObject = new(new LocationId(Guid.CreateVersion7()), name, address);
        newObject._stats.Add(Statistica.AddStatistics(newObject.Id.Value, newObject.GetType().Name, Statistica.Level.INFO, Statistica.Action.CREATE, $"Создание локации {newObject.Name}"));

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
        Address? address)
    {
        bool result = false;
        if (name != null && Name != name)
        {
            _stats.Add(Statistica.AddStatistics(Id.Value, this.GetType().Name, Statistica.Level.FINE, Statistica.Action.UPDATE, $"Название изменено с {Name} на {name}"));
            Name = name;
            result = true;
        }
        if (address != null && Address != address)
        {
            _stats.Add(Statistica.AddStatistics(Id.Value, this.GetType().Name, Statistica.Level.FINE, Statistica.Action.UPDATE, $"Адрес изменен с {Address} на {address}"));
            Address = address;

            result = true;
        }

        return result;
    }
}
