using DirectoryService.Domain.shared;
using System;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace DirectoryService.Domain.Positions
{
    /// <summary>
    /// определяет матричную должность и структуру подчинения. Применяется ко всем департаментам
    /// </summary>
    internal class PositionMatrix
    {
        private PositionMatrix(Guid id, PositionName name, Slug slug, PositionMatrix? parent, List<PositionMatrix>? childs)
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
        }

        public Guid id { get; private set; }
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
        public Slug? PathSlug { get; private set; }
        public Slug PathSlugFull => PathSlug == null ? Slug : Slug.Create(PathSlug.Value + "." + Slug.Value);
        /// <summary>
        /// вышестоящая должность
        /// </summary>
        public Guid? ParentId { get; private set; }
        
        private PositionMatrix? _parent;
        /// <summary>
        /// родитель
        /// </summary>
        public PositionMatrix? Parent => _parent;

        private readonly List<PositionMatrix>? _childs;
        /// <summary>
        /// потомки
        /// </summary>
        public IReadOnlyList<PositionMatrix>? Childs => _childs;
        /// <summary>
        /// коллекция записей статистики для сохранения
        /// </summary>
        private readonly List<Statistics> _stats = [];

        /// <summary>
        /// добавить матричную должность
        /// </summary>
        /// <param name="name"></param>
        /// <param name="slug"></param>
        /// <param name="parent"></param>
        /// <returns>новый объект</returns>
        /// <exception cref="ArgumentException"></exception>
        public static PositionMatrix Create(PositionName name, Slug slug, PositionMatrix? parent)
        {
            PositionMatrix newObject = new(Guid.CreateVersion7(), name, slug, parent, null);
            newObject._parent = parent;
            newObject._stats.Add(Statistics.AddStatistics(newObject.id, newObject.GetType().Name, Statistics.Level.INFO, Statistics.Action.CREATE, $"Создание матричной должности {newObject.Name}"));
            if(parent != null)
                newObject._stats.Add(Statistics.AddStatistics(newObject.id, newObject.GetType().Name, Statistics.Level.INFO, Statistics.Action.ATTACH, $"Родительская должность: {parent.Name}"));

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

            if ((_parent == null ? Guid.Empty : _parent.id) == (newParent == null ? Guid.Empty : newParent.id))
            {
                return false;
            }

            if(_parent != null)
            {
                _stats.Add(Statistics.AddStatistics(id, this.GetType().Name, Statistics.Level.INFO, Statistics.Action.DETACH, $"Отсоединён от {PathSlug}", _parent.id, _parent.GetType().Name));
            }

            if(newParent != null)
            {
                ParentId = newParent.id;
                PathSlug = newParent.PathSlugFull;
                _stats.Add(Statistics.AddStatistics(id, this.GetType().Name, Statistics.Level.INFO, Statistics.Action.ATTACH, $"Присоединён к {newParent.PathSlugFull}", newParent.id, newParent.GetType().Name));
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
                ParentId = _parent.id;
                if (PathSlug != _parent.PathSlugFull)
                {
                    PathSlug = _parent.PathSlugFull;
                    _stats.Add(Statistics.AddStatistics(id, this.GetType().Name, Statistics.Level.FINEST, Statistics.Action.UPDATE, $"Переподчинение {PathSlug}"));
                    result = true;
                }                
            }
            else
            {
                if (ParentId.HasValue)
                {
                    ParentId = null;
                    if (PathSlug != null)
                    {
                        _stats.Add(Statistics.AddStatistics(id, this.GetType().Name, Statistics.Level.FINEST, Statistics.Action.UPDATE, $"Становится корневым"));
                    }
                    result = true;
                }
                PathSlug = null;
            }

            if (_childs != null)
            {
                foreach (PositionMatrix child in _childs)
                {
                    if (child.refresh())
                        result = true;
                }
            }

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
                _stats.Add(Statistics.AddStatistics(id, this.GetType().Name, Statistics.Level.INFO, Statistics.Action.UPDATE, $"Название изменено с {Name} на {name}"));
                Name = name;
                result = true;
            }
            if (slug != null && Slug != slug)
            {
                _stats.Add(Statistics.AddStatistics(id, this.GetType().Name, Statistics.Level.INFO, Statistics.Action.UPDATE, $"Идентификатор изменен с {Slug} на {slug}"));
                Slug = slug;
                if(_childs != null)
                {
                    foreach(PositionMatrix child in _childs)
                        child.refresh();
                }
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
                throw new Exception("Сначала необходимо удалить зависимые матричные должности");
            }
            _stats.Add(Statistics.AddStatistics(id, this.GetType().Name, Statistics.Level.INFO, Statistics.Action.DELETE, $"Удаление: {Name}"));

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
            if(_parent != null)
            {
                foreach(PositionMatrix position in _parent.Childs)
                {
                    if(position.id != this.id)
                    {
                        if(position.Name == name)
                        {
                            throw new ArgumentException("Не уникальное название должности");
                        }
                    }
                }
            }
        }
    }
}
