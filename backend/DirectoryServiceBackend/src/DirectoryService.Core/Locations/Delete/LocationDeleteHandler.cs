using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.DepartmentLocations;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Locations.Delete;

public class LocationDeleteHandler : ICommandHandler<bool, LocationDeleteCommand>
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly IDepartmentLocationsRepository _departmentLocationsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<LocationDeleteHandler> _logger;
    private readonly IStatisticsService _stats;

    public LocationDeleteHandler(
        ILocationsRepository locationsRepository,
        IDepartmentLocationsRepository departmentLocationsRepository,
        ITransactionManager transactionManager,
        ILogger<LocationDeleteHandler> logger,
        IStatisticsService stats)
    {
        _locationsRepository = locationsRepository;
        _departmentLocationsRepository = departmentLocationsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
        _stats = stats;
    }

    public async Task<Result<bool, Errors>> ExecuteAsync(
        LocationDeleteCommand command,
        CancellationToken cancellationToken)
    {
        var resultLocation = await _locationsRepository.GetByIdAsync(command.LocationId, cancellationToken).ConfigureAwait(false);
        if (resultLocation.IsFailure)
        {
            _logger.LogError("Error _locationsRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска локации").ToErrors();
        }
        if (resultLocation.Value == null)
        {
            _logger.LogError("Record of Location not found {LocationId}", command.LocationId);
            return GeneralErrors.NotFound(command.LocationId, "Location").ToErrors();
        }
        Location location = resultLocation.Value;

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        // При удалении локации связи с подразделениями удаляются в той же транзакции.
        var resultLinks = await _departmentLocationsRepository
            .GetByAsync(dl => dl.LocationId == location.Id, cancellationToken)
            .ConfigureAwait(false);
        if (resultLinks.IsFailure)
        {
            _logger.LogError("Error _departmentLocationsRepository.GetByAsync");
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return GeneralErrors.Failure("ошибка запроса поиска связей локации").ToErrors();
        }

        foreach (DepartmentLocation link in resultLinks.Value ?? [])
        {
            var resultLinkDelete = await _departmentLocationsRepository.DeleteAsync(link, cancellationToken).ConfigureAwait(false);
            if (resultLinkDelete.IsFailure)
            {
                _logger.LogError("Error _departmentLocationsRepository.DeleteAsync");
                var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
                if (rollbackResult.IsFailure)
                    return rollbackResult.Error.ToErrors();

                return GeneralErrors.Failure("ошибка удаления связи подразделение-локация").ToErrors();
            }
        }

        var result = await _locationsRepository.DeleteAsync(location, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            _logger.LogError("Error deleting record of Location");
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return result.Error.ToErrors();
        }

        await _stats.CreateAsync(
            command.LocationId,
            typeof(Location).Name,
            Statistica.Level.INFO,
            Statistica.Action.DELETE,
            "Удаление",
            cancellationToken).ConfigureAwait(false);

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

        _logger.LogInformation("Deleting of location {LocationId}", command.LocationId);
        return result.Value;
    }
}
