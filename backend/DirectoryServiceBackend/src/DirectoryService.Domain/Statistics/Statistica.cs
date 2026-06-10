using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Domain.Statistics;

/// <summary>
/// Статистика изменения состояния объекта. Предназначена для "разбора полётов", вывода по запросу пользователя по уровню детализации.
/// Не привязана ни к одной таблице, не должна удаляться автоматически. Очистка записей статистики должна определяться внутренними регламентами
/// (например, удаление записей статистики, относящихся к удалённым объектам через год после удаления)
/// </summary>
public sealed class Statistica
{
    private Statistica() { }
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
    public static Statistica Create(
        Guid objectId,
        string objectTypeName,
        Level level,
        Action action,
        string description,
        Guid? parentId = null,
        string? parentTypeName = null)
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
    /// Уровни статистики для фильтрации отображения
    /// </summary>
    public enum Level
    {
        /// <summary>
        /// общая информация
        /// </summary>
        INFO = 10,
        /// <summary>
        /// подробная информация
        /// </summary>
        FINE = 20,
        /// <summary>
        /// ещё более подробная информация
        /// </summary>
        FINEST = 30,
        /// <summary>
        /// информация для администраторов
        /// </summary>
        SYSTEM = 99
    }

    /// <summary>
    /// Типовые события
    /// </summary>
    public enum Action
    {
        /// <summary>
        /// при создании объекта
        /// </summary>
        CREATE = 10,
        /// <summary>
        /// при изменении объекта
        /// </summary>
        UPDATE = 20,
        /// <summary>
        /// при присоединении объекта
        /// </summary>
        ATTACH = 30,
        /// <summary>
        /// при отсоединении объекта
        /// </summary>
        DETACH = 40,
        /// <summary>
        /// при удалении объекта
        /// </summary>
        DELETE = 50,
        /// <summary>
        /// прочие события
        /// </summary>
        OTHER = 100
    }
}
