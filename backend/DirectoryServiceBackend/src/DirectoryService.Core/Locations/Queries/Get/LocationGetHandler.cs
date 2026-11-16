using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Domain.Locations;
using DirectoryService.Shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace DirectoryService.Core.Locations.Queries.Get;

public class LocationGetHandler : ICommandHandler<List<GetLocationResponse>, LocationGetCommand>
{
    private readonly IReadDbContext _readDbContext;
    private readonly ILogger<LocationGetHandler> _logger;

    public LocationGetHandler(
        IReadDbContext readDbContext,
        ILogger<LocationGetHandler> logger)
    {
        _readDbContext = readDbContext;
        _logger = logger;
    }

    public async Task<Result<List<GetLocationResponse>, Errors>> ExecuteAsync(
        LocationGetCommand command,
        CancellationToken cancellationToken)
    {
        Dictionary<string, StringValues> parsedQuery = QueryHelpers.ParseQuery(command.Request.Search);
        IQueryable<Location> query = _readDbContext.LocationsRead;

        foreach (KeyValuePair<string, StringValues> keySearch in parsedQuery)
        {
            if (keySearch.Key.Equals(nameof(Location.Name), StringComparison.OrdinalIgnoreCase))
            {
                var resultName = LocationName.Create(keySearch.Value.ToString());
                if (resultName.IsFailure)
                    return resultName.Error.ToErrors();

                query = query.Where(l => l.Name == resultName.Value);
            }
            else if (keySearch.Key.Equals(nameof(Location.Address), StringComparison.OrdinalIgnoreCase))
            {
                var resultAddress = Address.Create(keySearch.Value.ToString());
                if (resultAddress.IsFailure)
                    return resultAddress.Error.ToErrors();

                query = query.Where(l => l.Address == resultAddress.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(command.Request.OrderBy))
        {
            string[] param = command.Request.OrderBy.Split(' ');
            string field = param[0];
            string orderType = param.Length > 1 ? param[1] : string.Empty;

            bool descending = orderType.StartsWith("desc", StringComparison.OrdinalIgnoreCase);
            switch (field.ToLowerInvariant())
            {
                case "name":
                    query = descending
                        ? query.OrderByDescending(l => l.Name)
                        : query.OrderBy(l => l.Name);
                    break;
                case "address":
                    query = descending
                        ? query.OrderByDescending(l => l.Address)
                        : query.OrderBy(l => l.Address);
                    break;
                default:
                    query = query.OrderBy(l => l.Id);
                    break;
            }
        }
        else
        {
            query = query.OrderBy(l => l.Id);
        }

        if (command.Request.Page != null && command.Request.PageSize != null)
        {
            int skipRecords = ((int)command.Request.Page - 1) * (int)command.Request.PageSize;
            query = query
                .Skip(skipRecords)
                .Take((int)command.Request.PageSize);
        }

        var rows = await query
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
            .ToList();
    }
}
