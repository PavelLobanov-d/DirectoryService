using DirectoryService.Contracts;
using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using CSharpFunctionalExtensions;


namespace DirectoryService.Core.Locations;

public interface ILocationsRepository
{
    /// <summary>
    /// создать локацию
    /// </summary>
    /// <param name="location">объект Location</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task <Result<Guid, Error>> AddAsync(Location location, CancellationToken cancellationToken = default);
    /// <summary>
    /// сохранить изменения в записи локации
    /// </summary>
    /// <param name="location">объект Location</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default);
    /// <summary>
    /// удалить локацию
    /// </summary>
    /// <param name="locationId">Id</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Result<bool, Error>> DeleteAsync(Guid locationId, CancellationToken cancellationToken = default);
    /// <summary>
    /// удалить локацию
    /// </summary>
    /// <param name="location">объект Location</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Result<bool, Error>> DeleteAsync(Location location, CancellationToken cancellationToken = default);
    /// <summary>
    /// получить локацию
    /// </summary>
    /// <param name="locationId">Id</param>
    /// <param name="cancellationToken"></param>
    /// <returns>объект Location</returns>
    Task<Result<Location?, Error>> GetByIdAsync(Guid locationId, CancellationToken cancellationToken = default);
    Task<Result<List<Location>, Error>> GetByIdsAsync(IEnumerable<Guid> locationIds, CancellationToken cancellationToken = default);
    /// <summary>
    /// получить коллекцию локаций, отвечающую условию
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task<Result<List<Location>, Error>> GetAsync(SelectDto request, CancellationToken cancellationToken = default);
    /// <summary>
    /// есть ли локация с именем
    /// </summary>
    /// <param name="name">проверяемое имя</param>
    /// <param name="excludeId">исключая локацию с этим Id</param>
    /// <param name="cancellationToken"></param>
    /// <returns>true, если существует</returns>
    Task<Result<bool, Error>> HasNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default);
    /// <summary>
    /// изменение локации
    /// </summary>
    /// <param name="location">объект изменённой локации</param>
    /// <param name="cancellationToken"></param>
    /// <returns>true, если изменение успешно</returns>
    Task<Result<bool, Error>> UpdateAsync(Location location, CancellationToken cancellationToken = default);
}
