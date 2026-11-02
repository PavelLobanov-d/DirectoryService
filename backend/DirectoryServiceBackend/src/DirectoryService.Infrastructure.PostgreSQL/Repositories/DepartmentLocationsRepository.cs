using CSharpFunctionalExtensions;
using DirectoryService.Core.Database;
using DirectoryService.Core.DepartmentLocations;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Departments;
using DirectoryService.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;


namespace DirectoryService.Infrastructure.PostgreSQL.Repositories;

internal class DepartmentLocationsRepository : IDepartmentLocationsRepository
{
    private readonly IDirectoryServiceDbContext _dbContext;
    private readonly ILogger _logger;
    public DepartmentLocationsRepository(IDirectoryServiceDbContext dbContext, ILogger<DepartmentLocationsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Guid, Error>> AddAsync(DepartmentLocation obj, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.DepartmentLocations.AddAsync(obj, cancellationToken).ConfigureAwait(false);
        return result.Entity.DepartmentId.Value;
    }

    public async Task<Result<DepartmentLocation?, Error>> GetByIdAsync(Guid departmentLocationId, CancellationToken cancellationToken = default)
    {
        Result<List<DepartmentLocation>?, Error> result = await GetByAsync(d => d.Id == new DepartmentLocationId(departmentLocationId), cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error;

        return result.Value?.FirstOrDefault();
    }

    public async Task<Result<List<DepartmentLocation>?, Error>> GetByAsync(Expression<Func<DepartmentLocation, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DepartmentLocations
            .Where(predicate)
            .Include(v => v.Location)
            .Include(v => v.Department)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<List<DepartmentLocation>?, Error>> GetLocationsByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        Result<List<DepartmentLocation>?, Error> result = await GetByAsync(dl => dl.DepartmentId == new DepartmentId(departmentId), cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error;

        return result;
    }

    public async Task<Result<bool, Error>> DeleteAsync(Guid departmentLocationId, CancellationToken cancellationToken = default)
    {
        Result<DepartmentLocation?, Error> result = await GetByIdAsync(departmentLocationId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error;

        DepartmentLocation? obj = result.Value;
        if (obj != null)
        {
            _dbContext.DepartmentLocations.Remove(obj);
            return true;
        }
        return false;
    }
    public async Task<Result<bool, Error>> DeleteAsync(DepartmentLocation departmentLocation, CancellationToken cancellationToken = default)
    {
        _dbContext.DepartmentLocations.Remove(departmentLocation);
        return true;
    }

    public async Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default)
    {
        int result = await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        return result > 0;
    }
}
