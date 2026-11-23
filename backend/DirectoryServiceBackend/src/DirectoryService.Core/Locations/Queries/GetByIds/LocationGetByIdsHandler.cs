using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Database;
using DirectoryService.Domain.Locations;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace DirectoryService.Core.Locations.Queries.GetByIds;

public class LocationGetByIdsHandler : ICommandHandler<List<GetLocationResponse>, LocationGetByIdsCommand>
{
    private readonly IReadDbContext _readDbContext;
    private readonly ILogger<LocationGetByIdsHandler> _logger;
    public LocationGetByIdsHandler(
        IReadDbContext readDbContext,
        ILogger<LocationGetByIdsHandler> logger)
    {
        _readDbContext = readDbContext;
        _logger = logger;
    }
    public async Task<Result<List<GetLocationResponse>, Errors>> ExecuteAsync(LocationGetByIdsCommand command, CancellationToken cancellationToken)
    {
        IEnumerable<LocationId> locationsId = command.LocationIds.Select(v => new LocationId(v));

        var rows = await _readDbContext.LocationsRead
            .Where(l => locationsId.Any(v => v == l.Id))
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.Address,
                DepartmentIds = l.DepartmentLocations.Select(dl => dl.DepartmentId).ToList(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
        .Select(row => new GetLocationResponse(
            row.Id.Value,
            row.Name.Value,
            row.Address.Value,
            row.DepartmentIds.Select(d => d.Value).ToList()))
        .OrderBy(l => l.Id)
        .ToList();
    }
}
