using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Core.Database;
using DirectoryService.Core.Locations;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace DirectoryService.Infrastructure.PostgreSQL.Repositories;

internal class LocationsRepository : ILocationsRepository
{
    private readonly IDirectoryServiceDbContext _dbContext;
    private readonly ILogger _logger;

    public LocationsRepository(IDirectoryServiceDbContext dbContext, ILogger<LocationsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
    public async Task<Result<Guid, Error>> AddAsync(Location location, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.Locations.AddAsync(location, cancellationToken).ConfigureAwait(false);
        return result.Entity.Id.Value;
    }
    public async Task<Result<bool, Error>> DeleteAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        Location? obj = await _dbContext.Locations
            .Where(l => l.Id == new LocationId(locationId))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (obj != null)
        {
            var result = _dbContext.Locations.Remove(obj);
            return result != null;
        }
        return false;
    }
    public async Task<Result<bool, Error>> DeleteAsync(Location location, CancellationToken cancellationToken = default)
    {
        var result = _dbContext.Locations.Remove(location);
        return result != null;
    }
    public async Task<Result<List<Location>, Error>> GetAsync(SelectDto request, CancellationToken cancellationToken = default)
    {
        Dictionary<string, StringValues> parsedQuery = QueryHelpers.ParseQuery(request.Search);
        IQueryable<Location> query = _dbContext.Locations;

        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> keySearch in parsedQuery)
        {
            switch (keySearch.Key)
            {
                case nameof(Location.Name):
                    var resultName = LocationName.Create(keySearch.Value.ToString());
                    query = query.Where(p => p.Name == resultName.Value);
                    break;
                case nameof(Location.Address):
                    var resultAddress = Address.Create(keySearch.Value.ToString());
                    query = query.Where(p => p.Address == resultAddress.Value);
                    break;
            }
        }

        if (request.OrderBy != null && !request.OrderBy.Equals(string.Empty))
        {
            string[] param = request.OrderBy.Split(' ');
            string field = param[0];
            string orderType = "";
            if (param.Length > 1)
            {
                orderType = param[1];
            }
            if(orderType.ToLowerInvariant().StartsWith("desc", StringComparison.OrdinalIgnoreCase))
            {
                switch (field.ToLowerInvariant())
                {
                    case "name":
                        query = query.OrderByDescending(p => p.Name);
                        break;
                    case "address":
                        query = query.OrderByDescending(p => p.Address);
                        break;
                }
            }
            else if(orderType.ToLowerInvariant().StartsWith("asc", StringComparison.OrdinalIgnoreCase) || orderType == "")
            {
                switch (field.ToLowerInvariant())
                {
                    case "name":
                        query = query.OrderBy(p => p.Name);
                        break;
                    case "address":
                        query = query.OrderBy(p => p.Address);
                        break;
                }
            }
        }
        else
            query = query.OrderBy(p => p.Id);

        if (request.Page != null && request.PageSize != null)
        {
            int skiprecords = ((int)request.Page - 1) * (int)request.PageSize;
            query = query
            .Skip(skiprecords)
            .Take((int)request.PageSize);
        }

        return await query
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }
    public async Task<Result<Location?, Error>> GetByIdAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .Where(l => l.Id == new LocationId(locationId))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
    public async Task<Result<bool, Error>> HasNameAsync(string name, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var resultName = LocationName.Create(name);
        if(resultName.IsFailure)
            return resultName.Error;

        bool result;

        if (excludeId != null)
        {
            LocationId id = new LocationId(excludeId.Value);
            result = await _dbContext.Locations
                .Where(l => l.Name == resultName.Value && l.Id != id)
                .AnyAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        else
            result = await _dbContext.Locations
            .Where(l => l.Name == resultName.Value)
            .AnyAsync(cancellationToken)
            .ConfigureAwait(false);

        return result;
    }
    public async Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default)
    {
        int result = await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        return result > 0;
    }
    public async Task<Result<bool, Error>> UpdateAsync(Location location, CancellationToken cancellationToken = default)
    {
        var result = _dbContext.Locations
            .Update(location);
        return result.Entity != null;
    }
}
