using DirectoryService.Domain.Positions;
using DirectoryService.Domain.shared;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Domain.Departments
{
    internal class Department
    {
        private Department(DepartmentId id, DepartmentName name, Slug slug, Department? parent, List<Department>? childs, PositionMatrix chiefPosition)
        {
            this.id = id;
            Name = name;
            Slug = slug;
            _parent = parent;
            if (_parent != null)
            {
                ParentId = _parent.id;
                PathSlug = _parent.PathSlugFull;
            }
            else
            {
                ParentId = null;
                PathSlug = null;
            }

            _childs = childs;
            _chiefPosition = chiefPosition;
        }
        public DepartmentId id {  get; private set; }
        public DepartmentName Name { get; private set; }
        public Slug Slug { get; private set; }
        public Slug? PathSlug { get; private set; }
        public Slug PathSlugFull => PathSlug == null ? Slug : Slug.Create(PathSlug.Value + "." + Slug.Value);
        public DepartmentId? ParentId { get; private set; }
        private Department? _parent = null;
        /// <summary>
        /// родитель
        /// </summary>
        public Department? Parent => _parent;

        private readonly List<Department>? _childs;
        /// <summary>
        /// потомки
        /// </summary>
        public IReadOnlyList<Department>? Childs => _childs;
        /// <summary>
        /// id матричной должности начальника департамента
        /// </summary>
        public Guid ChiefPasitionId { get; private set; }
        private PositionMatrix _chiefPosition;
        /// <summary>
        /// матричная должность начальника департамента. Остальные должности департамента должны быть потомками от этой
        /// </summary>
        public PositionMatrix ChiefPosition => _chiefPosition;

        /// <summary>
        /// коллекция записей статистики для сохранения
        /// </summary>
        private readonly List<Statistics> _stats = [];

        public static Department Create(DepartmentName name, Slug slug, Department? parent, PositionMatrix chief)
        {
            Department newObject = new Department(new DepartmentId(Guid.CreateVersion7()), name, slug, parent, null, chief);
            newObject._parent = parent;
            newObject._stats.Add(Statistics.AddStatistics(newObject.id.Value, newObject.GetType().Name, Statistics.Level.INFO, Statistics.Action.CREATE, $"Создание департамента {newObject.Name}"));
            if (parent != null)
                newObject._stats.Add(Statistics.AddStatistics(newObject.id.Value, newObject.GetType().Name, Statistics.Level.INFO, Statistics.Action.ATTACH, $"Вышестоящий департамент: {parent.Name}"));

            return newObject;
        }

        /// <summary>
        /// заменить родителя (null - становится головной должностью)
        /// </summary>
        /// <param name="newParent"></param>
        /// <returns>true, если были изменения</returns>
        public bool Mave(Department? newParent)
        {
            if ((_parent == null ? Guid.Empty : _parent.id.Value) == (newParent == null ? Guid.Empty : newParent.id.Value))
            {
                return false;
            }

            if (_parent != null)
            {
                _stats.Add(Statistics.AddStatistics(id.Value, this.GetType().Name, Statistics.Level.INFO, Statistics.Action.DETACH, $"Отсоединён от {PathSlug}", _parent.id.Value, _parent.GetType().Name));
            }

            if (newParent != null)
            {
                ParentId = newParent.id;
                PathSlug = newParent.PathSlugFull;
                _stats.Add(Statistics.AddStatistics(id.Value, this.GetType().Name, Statistics.Level.INFO, Statistics.Action.ATTACH, $"Присоединён к {newParent.PathSlugFull}", newParent.id.Value, newParent.GetType().Name));
            }

            _parent = newParent;
            return refresh();
        }
        /// <summary>
        /// изменить
        /// </summary>
        /// <param name="name"></param>
        /// <param name="slug"></param>
        /// <returns>true, если были изменения</returns>
        public bool Update(DepartmentName? name, Slug? slug)
        {
            bool result = false;
            if (name != null && Name != name)
            {
                _stats.Add(Statistics.AddStatistics(id.Value, this.GetType().Name, Statistics.Level.FINE, Statistics.Action.UPDATE, $"Название изменено с {Name} на {name}"));
                Name = name;
                result = true;
            }
            if (slug != null && Slug != slug)
            {
                _stats.Add(Statistics.AddStatistics(id.Value, this.GetType().Name, Statistics.Level.FINE, Statistics.Action.UPDATE, $"Идентификатор изменен с {Slug} на {slug}"));
                Slug = slug;
                if (_childs != null)
                {
                    foreach (Department child in _childs)
                        child.refresh();
                }
                result = true;
            }

            return result;
        }

        /// <summary>
        /// пересчёт матричной должности
        /// </summary>
        /// <returns>true, если были изменения</returns>
        private bool refresh()
        {
            bool result = false;

            if (_parent != null)
            {
                ParentId = _parent.id;
                if (PathSlug != _parent.PathSlugFull)
                {
                    PathSlug = _parent.PathSlugFull;
                    _stats.Add(Statistics.AddStatistics(id.Value, this.GetType().Name, Statistics.Level.FINEST, Statistics.Action.UPDATE, $"Переподчинение {PathSlug}"));
                    result = true;
                }
            }
            else
            {
                if (ParentId != null)
                {
                    if (PathSlug != null)
                    {
                        _stats.Add(Statistics.AddStatistics(id.Value, this.GetType().Name, Statistics.Level.FINEST, Statistics.Action.UPDATE, $"Становится корневым"));
                    }
                    result = true;
                }
                ParentId = null;
                PathSlug = null;
            }

            if (_childs != null)
            {
                foreach (Department child in _childs)
                {
                    if (child.refresh())
                        result = true;
                }
            }

            return result;
        }

        /// <summary>
        /// удалить департамент
        /// </summary>
        /// <returns>true, если были изменения</returns>
        /// <exception cref="Exception"></exception>
        public bool Delete()
        {
            if (_childs != null && _childs.Count > 0)
            {
                throw new Exception("Сначала необходимо удалить зависимые департаменты");
            }
            _stats.Add(Statistics.AddStatistics(id.Value, this.GetType().Name, Statistics.Level.INFO, Statistics.Action.DELETE, $"Удаление: {Name}"));

            if (_parent != null)
                _parent.Childs?.ToList().Remove(this);

            return true;
        }
    }
}
