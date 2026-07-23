using CSharpFunctionalExtensions;
using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
    public PathSlug PathSlugFull
    {
        get
        {
            var result = PathSlug == null ? PathSlug.Create(Slug) : PathSlug.CreateChild(Slug);
            if (result.IsFailure)
                throw new InvalidOperationException("Не удалось создать PathSlugFull.");
            return result.Value;
        }
    }

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
        PositionMatrix chiefPosition)
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
        newObject._parent = parent;
        if (parent != null)
        {
            parent._childs.Add(newObject);
        }

        return newObject;
    }
    public void SetParent(Department parent)
    {
        if (this.ParentId != parent.Id)
            _parent = parent;
        if (!_parent.Childs.Any(v => v.Id == this.Id))
            _parent.AddChild(this);
    }
    public void AddChild(Department child)
    {
        child.SetParent(this);
    }
    public void AddChilds(List<Department> childs)
    {
        foreach (var child in childs)
        {
            child.SetParent(this);
        }
    }

    /// <summary>
    /// добавить связку департамент-должность
    /// </summary>
    /// <param name="positionMatrix"></param>
    /// <returns></returns>
    /// <exception cref="DSException"></exception>
    public DepartmentPosition LinkPosition(PositionMatrix positionMatrix)
    {
        if(!positionMatrix.isParent(this.ChiefPositionMatrix))
        {
            throw new DSException("Добавляемая должность должна быть подчинённой руководителю департамента");
        }
        DepartmentPosition newLink = new DepartmentPosition(this, positionMatrix);
        _departmentPositions.Add(newLink);

        return newLink;
    }

    /// <summary>
    /// добавить связку департамент-локация
    /// </summary>
    /// <param name="location"></param>
    /// <returns></returns>
    /// <exception cref="DSException"></exception>
    public DepartmentLocation LinkLocation(Location location)
    {
        foreach (DepartmentLocation loc in _departmentLocations)
        {
            if (loc.Location.Id == location.Id)
            {
                throw new DSException("Дублирование связи с локацией");
            }
        }

        DepartmentLocation newLink = new DepartmentLocation(this, location);
        _departmentLocations.Add(newLink);

        return newLink;
    }
    /// <summary>
    /// добавить коллекцию связей с локациями
    /// </summary>
    /// <param name="locations"></param>
    /// <returns></returns>
    public IReadOnlyList<DepartmentLocation> LinkLocations(IEnumerable<Location> locations)
    {
        foreach (Location loc in locations)
            LinkLocation(loc);

        return this.DepartmentLocations;
    }

    public bool DetachLocation(Location location)
    {
        foreach (DepartmentLocation loc in _departmentLocations)
        {
            if (loc.Location.Id == location.Id)
            {
                _departmentLocations.Remove(loc);
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// заменить родителя (null - становится головным)
    /// </summary>
    /// <param name="newParent"></param>
    /// <returns>true, если были изменения</returns>
    public bool Move(Department? newParent)
    {
        if ((_parent == null ? Guid.Empty : _parent.Id.Value) == (newParent == null ? Guid.Empty : newParent.Id.Value))
        {
            return false;
        }

        if (newParent != null)
        {
            ParentId = newParent.Id;
            PathSlug = newParent.PathSlugFull;
        }
        bool result = false;
        Department? oldParent = _parent; 
        _parent = newParent;

        if(oldParent != null && oldParent.refresh())
            result = true;
        if(this.refresh())
            result = true;
        return result;
    }
    /// <summary>
    /// изменить
    /// </summary>
    /// <param name="newName"></param>
    /// <param name="newSlug"></param>
    /// <param name="newPathSlug"></param>
    /// <param name="newChiefPositionMatrix"></param>
    /// <returns>true, если были изменения</returns>
    public bool Update(
        DepartmentName? newName,
        Slug? newSlug,
        DepartmentChiefPosition? newDepartmentChiefPosition)
    {
        bool result = false;
        if (newName != null && newName != Name)
        {
            Name = newName;
            result = true;
        }
        if(newDepartmentChiefPosition != null && DepartmentChiefPosition.PositionMatrixId != newDepartmentChiefPosition.PositionMatrixId)
        {
            DepartmentChiefPosition = newDepartmentChiefPosition;
        }
        if ((newSlug != null && newSlug != Slug) || (this.Parent != null && this.Parent.PathSlugFull != PathSlug))
        {
            if (newSlug != null && newSlug != Slug)
                Slug = newSlug;
            if (Childs != null && Childs.Select(child =>
            {
                bool v = child.refresh();
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
    private bool refresh()
    {
        bool result = false;

        if (_parent != null)
        {
            ParentId = _parent.Id;
            if (PathSlug != _parent.PathSlugFull)
            {
                PathSlug = _parent.PathSlugFull;
                result = true;
            }
        }
        else
        {
            if (ParentId != null)
                result = true;
            ParentId = null;
            PathSlug = null;
        }

        if (Childs != null && Childs.Select(child =>
        {
            bool v;
            if (child.ParentId != this.Id)
                v = _childs.Remove(child);
            else
                v = child.refresh();
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
