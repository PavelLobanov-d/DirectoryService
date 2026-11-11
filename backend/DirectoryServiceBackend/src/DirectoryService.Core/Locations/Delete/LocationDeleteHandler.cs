using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;
using System.Transactions;


namespace DirectoryService.Core.Locations.Delete;

public class LocationDeleteHandler : ICommandHandler<bool, LocationDeleteCommand>
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<LocationDeleteHandler> _logger;
    private readonly IStatisticsService _stats;

    public LocationDeleteHandler(
        ILocationsRepository locationsRepository,
        ITransactionManager transactionManager,
        ILogger<LocationDeleteHandler> logger,
        IStatisticsService stats)
    {
        _locationsRepository = locationsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
        _stats = stats;
    }
    public async Task<Result<bool, Errors>> ExecuteAsync(LocationDeleteCommand command, CancellationToken cancellationToken)
    {
        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        var result = await _locationsRepository.DeleteAsync(command.LocationId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            _logger.LogError("Error deleting record of Location");
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return result.Error.ToErrors();
        }
        if (result.Value)
            await _stats.CreateAsync(
                command.LocationId,
                typeof(Location).Name,
                Statistica.Level.INFO,
                Statistica.Action.DELETE,
                $"Удаление",
                cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("Deleting of location {1}", command.LocationId);

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
}
