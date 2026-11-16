using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Locations.Queries.GetTop;

public class GetLocationsTopHandler : ICommandHandler<List<GetLocationsByDepartmentsCount>, GetLocationsTopCommand>
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
        var rows = await (
                from l in _readDbContext.LocationsRead
                join dl in _readDbContext.DepartmentLocationsRead on l.Id equals dl.LocationId
                group dl by new { l.Id, l.Name, l.Address }
                into g
                orderby g.Count() descending, g.Key.Id
                select new
                {
                    g.Key.Id,
                    g.Key.Name,
                    g.Key.Address,
                    DepartmentCount = g.Count(),
                })
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
