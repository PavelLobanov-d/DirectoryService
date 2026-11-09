using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Domain.Locations;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Locations.GetByIds;

public class LocationGetByIdsHandler : ICommandHandler<List<Location>, LocationGetByIdsCommand>
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly ILogger<LocationGetByIdsHandler> _logger;
    public LocationGetByIdsHandler(
        ILocationsRepository locationsRepository,
        ILogger<LocationGetByIdsHandler> logger)
    {
        _locationsRepository = locationsRepository;
        _logger = logger;
    }
    public async Task<Result<List<Location>, Errors>> ExecuteAsync(LocationGetByIdsCommand command, CancellationToken cancellationToken)
    {
        var resultLocation = await _locationsRepository.GetByIdsAsync(command.LocationIds, cancellationToken).ConfigureAwait(false);
        if (resultLocation.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса поиска локаций").ToErrors();
        }
        if (resultLocation.Value.Count < command.LocationIds.ToList().Count)
        {
            return GeneralErrors.ValueIsInvalid("не найдены локации").ToErrors();
        }

        return resultLocation.Value;
    }
}
