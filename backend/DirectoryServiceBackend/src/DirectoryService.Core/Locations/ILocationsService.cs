using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Domain.Locations;
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
    public Task<Guid> CreateAsync(CreateLocationDto locationDto, CancellationToken cancellationToken);
    /// <summary>
    /// сохранить локацию
    /// </summary>
    /// <param name="location"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<bool> SaveAsync(Location location, CancellationToken cancellationToken);
    /// <summary>
    /// удалить локацию
    /// </summary>
    /// <param name="locationId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<bool> DeleteAsync(Guid locationId, CancellationToken cancellationToken);
    /// <summary>
    /// получить локацию по Id
    /// </summary>
    /// <param name="locationId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<Location> GetByIdAsync(Guid locationId, CancellationToken cancellationToken);
    /// <summary>
    /// получить коллекцию локаций
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<Location>> GetAsync(GetLocationsDto request, CancellationToken cancellationToken);
    /// <summary>
    /// изменить локацию
    /// </summary>
    /// <param name="locationDto"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<bool> UpdateAsync(UpdateLocationDto locationDto, CancellationToken cancellationToken);
}
