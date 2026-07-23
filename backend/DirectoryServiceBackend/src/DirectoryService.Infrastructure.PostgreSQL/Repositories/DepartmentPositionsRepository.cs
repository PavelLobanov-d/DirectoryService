using CSharpFunctionalExtensions;
using DirectoryService.Core.Database;
using DirectoryService.Core.DepartmentPositions;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

    public async Task<Result<Guid, Error>> AddAsync(DepartmentPosition obj, CancellationToken cancellationToken)
    {
        var result = await _dbContext.DepartmentPositions.AddAsync(obj, cancellationToken).ConfigureAwait(false);
        return result.Entity.DepartmentId.Value;
    }

    public async Task<Result<DepartmentPosition?, Error>> GetByIdAsync(Guid departmentPositionId, CancellationToken cancellationToken)
    {
        return await _dbContext.DepartmentPositions
            .Where(dp => dp.Id == new DepartmentPositionId(departmentPositionId))
            .Include(v => v.PositionMatrix)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
    public async Task<Result<List<DepartmentPosition>, Error>> GetPositionsByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        return await _dbContext.DepartmentPositions
            .Where(dp => dp.DepartmentId == new DepartmentId(departmentId))
            .Include(v => v.PositionMatrix)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<bool, Error>> DeleteAsync(Guid departmentPositionId, CancellationToken cancellationToken = default)
    {
        DepartmentPosition? obj = await _dbContext.DepartmentPositions
            .Where(l => l.Id == new DepartmentPositionId(departmentPositionId))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
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

    public async Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken)
    {
        int result = await _dbContext
    .SaveChangesAsync(cancellationToken)
    .ConfigureAwait(false);
        return result > 0;
    }
}
