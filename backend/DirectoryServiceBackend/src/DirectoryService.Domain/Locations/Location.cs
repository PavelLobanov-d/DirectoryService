using DirectoryService.Domain.Departments;
using DirectoryService.Domain.shared;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DirectoryService.Domain.Locations
{
    internal class Location
    {
        private Location(Guid id, LocationName name, Address address)
        {
            this.id = id;
            Name = name;
            Address = address;
        }
        public Guid id { get; private set; }
        public LocationName Name { get; private set; }
        public Address Address { get; private set; }
        /// <summary>
        /// коллекция записей статистики для сохранения
        /// </summary>
        private readonly List<Statistics> _stats = [];

        public static Location Create(LocationName name,  Address address)
        {
            Location newObject = new(Guid.CreateVersion7(), name, address);
            newObject._stats.Add(Statistics.AddStatistics(newObject.id, newObject.GetType().Name, Statistics.Level.INFO, Statistics.Action.CREATE, $"Создание локации {newObject.Name}"));

            return newObject;
        }

        /// <summary>
        /// изменить
        /// </summary>
        /// <param name="name"></param>
        /// <param name="slug"></param>
        /// <returns>true, если были изменения</returns>
        public bool Update(LocationName? name, Address? address)
        {
            bool result = false;
            if (name != null && Name != name)
            {
                _stats.Add(Statistics.AddStatistics(id, this.GetType().Name, Statistics.Level.FINE, Statistics.Action.UPDATE, $"Название изменено с {Name} на {name}"));
                Name = name;
                result = true;
            }
            if (address != null && Address != address)
            {
                _stats.Add(Statistics.AddStatistics(id, this.GetType().Name, Statistics.Level.FINE, Statistics.Action.UPDATE, $"Адрес изменен с {Address} на {address}"));
                Address = address;

                result = true;
            }

            return result;
        }

    }
}
