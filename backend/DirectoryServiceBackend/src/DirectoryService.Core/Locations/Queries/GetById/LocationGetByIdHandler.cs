using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Domain.Locations;
using DirectoryService.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Locations.Queries.GetById;

public class LocationGetByIdHandler : ICommandHandler<GetLocationResponse, LocationGetByIdCommand>
{
    private readonly IReadDbContext _readDbContext;
    private readonly ILogger<LocationGetByIdHandler> _logger;

    public LocationGetByIdHandler(
        IReadDbContext readDbContext,
        ILogger<LocationGetByIdHandler> logger)
    {
        _readDbContext = readDbContext;
        _logger = logger;
    }

    public async Task<Result<GetLocationResponse, Errors>> ExecuteAsync(
        LocationGetByIdCommand command,
        CancellationToken cancellationToken)
    {
        var locationId = new LocationId(command.LocationId);

        var row = await _readDbContext.LocationsRead
            .Where(l => l.Id == locationId)
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.Address,
                DepartmentIds = l.DepartmentLocations.Select(dl => dl.DepartmentId).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            _logger.LogInformation("Location {LocationId} not found", command.LocationId);
            return GeneralErrors.NotFound(command.LocationId, "Location").ToErrors();
        }

        return new GetLocationResponse(
            row.Id.Value,
            row.Name.Value,
            row.Address.Value,
            row.DepartmentIds.Select(d => d.Value).ToList());
    }
}
