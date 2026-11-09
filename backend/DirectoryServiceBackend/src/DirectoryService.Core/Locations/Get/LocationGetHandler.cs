using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Domain.Locations;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Locations.Get;

public class LocationGetHandler : ICommandHandler<List<Location>, LocationGetCommand>
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly ILogger<LocationGetHandler> _logger;
    public LocationGetHandler(
        ILocationsRepository locationsRepository,
        ILogger<LocationGetHandler> logger)
    {
        _locationsRepository = locationsRepository;
        _logger = logger;
    }
    public async Task<Result<List<Location>, Errors>> ExecuteAsync(LocationGetCommand command, CancellationToken cancellationToken)
    {
        var resultLocations = await _locationsRepository.GetAsync(command.Request, cancellationToken).ConfigureAwait(false);
        if (resultLocations.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса поиска локаций").ToErrors();
        }
        return resultLocations.Value;
    }

}
