using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.PositionsMatrix;
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
        DepartmentPositionId chiefDepartmentPositionId, 
        IEnumerable<DepartmentPosition>? departmentPositions,
        IEnumerable<DepartmentLocation>? departmentLocations)
    {
        Id = id;
        Name = name;
        Slug = slug;
        ParentId = parentId;
        PathSlug = pathSlug;
        ChiefDepartmentPositionId = chiefDepartmentPositionId;
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
    /// id должности начальника департамента
    /// </summary>
    public DepartmentPositionId ChiefDepartmentPositionId { get; private set; }
    private readonly DepartmentPosition _chiefDepartmentPosition = null!;
    /// <summary>
    /// матричная должность начальника департамента. Остальные должности департамента должны быть потомками от этой
    /// </summary>
    public DepartmentPosition ChiefDepartmentPosition => _chiefDepartmentPosition;
    private readonly List<DepartmentPosition> _departmentPositions = [];
    private readonly List<DepartmentLocation> _departmentLocations = [];
 
    /// <summary>
    /// коллекция записей статистики для сохранения
    /// </summary>
    private readonly List<Statistica> _stats = [];

    private Department() { }
    private Department(DepartmentId id, DepartmentName name, Slug slug, Department? parent, DepartmentPosition chiefDepartmentPosition)
    {
        Id = id;
        Name = name;
        Slug = slug;
        PathSlug = parent?.PathSlugFull;
        ParentId = parent?.Id;
        _parent = parent;
        _childs = [];
        _chiefDepartmentPosition = chiefDepartmentPosition;
        _departmentPositions = [];
        _departmentLocations = [];
        _stats = [];
    }

    public static Department Create(
        DepartmentId id,
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
            if (!chiefPosition.isParent(parent.ChiefDepartmentPosition.PositionMatrix))
            {
                throw new DSException("Для подчинённого департамента в качестве руководителя должна быть выбрана должность, подчинённая руководителю родительского департамента");
            }
        }

        DepartmentId objectId;
        if (id == null)
            objectId = new DepartmentId(Guid.CreateVersion7());
        else
            objectId = id;

        DepartmentPosition objectDP = new DepartmentPosition(new DepartmentPositionId(Guid.CreateVersion7()), objectId, chiefPosition.Id);

        Department newObject = new(
            objectId,
            name,
            slug,
            parent,
            objectDP);

        newObject._stats.Add(Statistica.AddStatistics(
            newObject.Id.Value, 
            newObject.GetType().Name, 
            Statistica.Level.INFO, 
            Statistica.Action.CREATE, 
            $"Создание департамента {newObject.Name}"));

        newObject._parent = parent;
        if (parent != null)
        {
            parent._childs.Add(newObject);
            newObject._stats.Add(Statistica.AddStatistics(
                newObject.Id.Value, 
                newObject.GetType().Name, 
                Statistica.Level.INFO, 
                Statistica.Action.ATTACH, 
                $"Вышестоящий департамент: {parent.Name}", 
                parent.Id.Value, 
                parent.GetType().Name));
        }

        return newObject;
    }
    /// <summary>
    /// добавить связку департамент-должность
    /// </summary>
    /// <param name="positionMatrix"></param>
    /// <returns></returns>
    /// <exception cref="DSException"></exception>
    public DepartmentPosition LinkPosition(PositionMatrix positionMatrix)
    {
        if(!positionMatrix.isParent(this._chiefDepartmentPosition.PositionMatrix))
        {
            throw new DSException("Добавляемая должность должна быть подчинённой руководителю департамента");
        }
        DepartmentPosition newLink = new DepartmentPosition(this, positionMatrix);
        _departmentPositions.Add(newLink);
        _stats.Add(Statistica.AddStatistics(
            positionMatrix.Id.Value, 
            positionMatrix.GetType().Name, 
            Statistica.Level.INFO, 
            Statistica.Action.UPDATE, 
            $"Присоединена должность {positionMatrix.Name}", 
            Id.Value,
            GetType().Name));

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
        this._departmentLocations.Add(newLink);
        _stats.Add(Statistica.AddStatistics(
            location.Id.Value, 
            location.GetType().Name, 
            Statistica.Level.INFO, 
            Statistica.Action.UPDATE, 
            $"Присоединена локация {location.Name}", 
            Id.Value, 
            GetType().Name));
        return newLink;
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

        if (_parent != null)
        {
            _stats.Add(Statistica.AddStatistics(
                Id.Value,
                this.GetType().Name,
                Statistica.Level.INFO,
                Statistica.Action.DETACH,
                $"Отсоединён от {PathSlug}",
                _parent.Id.Value,
                _parent.GetType().Name));
        }

        if (newParent != null)
        {
            ParentId = newParent.Id;
            PathSlug = newParent.PathSlugFull;
            _stats.Add(Statistica.AddStatistics(
                Id.Value, 
                this.GetType().Name, 
                Statistica.Level.INFO, 
                Statistica.Action.ATTACH, 
                $"Присоединён к {newParent.PathSlugFull}", 
                newParent.Id.Value, 
                newParent.GetType().Name));
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
    /// <param name="name"></param>
    /// <param name="slug"></param>
    /// <returns>true, если были изменения</returns>
    public bool Update(DepartmentName? name, Slug? slug)
    {
        bool result = false;
        if (name != null && Name != name)
        {
            _stats.Add(Statistica.AddStatistics(
                Id.Value, 
                this.GetType().Name, 
                Statistica.Level.FINE, 
                Statistica.Action.UPDATE, 
                $"Название изменено с {Name} на {name}"));
            Name = name;
            result = true;
        }
        if (slug != null && Slug != slug)
        {
            _stats.Add(Statistica.AddStatistics(
                Id.Value, 
                this.GetType().Name, 
                Statistica.Level.FINE, 
                Statistica.Action.UPDATE, 
                $"Идентификатор изменен с {Slug} на {slug}"));
            Slug = slug;
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
                _stats.Add(Statistica.AddStatistics(
                    Id.Value, 
                    this.GetType().Name, 
                    Statistica.Level.FINEST, 
                    Statistica.Action.UPDATE, 
                    $"Переподчинение {PathSlug}"));
                result = true;
            }
        }
        else
        {
            if (ParentId != null)
            {
                if (PathSlug != null)
                {
                    _stats.Add(Statistica.AddStatistics(
                        Id.Value, 
                        this.GetType().Name, 
                        Statistica.Level.FINEST, 
                        Statistica.Action.UPDATE, 
                        $"Становится корневым"));
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
    public bool Delete()
    {
        if (Childs != null && Childs.Count > 0)
        {
            throw new DSException("Сначала необходимо удалить зависимые департаменты");
        }
        _stats.Add(Statistica.AddStatistics(
            Id.Value, 
            this.GetType().Name, 
            Statistica.Level.INFO, 
            Statistica.Action.DELETE, 
            $"Удаление: {Name}"));

        if (_parent != null)
            _parent._childs?.Remove(this);

        return true;
    }
}
