using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Locations.Queries.GetTop;

public class GetLocationsTopHandler : ICommandHandler<List<GetLocationsTop>, GetLocationsTopCommand>
{
    private const int TOP_SIZE = 5;

    private readonly IReadDbContext _readDbContext;
    private readonly ILogger<GetLocationsTopHandler> _logger;

    public GetLocationsTopHandler(
        IReadDbContext readDbContext,
        ILogger<GetLocationsTopHandler> logger)
    {
        _readDbContext = readDbContext;
        _logger = logger;
    }

    public async Task<Result<List<GetLocationsByDepartmentsCount>, Errors>> ExecuteAsync(
        GetLocationsTopCommand command,
        CancellationToken cancellationToken = default)
    {
        var rows = await _readDbContext.LocationsRead
            .Where(l => l.DepartmentLocations.Any())
            .Select(l => new
            {
                l.Id,
                l.Name,
                l.Address,
                DepartmentCount = l.DepartmentLocations.Count,
            })
            .OrderByDescending(l => l.DepartmentCount)
            .ThenBy(l => l.Id)
            .Take(TOP_SIZE)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(row => new GetLocationsByDepartmentsCount(
                row.Id.Value,
                row.Name.Value,
                row.Address.Value,
                row.DepartmentCount))
            .ToList();
    }
}
