using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Core.Database;
using DirectoryService.Core.Departments;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace DirectoryService.Infrastructure.PostgreSQL.Repositories;

public class DepartmentsRepository : IDepartmentsRepository
{
    private readonly IDirectoryServiceDbContext _dbContext;
    private readonly ILogger _logger;

    public DepartmentsRepository(IDirectoryServiceDbContext dbContext, ILogger<DepartmentsRepository> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Guid, Error>> AddAsync(Department department, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.Departments.AddAsync(department, cancellationToken).ConfigureAwait(false);
        return result.Entity.Id.Value;
    }
    public async Task<Result<bool, Error>> DeleteAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        Department? obj = await _dbContext.Departments
        .Where(l => l.Id == new DepartmentId(departmentId))
        .SingleOrDefaultAsync(cancellationToken)
        .ConfigureAwait(false);
        if (obj != null)
        {
            _dbContext.Departments.Remove(obj);
            return true;
        }
        return false;
    }
    public async Task<Result<bool, Error>> DeleteAsync(Department department, CancellationToken cancellationToken = default)
    {
        _dbContext.Departments.Remove(department);
        return true;
    }

    public async Task<Result<List<Department>, Error>> GetAsync(SelectDto request, CancellationToken cancellationToken = default)
    {
        Dictionary<string, StringValues> parsedQuery = QueryHelpers.ParseQuery(request.Search);
        IQueryable<Department> query = _dbContext.Departments;

        foreach (KeyValuePair<string, Microsoft.Extensions.Primitives.StringValues> keySearch in parsedQuery)
        {
            switch (keySearch.Key)
            {
                case nameof(Department.Name):
                    var resultName = DepartmentName.Create(keySearch.Value.ToString());
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
    public async Task<Result<Department?, Error>> GetByIdAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Departments
            .Where(l => l.Id == new DepartmentId(departmentId))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<List<Department>, Error>> GetByParentIdAsync(Guid? parentDepartmentId, CancellationToken cancellationToken = default)
    {
        DepartmentId? parentId = null;
        if (parentDepartmentId != null)
            parentId = new DepartmentId((Guid)parentDepartmentId);
        return await _dbContext.Departments
            .Where(l => l.ParentId == parentId)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<Result<bool, Error>> HasNameSlugAsync(string name, string slug, Guid? parentId, Guid? excludeId, CancellationToken cancellationToken = default)
    {
        var resultName = DepartmentName.Create(name);
        if (resultName.IsFailure)
            return resultName.Error;
        var resultSlug = Slug.Create(slug);
        if (resultSlug.IsFailure)
            return resultSlug.Error;

        bool result;

        DepartmentId? objParentId = null;
        if (parentId != null)
            objParentId = new DepartmentId(parentId.Value);

        if (excludeId != null)
        {
            DepartmentId id = new DepartmentId(excludeId.Value);
            result = await _dbContext.Departments
                .Where(l => (l.Name == resultName.Value || l.Slug == resultSlug.Value) && l.Id != id && l.ParentId == objParentId)
                .AnyAsync(cancellationToken)
                .ConfigureAwait(false);
        }
        else
            result = await _dbContext.Departments
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
    public async Task<Result<bool, Error>> UpdateAsync(Department department, CancellationToken cancellationToken = default)
    {
        var result = _dbContext.Departments
        .Update(department);
        return result.Entity != null;
    }
}
