using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Locations;

namespace DirectoryService.Core.Locations;

public interface ILocationsRepository
{
    /// <summary>
    /// создать локацию
    /// </summary>
    /// <param name="location">объект Location</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task <Guid> AddAsync(Location location, CancellationToken cancellationToken);
    /// <summary>
    /// сохранить локацию
    /// </summary>
    /// <param name="location">объект Location</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> SaveAsync(Location location, CancellationToken cancellationToken);
    /// <summary>
    /// удалить локацию
    /// </summary>
    /// <param name="locationId">Id</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<bool> DeleteAsync(Guid locationId, CancellationToken cancellationToken);
    /// <summary>
    /// получить локацию
    /// </summary>
    /// <param name="locationId">Id</param>
    /// <param name="cancellationToken"></param>
    /// <returns>объект Location</returns>
    Task<Location> GetByIdAsync(Guid locationId, CancellationToken cancellationToken);
    /// <summary>
    /// получить коллекцию локаций, отвечающую условию
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<List<Location>> GetAsync(GetLocationsDto request, CancellationToken cancellationToken);
    /// <summary>
    /// есто ли локация с именем
    /// </summary>
    /// <param name="name">проверяемое имя</param>
    /// <param name="excludeId">исключая локацию с этим Id</param>
    /// <param name="cancellationToken"></param>
    /// <returns>true, если существует</returns>
    Task<bool> HasNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken);
}
