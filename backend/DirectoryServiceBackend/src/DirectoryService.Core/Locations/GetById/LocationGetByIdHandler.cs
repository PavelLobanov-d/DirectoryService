using CSharpFunctionalExtensions;
using DirectoryService.Domain.Locations;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.Locations.GetById;

public class LocationGetByIdHandler : ICommandHandler<Location?, LocationGetByIdCommand>
{
    private readonly ILocationsRepository _locationsRepository;
    private readonly ILogger<LocationGetByIdHandler> _logger;
    public LocationGetByIdHandler(
        ILocationsRepository locationsRepository,
        ILogger<LocationGetByIdHandler> logger)
    {
        _locationsRepository = locationsRepository;
        _logger = logger;
    }
    public async Task<Result<Location?, Errors>> ExecuteAsync(LocationGetByIdCommand command, CancellationToken cancellationToken)
    {
        var resultLocation = await _locationsRepository.GetByIdAsync(command.LocationId, cancellationToken).ConfigureAwait(false);
        if (resultLocation.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса поиска локации").ToErrors();
        }
        if(resultLocation.Value == null)
            return GeneralErrors.NotFound(command.LocationId).ToErrors();

        return resultLocation.Value;
    }
}
