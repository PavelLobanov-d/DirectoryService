using CSharpFunctionalExtensions;
using DirectoryService.Core.Database;
using DirectoryService.Core.DepartmentChiefPositions;
using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace DirectoryService.Infrastructure.PostgreSQL.Repositories;

internal class DepartmentsChiefPositionRepository : IDepartmentsChiefPositionRepository
{
    private readonly IDirectoryServiceDbContext _dbContext;
    private readonly ILogger _logger;
    public DepartmentsChiefPositionRepository(IDirectoryServiceDbContext dbContext, ILogger<DepartmentsChiefPositionRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Guid, Error>> AddAsync(DepartmentChiefPosition obj, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.DepartmentChiefPositions.AddAsync(obj, cancellationToken).ConfigureAwait(false);
        return result.Entity.DepartmentId.Value;
    }
    public async Task<Result<bool, Error>> DeleteAsync(DepartmentChiefPosition departmentChiefPosition, CancellationToken cancellationToken = default)
    {
        _dbContext.DepartmentChiefPositions.Remove(departmentChiefPosition);
        return true;
    }

    public async Task<Result<DepartmentChiefPosition?, Error>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        Result<List<DepartmentChiefPosition>?, Error> result = await GetByAsync(dcp => dcp.DepartmentId == new DepartmentId(departmentId),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error;

        return result.Value?.FirstOrDefault();
    }

    public async Task<Result<List<DepartmentChiefPosition>?, Error>> GetByAsync(Expression<Func<DepartmentChiefPosition, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbContext.DepartmentChiefPositions
            .Where(predicate)
            .Include(v => v.PositionMatrix)
            .Include(v => v.Department)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default)
    {
        int result = await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        return result > 0;
    }
}
