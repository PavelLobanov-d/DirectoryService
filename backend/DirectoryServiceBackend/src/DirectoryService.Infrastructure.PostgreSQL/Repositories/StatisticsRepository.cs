using CSharpFunctionalExtensions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using System;
using System.Collections.Generic;
using System.Text;

namespace DirectoryService.Infrastructure.PostgreSQL.Repositories;

internal class StatisticsRepository : IStatisticsRepository
{
    private readonly IDirectoryServiceDbContext _dbContex;
    public StatisticsRepository(IDirectoryServiceDbContext dbContex)
    {
        _dbContex = dbContex;
    }
    public async Task<Result<Guid, Errors>> AddAsync(Statistica statistica, CancellationToken cancellationToken = default)
    {
        var result = await _dbContex.Statistics.AddAsync(statistica, cancellationToken).ConfigureAwait(false);
        return result.Entity.Id;
    }
    public Task<Result<List<Statistica>, Error>> GetByObjectIdAsync(Guid objectId, CancellationToken cancellationToken) => throw new NotImplementedException();
    public Task<Result<List<Statistica>, Error>> GetByParentIdAsync(Guid parentId, CancellationToken cancellationToken) => throw new NotImplementedException();
}
