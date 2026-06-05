using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Domain.shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace DirectoryService.Domain.Departments;

public sealed class Department
{
    private Department(
        DepartmentId id,
        DepartmentName name,
        Slug slug,
        DepartmentId? parentId,
        PathSlug pathSlug,
        IEnumerable<DepartmentPosition>? departmentPositions,
        IEnumerable<DepartmentLocation>? departmentLocations)
    {
        Id = id;
        Name = name;
        Slug = slug;
        ParentId = parentId;
        PathSlug = pathSlug;
        _departmentPositions = departmentPositions?.ToList();
        _departmentLocations = departmentLocations?.ToList();
    }
    public DepartmentId Id { get; private set; }
    public DepartmentName Name { get; private set; }
    public Slug Slug { get; private set; }
    public PathSlug? PathSlug { get; private set; } = null;
    public PathSlug PathSlugFull => PathSlug == null ? PathSlug.Create(Slug) : PathSlug.CreateChild(Slug);
    public DepartmentId? ParentId { get; private set; }
    private Department? _parent = null;
    /// <summary>
    /// родитель
    /// </summary>
    public Department? Parent => _parent;
    private readonly List<Department> _childs = [];
    /// <summary>
    /// потомки
    /// </summary>
    public IReadOnlyList<Department> Childs => _childs;
    /// <summary>
    /// связь с должностью начальника департамента
    /// </summary>
    public DepartmentChiefPosition DepartmentChiefPosition {  get; private set; }

    /// <summary>
    /// должность начальника департамента
    /// </summary>
    public PositionMatrix ChiefPositionMatrix => DepartmentChiefPosition.PositionMatrix;

    private readonly List<DepartmentPosition> _departmentPositions = [];
    public IReadOnlyList<DepartmentPosition> DepartmentPositions => _departmentPositions;
    private readonly List<DepartmentLocation> _departmentLocations = [];
    public IReadOnlyList<DepartmentLocation> DepartmentLocations => _departmentLocations;

    private Department() { }
    private Department(DepartmentId id, DepartmentName name, Slug slug, Department? parent, DepartmentChiefPosition chiefDepartmentPosition)
    {
        Id = id;
        Name = name;
        Slug = slug;
        PathSlug = parent?.PathSlugFull;
        ParentId = parent?.Id;
        _parent = parent;
        _childs = [];
        DepartmentChiefPosition = chiefDepartmentPosition;
        _departmentPositions = [];
        _departmentLocations = [];
    }

    public static Department Create(
        DepartmentName name,
        Slug slug,
        Department? parent,
        PositionMatrix chiefPosition,
        GlobalStatistics globalstats)
    {
        if (parent == null)
        {
            if(chiefPosition.Parent != null)
            {
                throw new DSException("Для департамента верхнего уровня в качестве руководителя должна быть выбрана должность верхнего уровня");
            }
        }
        else
        {
            if (chiefPosition.Parent == null)
            {
                throw new DSException("Для подчинённого департамента в качестве руководителя не может быть выбрана должность верхнего уровня");
            }
            if (!chiefPosition.isParent(parent.DepartmentChiefPosition.PositionMatrix))
            {
                throw new DSException("Для подчинённого департамента в качестве руководителя должна быть выбрана должность, подчинённая руководителю родительского департамента");
            }
        }

        DepartmentId objectId = new DepartmentId(Guid.CreateVersion7());
        DepartmentChiefPosition objectDP = DepartmentChiefPosition.Create(objectId, chiefPosition);

        Department newObject = new(
            objectId,
            name,
            slug,
            parent,
            objectDP);

        newObject.DepartmentChiefPosition = objectDP;

        globalstats.AddStatistica(
            newObject.Id.Value, 
            newObject.GetType().Name, 
            Statistica.Level.INFO, 
            Statistica.Action.CREATE, 
            $"Создание департамента {newObject.Name.Value}");

        newObject._parent = parent;
        if (parent != null)
        {
            parent._childs.Add(newObject);
            globalstats.AddStatistica(
                newObject.Id.Value, 
                newObject.GetType().Name, 
                Statistica.Level.INFO, 
                Statistica.Action.ATTACH, 
                $"Вышестоящий департамент: {parent.Name.Value}", 
                parent.Id.Value, 
                parent.GetType().Name);
        }

        return newObject;
    }
    /// <summary>
    /// добавить связку департамент-должность
    /// </summary>
    /// <param name="positionMatrix"></param>
    /// <returns></returns>
    /// <exception cref="DSException"></exception>
    public DepartmentPosition LinkPosition(PositionMatrix positionMatrix,
        GlobalStatistics globalstats)
    {
        if(!positionMatrix.isParent(this.ChiefPositionMatrix))
        {
            throw new DSException("Добавляемая должность должна быть подчинённой руководителю департамента");
        }
        DepartmentPosition newLink = new DepartmentPosition(this, positionMatrix);
        _departmentPositions.Add(newLink);
        globalstats.AddStatistica(
            positionMatrix.Id.Value, 
            positionMatrix.GetType().Name, 
            Statistica.Level.INFO, 
            Statistica.Action.UPDATE, 
            $"Присоединена должность {positionMatrix.Name.Value}", 
            Id.Value,
            GetType().Name);

        return newLink;
    }

    /// <summary>
    /// добавить связку департамент-локация
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    /// <exception cref="DSException"></exception>
    public DepartmentLocation LinkLocation(Location location,
        GlobalStatistics globalstats)
    {
        foreach (DepartmentLocation loc in _departmentLocations)
        {
            if (loc.Location.Id == location.Id)
            {
                throw new DSException("Дублирование связи с локацией");
            }
        }

        DepartmentLocation newLink = new DepartmentLocation(this, location);
        this._departmentLocations.Add(newLink);
        globalstats.AddStatistica(
            location.Id.Value,
            location.GetType().Name,
            Statistica.Level.INFO,
            Statistica.Action.UPDATE,
            $"Присоединена локация {location.Name.Value}",
            Id.Value,
            GetType().Name);
        return newLink;
    }

    /// <summary>
    /// заменить родителя (null - становится головным)
    /// </summary>
    /// <param name="newParent"></param>
    /// <returns>true, если были изменения</returns>
    public bool Move(Department? newParent,
        GlobalStatistics globalstats)
    {
        if ((_parent == null ? Guid.Empty : _parent.Id.Value) == (newParent == null ? Guid.Empty : newParent.Id.Value))
        {
            return false;
        }

        if (_parent != null)
        {
            globalstats.AddStatistica(
                Id.Value,
                this.GetType().Name,
                Statistica.Level.INFO,
                Statistica.Action.DETACH,
                $"Отсоединён от {PathSlug.Value}",
                _parent.Id.Value,
                _parent.GetType().Name);
        }

        if (newParent != null)
        {
            ParentId = newParent.Id;
            PathSlug = newParent.PathSlugFull;
            globalstats.AddStatistica(
                Id.Value, 
                this.GetType().Name, 
                Statistica.Level.INFO, 
                Statistica.Action.ATTACH, 
                $"Присоединён к {newParent.PathSlugFull.Value}", 
                newParent.Id.Value, 
                newParent.GetType().Name);
        }
        bool result = false;
        Department? oldParent = _parent; 
        _parent = newParent;

        if(oldParent != null && oldParent.refresh(globalstats))
            result = true;
        if(this.refresh(globalstats))
            result = true;
        return result;
    }
    /// <summary>
    /// изменить
    /// </summary>
    /// <param name="name"></param>
    /// <param name="slug"></param>
    /// <returns>true, если были изменения</returns>
    public bool Update(DepartmentName? name,
        Slug? slug,
        GlobalStatistics globalstats)
    {
        bool result = false;
        if (name != null && Name != name)
        {
            globalstats.AddStatistica(
                Id.Value, 
                this.GetType().Name, 
                Statistica.Level.FINE, 
                Statistica.Action.UPDATE, 
                $"Название изменено с {Name.Value} на {name.Value}");
            Name = name;
            result = true;
        }
        if (slug != null && Slug != slug)
        {
            globalstats.AddStatistica(
                Id.Value, 
                this.GetType().Name, 
                Statistica.Level.FINE, 
                Statistica.Action.UPDATE, 
                $"Идентификатор изменен с {Slug.Value} на {slug.Value}");
            Slug = slug;
            if (Childs != null && Childs.Select(child =>
            {
                bool v = child.refresh(globalstats);
                return v;
            }).Contains(true))
                result = true;
        }

        return result;
    }

    /// <summary>
    /// пересчёт
    /// </summary>
    /// <returns>true, если были изменения</returns>
    private bool refresh(GlobalStatistics globalstats)
    {
        bool result = false;

        if (_parent != null)
        {
            ParentId = _parent.Id;
            if (PathSlug != _parent.PathSlugFull)
            {
                PathSlug = _parent.PathSlugFull;
                globalstats.AddStatistica(
                    Id.Value, 
                    this.GetType().Name, 
                    Statistica.Level.FINEST, 
                    Statistica.Action.UPDATE, 
                    $"Переподчинение {PathSlug.Value}");
                result = true;
            }
        }
        else
        {
            if (ParentId != null)
            {
                if (PathSlug != null)
                {
                    globalstats.AddStatistica(
                        Id.Value, 
                        this.GetType().Name, 
                        Statistica.Level.FINEST, 
                        Statistica.Action.UPDATE, 
                        $"Становится корневым");
                }
                result = true;
            }
            ParentId = null;
            PathSlug = null;
        }

        if (Childs != null && Childs.Select(child =>
        {
            bool v;
            if (child.ParentId != this.Id)
                v = _childs.Remove(child);
            else
                v = child.refresh(globalstats);
            return v;
        }).Contains(true))
            result = true;

        return result;
    }

    /// <summary>
    /// удалить департамент
    /// </summary>
    /// <returns>true, если были изменения</returns>
    /// <exception cref="DSException"></exception>
    public bool Delete(GlobalStatistics globalstats)
    {
        if (Childs != null && Childs.Count > 0)
        {
            throw new DSException("Сначала необходимо удалить зависимые департаменты");
        }
        globalstats.AddStatistica(
            Id.Value, 
            this.GetType().Name, 
            Statistica.Level.INFO, 
            Statistica.Action.DELETE, 
            $"Удаление: {Name.Value}");

        if (_parent != null)
            _parent._childs?.Remove(this);

        return true;
    }
}
