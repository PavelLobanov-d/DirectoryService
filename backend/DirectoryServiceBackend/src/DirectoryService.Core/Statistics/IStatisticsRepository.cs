using CSharpFunctionalExtensions;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Core.Statistics;

public interface IStatisticsRepository
{
    /// <summary>
    /// создать запись статистики
    /// </summary>
    /// <param name="statistica"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<Guid, Errors>> AddAsync(Statistica statistica, CancellationToken cancellationToken = default);
    /// <summary>
    /// получить записи статистики по Id объекта
    /// </summary>
    /// <param name="objectId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<List<Statistica>, Error>> GetByObjectIdAsync(Guid objectId, CancellationToken cancellationToken = default);
    /// <summary>
    /// получить записи статистики по Id родительского объекта
    /// </summary>
    /// <param name="parentId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<List<Statistica>, Error>> GetByParentIdAsync(Guid parentId, CancellationToken cancellationToken = default);
}
