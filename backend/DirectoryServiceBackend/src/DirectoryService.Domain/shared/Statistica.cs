using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Domain.shared;

/// <summary>
/// Статистика изменения состояния объекта. Предназначена для "разбора полётов", вывода по запросу пользователя по уровню детализации.
/// Не привязана ни к одной таблице, не должна удаляться автоматически. Очистка записей статистики должна определяться внутренними регламентами
/// (например, удаление записей статистики, относящихся к удалённым объектам через год после удаления)
/// </summary>
public sealed class Statistica
{
    private Statistica(
        Guid id, 
        Guid objectId, 
        string objectTypeName, 
        string userName , 
        Level level, 
        Action action, 
        string description, 
        DateTime dateTime, 
        Guid? parentId, 
        string? parentTypeName)
    {
        this.Id = id;
        this.objectId = objectId;
        this.objectTypeName = objectTypeName;
        this.userName = userName;
        this.level = level;
        this.action = action;
        this.description = description;
        this.dateTime = dateTime;
        this.parentId = parentId;
        this.parentTypeName = parentTypeName;
    }

    public Guid Id { get; private set; }
    /// <summary>
    /// Id объекта - экземпляра любой сущности
    /// </summary>
    public Guid objectId { get; private set; }
    /// <summary>
    /// имя класса объекта
    /// </summary>
    public string objectTypeName { get; private set; }
    /// <summary>
    /// имя пользователя, инициирующего событие
    /// </summary>
    public string userName { get; private set; }
    /// <summary>
    /// уровень отображения статистики
    /// </summary>
    public Level level { get; private set; }
    /// <summary>
    /// действие
    /// </summary>
    public Action action { get; private set; }
    /// <summary>
    /// описание события
    /// </summary>
    public string description { get; private set; }
    /// <summary>
    /// время события
    /// </summary>
    public DateTime dateTime { get; private set; }
    /// <summary>
    /// опционально. Id родительского объекта
    /// </summary>
    public Guid? parentId { get; private set; }
    /// <summary>
    /// опционально. Имя типа родительского объекта
    /// </summary>
    public string? parentTypeName { get; private set; }

    /// <summary>
    /// добавить запись статистики
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="objectTypeName"></param>
    /// <param name="level"></param>
    /// <param name="action"></param>
    /// <param name="description"></param>
    /// <param name="parentId"></param>
    /// <param name="parentTypeName"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    public static Statistica AddStatistics(
        Guid objectId, 
        string objectTypeName, 
        Level level, 
        Action action, 
        string description, 
        Guid? parentId, 
        string? parentTypeName)
    {
        if (parentId == null || parentTypeName == null)
            switch (action)
            {
                case Action.ATTACH:
                    throw new ArgumentNullException("parentId", "Для операции \"Присоединить\" не заданы параметры родительского объекта");
                case Action.DETACH:
                    throw new ArgumentNullException("parentId", "Для операции \"Отсоединить\" не заданы параметры родительского объекта");
            }
        return new Statistica(
            Guid.CreateVersion7(), 
            objectId, 
            objectTypeName, 
            Environment.UserName, 
            level, 
            action, 
            description, 
            DateTime.Now, 
            parentId, 
            parentTypeName);
    }

    /// <summary>
    /// добавить запись статистики
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="objectTypeName"></param>
    /// <param name="level"></param>
    /// <param name="action"></param>
    /// <param name="description"></param>
    /// <returns></returns>
    public static Statistica AddStatistics(
        Guid objectId, 
        string objectTypeName, 
        Level level, 
        Action action, 
        string description)
    {
        return AddStatistics(
            objectId, 
            objectTypeName, 
            level, 
            action, 
            description, 
            null, 
            null);
    }

 
    /// <summary>
    /// Уровни статистики для фильтрации отображения
    /// </summary>
    public enum Level
    {
        INFO = 10,
        FINE = 20,
        FINEST = 30,
        SYSTEM = 99
    }

    /// <summary>
    /// Типовые действия
    /// </summary>
    public enum Action
    {
        CREATE = 10,
        UPDATE = 20,
        ATTACH = 30,
        DETACH = 40,
        DELETE = 50,
        OTHER = 100
    }
}
