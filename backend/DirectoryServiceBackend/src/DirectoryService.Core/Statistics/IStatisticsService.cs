using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Core.Statistics;

public interface IStatisticsService
{
    /// <summary>
    /// создать запись статистики
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="objectTypeName"></param>
    /// <param name="level"></param>
    /// <param name="action"></param>
    /// <param name="description"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<Guid, Error>> CreateAsync(
            Guid objectId,
            string objectTypeName,
            Statistica.Level level,
            Statistica.Action action,
            string description,
            CancellationToken cancellationToken);
    /// <summary>
    /// создать запись статистики
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="objectTypeName"></param>
    /// <param name="level"></param>
    /// <param name="action"></param>
    /// <param name="description"></param>
    /// <param name="parentId"></param>
    /// <param name="parentTypeName"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<Guid, Error>> CreateAsync(
            Guid objectId,
            string objectTypeName,
            Statistica.Level level,
            Statistica.Action action,
            string description,
            Guid? parentId,
            string? parentTypeName,
            CancellationToken cancellationToken);
    /// <summary>
    /// получить записи статистики по Id объекта
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<List<Statistica>, Error>> GetByObjectIdAsync(Guid objectId, CancellationToken cancellationToken);
    /// <summary>
    /// получить записи статистики по Id родительского объекта
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<List<Statistica>, Error>> GetByParentIdAsync(Guid parentId, CancellationToken cancellationToken);
}
