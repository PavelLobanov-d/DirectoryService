using CSharpFunctionalExtensions;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.Locations;
using DirectoryService.Shared;
using DirectoryService.Domain.Statistics;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using DirectoryService.Core.Abstractions;


namespace DirectoryService.Core.Locations.Create;

public class LocationCreateHandler : ICommandHandler<CreatedObjectGuid, LocationCreateCommand>
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly ILocationsService _locationsService;
    private readonly CreateLocationValidator _createValidator;
    private readonly ILogger<LocationCreateHandler> _logger;
    private readonly IStatisticsService _stats;

    public LocationCreateHandler(
        ILocationsRepository locationsRepository,
        ILocationsService locationsService,
        CreateLocationValidator createValidator,
        ILogger<LocationCreateHandler> logger,
        IStatisticsService stats)
    {
        _locationsRepository = locationsRepository;
        _locationsService = locationsService;
        _createValidator = createValidator;
        _logger = logger;
        _stats = stats;
    }
    public async Task<Result<CreatedObjectGuid, Errors>> ExecuteAsync(LocationCreateCommand command, CancellationToken cancellationToken)
    {
        //валидация входящих параметров
        ValidationResult validationResult = await _createValidator.ValidateAsync(command.LocationDto, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            return new Errors(validationResult);
        }
        //валидация бизнес-правил
        var resultIsDuplicate = await _locationsRepository.HasNameAsync(command.LocationDto.Name, excludeId: null, cancellationToken).ConfigureAwait(false);
        if (resultIsDuplicate.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса").ToErrors();
        }
        if (resultIsDuplicate.Value)
        {
            return GeneralErrors.AlreadyExist().ToErrors();
        }

        var resultLocationName = LocationName.Create(command.LocationDto.Name);
        if (resultLocationName.IsFailure)
        {
            return GeneralErrors.ValueIsInvalid("название локации").ToErrors();
        }
        var resultLocationAddress = Address.Create(command.LocationDto.Address);
        if (resultLocationAddress.IsFailure)
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
            resultAdd.Value,
            location.GetType().Name,
            Statistica.Level.INFO,
            Statistica.Action.CREATE,
            $"Создание локации {location.Name}",
            cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Location created with Id {1}", resultAdd.Value);

        var resultSave = await _locationsService.SaveAsync(cancellationToken).ConfigureAwait(false);
        if (resultSave.IsFailure)
            return resultSave.Error.ToErrors();

        return new CreatedObjectGuid(resultAdd.Value);
    }

}
