using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Core.Database;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;


namespace DirectoryService.Infrastructure.PostgreSQL.Repositories;

public class PositionMatrixRepository : IPositionMatrixRepository
{
    private readonly IDirectoryServiceDbContext _dbContext;
    private readonly ILogger _logger;

    public PositionMatrixRepository(IDirectoryServiceDbContext dbContext, ILogger<PositionMatrixRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Guid, Error>> AddAsync(PositionMatrix positionMatrix, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.PositionsMatrix.AddAsync(positionMatrix, cancellationToken).ConfigureAwait(false);
        return result.Entity.Id.Value;
    }
    public async Task<Result<bool, Error>> DeleteAsync(Guid positionMatrixId, CancellationToken cancellationToken = default)
    {
        PositionMatrix? obj = await _dbContext.PositionsMatrix
        .Where(l => l.Id == new PositionMatrixId(positionMatrixId))
        .SingleOrDefaultAsync(cancellationToken)
        .ConfigureAwait(false);
        if (obj != null)
        {
            var result = _dbContext.PositionsMatrix.Remove(obj);
            return result != null;
        }
        return false;
    }
    public async Task<Result<bool, Error>> DeleteAsync(PositionMatrix positionMatrix, CancellationToken cancellationToken = default)
    {
        var result = _dbContext.PositionsMatrix.Remove(positionMatrix);
        return result != null;
    }

    public async Task<Result<List<PositionMatrix>, Error>> GetAsync(SelectDto request, CancellationToken cancellationToken = default)
    {
        Dictionary<string, StringValues> parsedQuery = QueryHelpers.ParseQuery(request.Search);
        IQueryable<PositionMatrix> query = _dbContext.PositionsMatrix;

        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> keySearch in parsedQuery)
        {
            switch (keySearch.Key)
            {
                case nameof(PositionMatrix.Name):
                    var resultName = PositionName.Create(keySearch.Value.ToString());
                    query = query.Where(p => p.Name == resultName.Value);
                    break;
                case nameof(PositionMatrix.Slug):
                    var resultSlug = Slug.Create(keySearch.Value.ToString());
                    query = query.Where(p => p.Slug == resultSlug.Value);
                    break;
            }
        }

        if (request.OrderBy != null && !request.OrderBy.Trim().Equals(string.Empty))
        {
            string[] param = request.OrderBy.Split(' ');
            string field = param[0];
            string orderType = "";
            if (param.Length > 1)
            {
                orderType = param[1];
            }
            if (orderType.ToLowerInvariant().StartsWith("desc", StringComparison.OrdinalIgnoreCase))
            {
                switch (field.ToLowerInvariant())
                {
                    case "name":
                        query = query.OrderByDescending(p => p.Name);
                        break;
                    case "slug":
                        query = query.OrderByDescending(p => p.Slug);
                        break;
                }
            }
            else if (orderType.ToLowerInvariant().StartsWith("asc", StringComparison.OrdinalIgnoreCase) || orderType == "")
            {
                switch (field.ToLowerInvariant())
                {
                    case "name":
                        query = query.OrderBy(p => p.Name);
                        break;
                    case "slug":
                        query = query.OrderBy(p => p.Slug);
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

        var resultSelect = await query
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return resultSelect;
    }
    public async Task<Result<PositionMatrix?, Error>> GetByIdAsync(Guid positionMatrixId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.PositionsMatrix
            .Where(l => l.Id == new PositionMatrixId(positionMatrixId))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<List<PositionMatrix>, Error>> GetByParentIdAsync(Guid? parentPositionMatrixId, CancellationToken cancellationToken = default)
    {
        PositionMatrixId? parentId = null;
        if (parentPositionMatrixId != null)
            parentId = new PositionMatrixId((Guid)parentPositionMatrixId);
        return await _dbContext.PositionsMatrix
            .Where(l => l.ParentId == parentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<bool, Error>> HasNameSlugAsync(string name, string slug, Guid? parentId, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var resultName = PositionName.Create(name);
        if (resultName.IsFailure)
            return resultName.Error;
        var resultSlug = Slug.Create(slug);
        if (resultSlug.IsFailure)
            return resultSlug.Error;

        bool result;

        PositionMatrixId? objParentId = null;
        if(parentId != null)
            objParentId = new PositionMatrixId(parentId.Value);

        if (excludeId != null)
        {
            PositionMatrixId id = new PositionMatrixId(excludeId.Value);
            result = await _dbContext.PositionsMatrix
                .Where(l => (l.Name == resultName.Value || l.Slug == resultSlug.Value) && l.Id != id && l.ParentId == objParentId)
                .AnyAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        else
            result = await _dbContext.PositionsMatrix
            .Where(l => (l.Name == resultName.Value || l.Slug == resultSlug.Value) && l.ParentId == objParentId)
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
    public async Task<Result<bool, Error>> UpdateAsync(PositionMatrix positionMatrix, CancellationToken cancellationToken = default)
    {
        var result = _dbContext.PositionsMatrix
        .Update(positionMatrix);
        return result.Entity != null;
    }
}
