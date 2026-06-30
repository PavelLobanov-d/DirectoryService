using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Database;
using DirectoryService.Core.Locations;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace DirectoryService.Infrastructure.PostgreSQL.Repositories;

internal class LocationsRepository : ILocationsRepository
{
    private readonly IDirectoryServiceDbContext _dbContext;
    public LocationsRepository(IDirectoryServiceDbContext dbContext)
    {
        _dbContext = dbContext;
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

        return await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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
