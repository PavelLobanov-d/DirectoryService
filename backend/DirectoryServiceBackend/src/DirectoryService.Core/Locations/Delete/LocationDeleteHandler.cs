using CSharpFunctionalExtensions;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.Locations;
using DirectoryService.Shared;
using DirectoryService.Domain.Statistics;
using Microsoft.Extensions.Logging;
using DirectoryService.Core.Abstractions;


namespace DirectoryService.Core.Locations.Delete;

public class LocationDeleteHandler : ICommandHandler<bool, LocationDeleteCommand>
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly ILocationsService _locationsService;
    private readonly ILogger<LocationDeleteHandler> _logger;
    private readonly IStatisticsService _stats;

    public LocationDeleteHandler(
        ILocationsRepository locationsRepository,
        ILocationsService locationsService,
        ILogger<LocationDeleteHandler> logger,
        IStatisticsService stats)
    {
        _locationsRepository = locationsRepository;
        _locationsService = locationsService;
        _logger = logger;
        _stats = stats;
    }
    public async Task<Result<bool, Errors>> ExecuteAsync(LocationDeleteCommand command, CancellationToken cancellationToken)
    {
        var result = await _locationsRepository.DeleteAsync(command.LocationId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            _logger.LogError("Error deleting record of Location");
            return GeneralErrors.Failure("ошибка удаления локации").ToErrors();
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

        var resultSave = await _locationsService.SaveAsync(cancellationToken).ConfigureAwait(false);
        if (resultSave.IsFailure)
            return resultSave.Error.ToErrors();

        return true;
    }
}
