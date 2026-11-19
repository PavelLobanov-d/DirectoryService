using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;

namespace DirectoryService.Domain.PositionsMatrix;

/// <summary>
/// определяет матричную должность и структуру подчинения. Применяется ко всем департаментам
/// </summary>
public sealed class PositionMatrix : ISoftDeletable
{
    private PositionMatrix() { }
    private PositionMatrix(
        PositionMatrixId id, 
        PositionName name, 
        Slug slug, 
        PositionMatrix? parent)
    {
        Id = id;
        Name = name;
        Slug = slug;
        ParentId = parent?.Id;
        _parent = parent;
        Path = parent is null ? PathSlug.Create(slug).Value : parent.Path.CreateChild(slug).Value;
    }

    public PositionMatrixId Id { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public bool IsDeleted => DeletedAt.HasValue;

    public void SoftDelete()
    {
        DeletedAt ??= DateTime.UtcNow;
    }

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
    public PathSlug Path { get; private set; } = null!;
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

    private readonly List<DepartmentPosition> _departmentPositions = [];
    public IReadOnlyList<DepartmentPosition> DepartmentPositions => _departmentPositions;

    private readonly List<DepartmentChiefPosition> _departmentChiefPositions = [];
    public IReadOnlyList<DepartmentChiefPosition> DepartmentChiefPositions => _departmentChiefPositions;
    //public ICollection<DepartmentChiefPosition> DepartmentChiefPositions { get; set; } = new List<DepartmentChiefPosition>();

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
            parent);
        if (parent != null)
        {
            parent._childs.Add(newObject);
        }

        return newObject;
    }
    public void SetParent(PositionMatrix parent)
    {
        if(this.ParentId != parent.Id)
            _parent = parent;
        if(!_parent.Childs.Any(v => v.Id == this.Id))
            _parent.AddChild(this);
    }
    public void AddChild(PositionMatrix child)
    {
        child.SetParent(this);
    }
    public void AddChilds(List<PositionMatrix> childs)
    {
        foreach(var child in childs)
        {
            child.SetParent(this);
        }
    }
    /// <summary>
    /// заменить родителя (null - становится головной должностью)
    /// </summary>
    /// <param name="newParent"></param>
    /// <returns>true, если были изменения</returns>
    public bool Move(PositionMatrix newParent)
    {
        //надо сделать пересчёт частных должностей, привязанных к этой матричной

        if (ParentId == newParent.Id)
        {
            return false;
        }

        if(_parent != null)
        {
            _parent._childs.Remove(this);
        }

        if(newParent != null)
        {
            newParent._childs.Add(this);
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
            PathSlug expectedPath = _parent.Path.CreateChild(Slug).Value;
            if (Path != expectedPath)
            {
                Path = expectedPath;
                result = true;
            }
        }
        else
        {
            if (ParentId != null)
            {
                ParentId = null;
                result = true;
            }
            PathSlug rootPath = PathSlug.Create(Slug).Value;
            if (Path != rootPath)
            {
                Path = rootPath;
                result = true;
            }
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
    public bool Update(PositionName? name,
        Slug? slug)
    {
        bool result = false;
        bool slugChanged = slug != null && Slug != slug;
        bool parentPathChanged = Parent != null
            && Path != Parent.Path.CreateChild(Slug).Value;

        if (slugChanged)
        {
            Slug = slug!;
            result = true;
        }

        if ((slugChanged || parentPathChanged) && refresh())
        {
            result = true;
        }

        if (name != null && Name != name)
        {
            Name = name;
            result = true;
        }

        return result;
    }
    /// <summary>
    /// удалить должность
    /// </summary>
    /// <returns>true, если были изменения</returns>
    /// <exception cref="Exception"></exception>
    public bool Delete(GlobalStatistics globalstats)
    {
        //надо сделать проверку на существование частных должностей к этой матричной. Потом

        if (_childs != null && _childs.Count > 0)
        {
            throw new DSException("Сначала необходимо удалить зависимые матричные должности");
        }
        globalstats.AddStatistica(
            Id.Value, 
            this.GetType().Name, 
            Statistica.Level.INFO, 
            Statistica.Action.DELETE, 
            $"Удаление: {Name}");

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
    /// <summary>
    /// проверяет, является ли должность position родителем этой
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public bool isParent(PositionMatrix position)
    {
        return isParent(position.Id);
    }
}
