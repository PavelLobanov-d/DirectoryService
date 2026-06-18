using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.GlobalStatisticsClass;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;


namespace DirectoryService.Core.Locations;

public class LocationsService: ILocationsService
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly CreateLocationValidator _createValidator;
    private readonly UpdateLocationValidator _updateValidator;
    private readonly ILogger<LocationsService> _logger;
    private readonly IStatisticsService _stats;
    public LocationsService(
        ILocationsRepository locationsRepository,
        CreateLocationValidator createValidator,
        UpdateLocationValidator updateValidator,
        IStatisticsService stats,
        ILogger<LocationsService> logger)
    {
        _locationsRepository = locationsRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _stats = stats;
        _logger = logger;
    }
    /// <summary>
    /// создать новую локацию
    /// </summary>
    /// <param name="locationDto"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>Guid новой локации</returns>
    public async Task<Result<Guid, Errors>> CreateAsync(CreateLocationDto locationDto, CancellationToken cancellationToken)
    {
        //валидация входящих параметров
        ValidationResult validationResult = await _createValidator.ValidateAsync(locationDto, cancellationToken).ConfigureAwait(false);
        if(!validationResult.IsValid)
        {
            return new Errors(validationResult);
        }
        //валидация бизнес-правил
        var resultIsDuplicate = await _locationsRepository.HasNameAsync(locationDto.Name, excludeId: null, cancellationToken).ConfigureAwait(false);
        if(resultIsDuplicate.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса").ToErrors();
        }
        if(resultIsDuplicate.Value)
        {
            return GeneralErrors.AlreadyExist().ToErrors();
        }

        var resultLocationName = LocationName.Create(locationDto.Name);
        if (resultLocationName.IsFailure)
        {
            return GeneralErrors.ValueIsInvalid("название локации").ToErrors();
        }
        var resultLocationAddress = Address.Create(locationDto.Address);
        if(resultLocationAddress.IsFailure)
        {
            return GeneralErrors.ValueIsInvalid("адрес локации").ToErrors();
        }

        var location = Location.Create(resultLocationName.Value, resultLocationAddress.Value);
        var resultAdd = await _locationsRepository.AddAsync(location, cancellationToken).ConfigureAwait(false);
        if (resultAdd.IsFailure)
        {
            _logger.LogError("Error creating record of Location");
            return GeneralErrors.Failure("ошибка добавления локации").ToErrors();
        }

        await _stats.CreateAsync(
            location.Id.Value,
            location.GetType().Name,
            Statistica.Level.INFO,
            Statistica.Action.CREATE,
            $"Создание локации {location.Name}",
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Location created with Id {1}", location.Id.Value);

        return location.Id.Value;
    }
    /// <summary>
    /// удалить локацию
    /// </summary>
    /// <param name="locationId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>true, если удаление успешно</returns>
    public async Task<Result<bool, Error>> DeleteAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var result = await _locationsRepository.DeleteAsync(locationId, cancellationToken).ConfigureAwait(false);
        if(result.IsFailure)
        {
            _logger.LogError("Error deleting record of Location");
            return GeneralErrors.Failure("ошибка удаления локации");
        }

        await _stats.CreateAsync(
            locationId,
            typeof(Location).Name,
            Statistica.Level.INFO,
            Statistica.Action.DELETE,
            $"Удаление",
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleting of location {1}", locationId);
        return result;
    }
    /// <summary>
    /// получить локацию по id
    /// </summary>
    /// <param name="locationId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>объект локации</returns>
    public async Task<Result<Location, Error>> GetByIdAsync(Guid locationId, CancellationToken cancellationToken)
    {
        var resultLocation = await _locationsRepository.GetByIdAsync(locationId, cancellationToken).ConfigureAwait(false);
        if(resultLocation.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса поиска локации");
        }
        return resultLocation.Value;
    }
    /// <summary>
    /// получить коллекцию локаций по условию
    /// </summary>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<List<Location>, Error>> GetAsync(GetLocationsDto request, CancellationToken cancellationToken)
    {
        var resultLocations = await _locationsRepository.GetAsync(request, cancellationToken).ConfigureAwait(false);
        if (resultLocations.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса поиска локаций");
        }
        return resultLocations.Value;
    }

    /// <summary>
    /// обновление локации
    /// </summary>
    /// <param name="locationDto"></param>
    /// <param name="cancellationToken"></param>
    /// <returns>true, если обновление успешно</returns>
    public async Task<Result<bool, Errors>> UpdateAsync(UpdateLocationDto locationDto, CancellationToken cancellationToken)
    {
        //валидация входящих параметров
        var validationResult = await _updateValidator.ValidateAsync(locationDto, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            return new Errors(validationResult);
        }

        //валидация бизнес-правил
        if (locationDto.NewName != null)
        {
            var resultIsDuplicate = await _locationsRepository.HasNameAsync(locationDto.NewName, locationDto.locationId, cancellationToken).ConfigureAwait(false);
            if (resultIsDuplicate.IsFailure)
            {
                _logger.LogError("Request error");
                return GeneralErrors.Failure("ошибка запроса поиска дубликатов").ToErrors();
            }
            if (resultIsDuplicate.Value)
            {
                return GeneralErrors.AlreadyExist().ToErrors();
            }
        }
        else if (locationDto.NewAddress == null)
            return false;

        var resultLocation = await _locationsRepository.GetByIdAsync(locationDto.locationId, cancellationToken).ConfigureAwait(false);
        if (resultLocation.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса поиска локации").ToErrors();
        }
        if (resultLocation.Value != null)
        {
            Location location = resultLocation.Value;

            LocationName? name = null;
            if (locationDto.NewName != null)
            {
                var resultName = LocationName.Create(locationDto.NewName);
                if (resultName.IsSuccess)
                    name = resultName.Value;
            }
                
            Address? address = null;
            if (locationDto.NewAddress != null)
            {
                var resultAddress = Address.Create(locationDto.NewAddress);
                if(resultAddress.IsSuccess)
                    address = resultAddress.Value;
            }

            if (location.Update(name, address))
            {
                var locationUpdate = await _locationsRepository.SaveAsync(location, cancellationToken).ConfigureAwait(false);
                if (locationUpdate.IsFailure)
                {
                    _logger.LogError("Error updating record of Location");
                    return locationUpdate.Error.ToErrors();
                }

                if (locationDto.NewName != null)
                {
                    await _stats.CreateAsync(
                        location.Id.Value,
                        typeof(Location).Name,
                        Statistica.Level.INFO,
                        Statistica.Action.UPDATE,
                        $"Изменение имени на {location.Name}",
                        cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("Change name of location {1} : {2}", location.Id.Value, location.Name.Value);
                }
                if (locationDto.NewAddress != null)
                {
                    await _stats.CreateAsync(
                        location.Id.Value,
                        typeof(Location).Name,
                        Statistica.Level.INFO,
                        Statistica.Action.UPDATE,
                        $"Изменение адреса на {location.Address}",
                        cancellationToken).ConfigureAwait(false);

                    _logger.LogInformation("Change address of location {1} : {2}", location.Id.Value, location.Address.Value);
                }
                return true;
            }
        }
        return GeneralErrors.NotFound(locationDto.locationId).ToErrors();
    }
}
