using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace DirectoryService.Core.Locations.Update;

public class LocationUpdateHandler : ICommandHandler<bool, LocationUpdateCommand>
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly UpdateLocationValidator _updateValidator;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<LocationUpdateHandler> _logger;
    private readonly IStatisticsService _stats;
    public LocationUpdateHandler(
        ILocationsRepository locationsRepository,
        UpdateLocationValidator updateValidator,
        ITransactionManager transactionManager,
        ILogger<LocationUpdateHandler> logger,
        IStatisticsService stats)
    {
        _locationsRepository = locationsRepository;
        _updateValidator = updateValidator;
        _transactionManager = transactionManager;
        _logger = logger;
        _stats = stats;
    }
    public async Task<Result<bool, Errors>> ExecuteAsync(LocationUpdateCommand command, CancellationToken cancellationToken)
    {
        //валидация входящих параметров
        var validationResult = await _updateValidator.ValidateAsync(command.LocationDto, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            return new Errors(validationResult);
        }

        //валидация бизнес-правил
        if (command.LocationDto.NewName != null)
        {
            var resultIsDuplicate = await _locationsRepository.HasNameAsync(command.LocationDto.NewName, command.LocationId, cancellationToken).ConfigureAwait(false);
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
        else if (command.LocationDto.NewAddress == null)
            return GeneralErrors.ValueIsRequired("NewName or NewAddress").ToErrors();

        var resultLocation = await _locationsRepository.GetByIdAsync(command.LocationId, cancellationToken).ConfigureAwait(false);
        if (resultLocation.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса поиска локации").ToErrors();
        }
        if (resultLocation.Value != null)
        {
            Location location = resultLocation.Value;

            LocationName? name = null;
            if (command.LocationDto.NewName != null)
            {
                var resultName = LocationName.Create(command.LocationDto.NewName);
                if (resultName.IsSuccess)
                    name = resultName.Value;
            }

            Address? address = null;
            if (command.LocationDto.NewAddress != null)
            {
                var resultAddress = Address.Create(command.LocationDto.NewAddress);
                if (resultAddress.IsSuccess)
                    address = resultAddress.Value;
            }

            var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
            if (transactionScopeResult.IsFailure)
                return transactionScopeResult.Error.ToErrors();

            await using var transactionScope = transactionScopeResult.Value;

            if (!location.Update(name, address))
            {
                _logger.LogInformation("обновление не требуется");
                return false;
            }

            var locationUpdate = await _locationsRepository.UpdateAsync(location, cancellationToken).ConfigureAwait(false);
            if (locationUpdate.IsFailure)
            {
                _logger.LogError("Error updating record of Location");
                var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
                if (rollbackResult.IsFailure)
                    return rollbackResult.Error.ToErrors();

                return locationUpdate.Error.ToErrors();
            }

            if (command.LocationDto.NewName != null)
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
            if (command.LocationDto.NewAddress != null)
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

            var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            if (saveResult.IsFailure)
            {
                var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
                if (rollbackResult.IsFailure)
                    return rollbackResult.Error.ToErrors();

                return saveResult.Error.ToErrors();
            }

            var commitResult = await transactionScope.CommitAsync(cancellationToken).ConfigureAwait(false);
            if (commitResult.IsFailure)
                return commitResult.Error.ToErrors();

            return true;
        }
        return GeneralErrors.NotFound(command.LocationId).ToErrors();
    }
}
