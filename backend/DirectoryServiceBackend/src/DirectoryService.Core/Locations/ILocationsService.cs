using DirectoryService.Contracts;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using CSharpFunctionalExtensions;

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Core.Locations;

public interface ILocationsService
{
    /// <summary>
    /// создать новую локацию
    /// </summary>
    /// <param name="locationDto"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<Guid, Errors>> CreateAsync(CreateLocationDto locationDto, CancellationToken cancellationToken);
    /// <summary>
    /// удалить локацию
    /// </summary>
    /// <param name="locationId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<bool, Error>> DeleteAsync(Guid locationId, CancellationToken cancellationToken);
    /// <summary>
    /// получить локацию по Id
    /// </summary>
    /// <param name="locationId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<Location, Error>> GetByIdAsync(Guid locationId, CancellationToken cancellationToken);
    /// <summary>
    /// получить коллекцию локаций
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<List<Location>, Error>> GetAsync(SelectDto request, CancellationToken cancellationToken);
    /// <summary>
    /// изменить локацию
    /// </summary>
    /// <param name="locationDto"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<bool, Errors>> UpdateAsync(Guid locationId, UpdateLocationDto locationDto, CancellationToken cancellationToken);
    /// <summary>
    /// сохранить изменения в контексте
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken);
}
