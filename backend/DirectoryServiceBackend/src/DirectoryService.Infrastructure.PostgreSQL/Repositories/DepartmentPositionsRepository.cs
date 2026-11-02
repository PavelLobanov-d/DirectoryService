using CSharpFunctionalExtensions;
using DirectoryService.Core.Database;
using DirectoryService.Core.DepartmentPositions;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace DirectoryService.Infrastructure.PostgreSQL.Repositories;

internal class DepartmentPositionsRepository : IDepartmentPositionsRepository
{
    private readonly IDirectoryServiceDbContext _dbContext;
    private readonly ILogger _logger;
    public DepartmentPositionsRepository(IDirectoryServiceDbContext dbContext, ILogger<DepartmentPositionsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Guid, Error>> AddAsync(DepartmentPosition obj, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.DepartmentPositions.AddAsync(obj, cancellationToken).ConfigureAwait(false);
        return result.Entity.DepartmentId.Value;
    }

    public async Task<Result<DepartmentPosition?, Error>> GetByIdAsync(Guid departmentPositionId, CancellationToken cancellationToken = default)
    {
        Result<List<DepartmentPosition>?, Error> result = await GetByAsync(dp => dp.Id == new DepartmentPositionId(departmentPositionId),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error;

        return result.Value?.FirstOrDefault();
    }

    public async Task<Result<List<DepartmentPosition>?, Error>> GetByAsync(Expression<Func<DepartmentPosition, bool>> predicate,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.DepartmentPositions
            .Where(predicate)
            .Include(v => v.PositionMatrix)
            .Include(v => v.Department)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<List<DepartmentPosition>?, Error>> GetPositionsByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        Result<List<DepartmentPosition>?, Error> result = await GetByAsync(dp => dp.DepartmentId == new DepartmentId(departmentId),
            cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error;

        return result;
    }

    public async Task<Result<bool, Error>> DeleteAsync(Guid departmentPositionId, CancellationToken cancellationToken = default)
    {
        Result<DepartmentPosition?, Error> result = await GetByIdAsync(departmentPositionId, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
            return result.Error;

        DepartmentPosition? obj = result.Value;
        if (obj != null)
        {
            _dbContext.DepartmentPositions.Remove(obj);
            return true;
        }
        return false;
    }

    public async Task<Result<bool, Error>> DeleteAsync(DepartmentPosition departmentPosition, CancellationToken cancellationToken = default)
    {
        var result = _dbContext.DepartmentPositions.Remove(departmentPosition);
        return result != null;
    }

    public async Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default)
    {
        int result = await _dbContext
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
        return result > 0;
    }
}
