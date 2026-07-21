using CSharpFunctionalExtensions;
using DirectoryService.Core.Database;
using DirectoryService.Core.DepartmentLocations;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;


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

    public async Task<Result<Guid, Error>> AddAsync(DepartmentLocation obj, CancellationToken cancellationToken)
    {
        var result = await _dbContext.DepartmentLocations.AddAsync(obj, cancellationToken).ConfigureAwait(false);
        return result.Entity.DepartmentId.Value;
    }

    public async Task<Result<DepartmentLocation?, Error>> GetByIdAsync(Guid departmentLocationId, CancellationToken cancellationToken)
    {
        return await _dbContext.DepartmentLocations
            .Where(dl => dl.Id == new DepartmentLocationId(departmentLocationId))
            .Include(v => v.Location)
            .Include(v => v.Department)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
    public async Task<Result<List<DepartmentLocation>, Error>> GetLocationsByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        return await _dbContext.DepartmentLocations
            .Where(dp => dp.DepartmentId == new DepartmentId(departmentId))
            .Include(v => v.Location)
            .Include(v => v.Department)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<bool, Error>> DeleteAsync(Guid departmentLocationId, CancellationToken cancellationToken = default)
    {
        DepartmentLocation? obj = await _dbContext.DepartmentLocations
            .Where(l => l.Id == new DepartmentLocationId(departmentLocationId))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
        if (obj != null)
        {
            var result = _dbContext.DepartmentLocations.Remove(obj);
            return result != null;
        }
        return false;
    }
    public async Task<Result<bool, Error>> DeleteAsync(DepartmentLocation departmentLocation, CancellationToken cancellationToken = default)
    {
        var result = _dbContext.DepartmentLocations.Remove(departmentLocation);
        return result != null;
    }

    public async Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken)
    {
        int result = await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        return result > 0;
    }
}
