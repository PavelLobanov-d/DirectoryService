using DirectoryService.Contracts.Locations;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Statistics;
using DirectoryService.Domain.shared;

using FluentValidation;
using Microsoft.Extensions.Logging;


namespace DirectoryService.Core.Locations;

public class LocationsService: ILocationsService
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly CreateLocationValidator _createValidator;
    private readonly UpdateLocationValidator _updateValidator;
    private readonly ILogger<LocationsService> _logger;
    private readonly GlobalStatistics _globalstats;
    public LocationsService(
        ILocationsRepository locationsRepository,
        CreateLocationValidator createValidator,
        UpdateLocationValidator updateValidator,
        GlobalStatistics globalstats,
        ILogger<LocationsService> logger)
    {
        _locationsRepository = locationsRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _globalstats = globalstats;
        _logger = logger;
    }
    /// <summary>
    /// создать новую локацию
    /// </summary>
    /// <param name="locationDto"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Guid новой локации</returns>
    /// <exception cref="ValidationException"></exception>
    /// <exception cref="DSException"></exception>
    public async Task<Guid> CreateAsync(CreateLocationDto locationDto, CancellationToken cancellationToken)
    {
        //валидация входящих параметров
        var validationResult = await _createValidator.ValidateAsync(locationDto, cancellationToken).ConfigureAwait(false);
        if(!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        //валидация бизнес-правил
        bool isDuplicate = await _locationsRepository.HasNameAsync(locationDto.Name, excludeId: null, cancellationToken).ConfigureAwait(false);
        if(isDuplicate)
        {
            throw new DSException("Дублирование имени локации");
        }

        var location = Location.Create(new LocationName(locationDto.Name), new Address(locationDto.Address));
        await _locationsRepository.AddAsync(location, cancellationToken).ConfigureAwait(false);

        _globalstats.AddStatistica(
            location.Id.Value,
            location.GetType().Name,
            Statistica.Level.INFO,
            Statistica.Action.CREATE,
            $"Создание локации {location.Name}");

        _logger.LogInformation("Location created with Id {1}", location.Id.Value);

        return location.Id.Value;
    }
    /// <summary>
    /// сохранить локацию
    /// </summary>
    /// <param name="location"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<bool> SaveAsync(Location location, CancellationToken cancellationToken)
    {
        bool result = await _locationsRepository.SaveAsync(location, cancellationToken).ConfigureAwait(false);
        return result;
    }
    /// <summary>
    /// удалить локацию
    /// </summary>
    /// <param name="locationId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>true, если удаление успешно</returns>
    public async Task<bool> DeleteAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var result = await _locationsRepository.DeleteAsync(locationId, cancellationToken).ConfigureAwait(false);
        if(result)
        {
            _globalstats.AddStatistica(
                locationId,
                typeof(Location).Name,
                Statistica.Level.INFO,
                Statistica.Action.DELETE,
                $"Удаление");

            _logger.LogInformation("Deleting of location {1}", locationId);
        }
        return result;
    }
    /// <summary>
    /// получить локацию по id
    /// </summary>
    /// <param name="locationId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>объект локации</returns>
    public async Task<Location> GetByIdAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var location = await _locationsRepository.GetByIdAsync(locationId, cancellationToken).ConfigureAwait(false);
        return location;
    }
    /// <summary>
    /// обновление локации
    /// </summary>
    /// <param name="locationDto"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>true, если обновление успешно</returns>
    /// <exception cref="ValidationException"></exception>
    /// <exception cref="DSException"></exception>
    public async Task<bool> UpdateAsync(UpdateLocationDto locationDto, CancellationToken cancellationToken)
    {
        //валидация входящих параметров
        var validationResult = await _updateValidator.ValidateAsync(locationDto, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }
        //валидация бизнес-правил
        if (locationDto.NewName != null)
        {
            bool isDuplicate = await _locationsRepository.HasNameAsync(locationDto.NewName, locationDto.locationId, cancellationToken).ConfigureAwait(false);
            if (isDuplicate)
            {
                throw new DSException("Дублирование имени локации");
            }
        }
        else if (locationDto.NewAddress == null)
            return false;

        var location = await _locationsRepository.GetByIdAsync(locationDto.locationId, cancellationToken).ConfigureAwait(false);
        if (location != null)
        {
            LocationName? name = null;
            if (locationDto.NewName != null)
                name = new LocationName(locationDto.NewName);
            Address? address = null;
            if (locationDto.NewAddress != null)
                address = new Address(locationDto.NewAddress);

            if (location.Update(name, address)
            && await _locationsRepository.SaveAsync(location, cancellationToken).ConfigureAwait(false))
            {
                if (locationDto.NewName != null)
                {
                    _globalstats.AddStatistica(
                        location.Id.Value,
                        location.GetType().Name,
                        Statistica.Level.INFO,
                        Statistica.Action.UPDATE,
                        $"Изменение имени на {location.Name}");

                    _logger.LogInformation("Change name of location {1} : {2}", location.Id.Value, location.Name.Value);
                }
                if (locationDto.NewAddress != null)
                {
                    _globalstats.AddStatistica(
                        location.Id.Value,
                        location.GetType().Name,
                        Statistica.Level.INFO,
                        Statistica.Action.UPDATE,
                        $"Изменение адреса на {location.Address}");

                    _logger.LogInformation("Change address of location {1} : {2}", location.Id.Value, location.Address.Value);
                }
                return true;
            }
        }
        return false;
    }
}
