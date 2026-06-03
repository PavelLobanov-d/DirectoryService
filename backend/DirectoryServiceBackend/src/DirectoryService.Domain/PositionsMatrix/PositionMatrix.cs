using DirectoryService.Domain.shared;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace DirectoryService.Domain.PositionsMatrix;

/// <summary>
/// определяет матричную должность и структуру подчинения. Применяется ко всем департаментам
/// </summary>
public sealed class PositionMatrix
{
    private PositionMatrix() { }
    private PositionMatrix(
        PositionMatrixId id, 
        PositionName name, 
        Slug slug, 
        PositionMatrixId? parentId, 
        PathSlug? parentPath)
    {
        Id = id;
        Name = name;
        Slug = slug;
        ParentId = parentId;
        PathSlug = parentPath;
    }

    public PositionMatrixId Id { get; private set; }
    /// <summary>
    /// название должности
    /// </summary>
    public PositionName Name { get; private set; }
    /// <summary>
    /// для организации дерева подчинённости
    /// </summary>
    public Slug Slug { get; private set; }
    /// <summary>
    /// иерархия родительских Slug'ов
    /// </summary>
    public PathSlug? PathSlug { get; private set; }
    public PathSlug PathSlugFull => PathSlug == null ? PathSlug.Create(Slug) : PathSlug.CreateChild(Slug);
    /// <summary>
    /// вышестоящая должность
    /// </summary>
    public PositionMatrixId? ParentId { get; private set; }
    
    private PositionMatrix? _parent;
    /// <summary>
    /// родитель
    /// </summary>
    public PositionMatrix? Parent => _parent;

    private readonly List<PositionMatrix> _childs = [];
    /// <summary>
    /// потомки
    /// </summary>
    public IReadOnlyList<PositionMatrix> Childs => _childs;
    /// <summary>
    /// коллекция записей статистики для сохранения
    /// </summary>
    private readonly List<Statistica> _stats = [];

    /// <summary>
    /// добавить матричную должность
    /// </summary>
    /// <param name="name"></param>
    /// <param name="slug"></param>
    /// <param name="parent"></param>
    /// <returns>новый объект</returns>
    /// <exception cref="ArgumentException"></exception>
    public static PositionMatrix Create(
        PositionName name, 
        Slug slug, 
        PositionMatrix? parent)
    {
        PositionMatrix newObject = new PositionMatrix(
            new PositionMatrixId(Guid.CreateVersion7()), 
            name, 
            slug, 
            parent?.Id, 
            parent?.PathSlugFull);
        newObject._parent = parent;

        newObject._stats.Add(Statistica.AddStatistics(
            newObject.Id.Value, 
            newObject.GetType().Name, 
            Statistica.Level.INFO, 
            Statistica.Action.CREATE, 
            $"Создание матричной должности {newObject.Name}"));
        if(parent != null)
        {
            parent._childs.Add(newObject);
            newObject._stats.Add(Statistica.AddStatistics(
                newObject.Id.Value, 
                newObject.GetType().Name, 
                Statistica.Level.INFO, 
                Statistica.Action.ATTACH, 
                $"Родительская должность: {parent.Name}"));
        }

        return newObject;
    }

    /// <summary>
    /// заменить родителя (null - становится головной должностью)
    /// </summary>
    /// <param name="newParent"></param>
    /// <returns>true, если были изменения</returns>
    public bool Move(PositionMatrix? newParent)
    {
        //надо сделать пересчёт частных должностей, привязанных к этой матричной

        if (ParentId == newParent?.Id)
        {
            return false;
        }

        if(_parent != null)
        {
            _parent._childs.Remove(this);

            _stats.Add(Statistica.AddStatistics(
                Id.Value, 
                this.GetType().Name, 
                Statistica.Level.INFO, 
                Statistica.Action.DETACH, 
                $"Отсоединён от {PathSlug}", 
                _parent.Id.Value, 
                _parent.GetType().Name));
        }

        if(newParent != null)
        {
            ParentId = newParent.Id;
            PathSlug = newParent.PathSlugFull;
            newParent._childs.Add(this);

            _stats.Add(Statistica.AddStatistics(
                Id.Value, 
                this.GetType().Name, 
                Statistica.Level.INFO, 
                Statistica.Action.ATTACH, 
                $"Присоединён к {newParent.PathSlugFull}", 
                newParent.Id.Value, 
                newParent.GetType().Name));
        }

        _parent = newParent;
        return refresh();
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
                ParentId = null;
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
            PathSlug = null;
        }

        if (_childs != null && _childs.Select(child =>
            {
                bool v = child.refresh();
                return v;
            }).Contains(true))
            result = true;

        return result;
    }
    /// <summary>
    /// изменить
    /// </summary>
    /// <param name="name"></param>
    /// <param name="slug"></param>
    /// <returns>true, если были изменения</returns>
    public bool Update(PositionName? name, Slug? slug)
    {
        bool result = false;
        if (name != null && Name != name)
        {
            validationName(name);
            _stats.Add(Statistica.AddStatistics(
                Id.Value, 
                this.GetType().Name, 
                Statistica.Level.INFO, 
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
                Statistica.Level.INFO, 
                Statistica.Action.UPDATE, 
                $"Идентификатор изменен с {Slug} на {slug}"));
            Slug = slug;

            if (_childs != null && _childs.Select(child =>
            {
                bool v = child.refresh();
                return v;
            }).Contains(true))
                result = true;
        }

        return result;
    }
    /// <summary>
    /// удалить должность
    /// </summary>
    /// <returns>true, если были изменения</returns>
    /// <exception cref="Exception"></exception>
    public bool Delete()
    {
        //надо сделать проверку на существование частных должностей к этой матричной. Потом

        if (_childs != null && _childs.Count > 0)
        {
            throw new DSException("Сначала необходимо удалить зависимые матричные должности");
        }
        _stats.Add(Statistica.AddStatistics(
            Id.Value, 
            this.GetType().Name, 
            Statistica.Level.INFO, 
            Statistica.Action.DELETE, 
            $"Удаление: {Name}"));

        if(_parent != null)
            _parent.Childs?.ToList().Remove(this);

        return true;
    }
    /// <summary>
    /// проверка, чтобы не было одинаковых названий должностей с одним родителем
    /// </summary>
    /// <returns></returns>
    private void validationName(PositionName name)
    {
        if(_parent != null && _parent.Childs != null)
        {
            foreach(PositionMatrix position in _parent.Childs)
            {
                if (position.Id != this.Id && position.Name == name)
                {
                    throw new DSException("Не уникальное название должности");
                }
            }
        }
    }
    /// <summary>
    /// проверяет, является ли должность родителем этой
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public bool isParent(PositionMatrixId id)
    {
        if (this.Id == id)
        {
            return true;
        }
        else if (Parent != null)
        {
            return Parent.isParent(id);
        }
        return false;
    }
    public bool isParent(PositionMatrix position)
    {
        return isParent(position.Id);
    }
}
