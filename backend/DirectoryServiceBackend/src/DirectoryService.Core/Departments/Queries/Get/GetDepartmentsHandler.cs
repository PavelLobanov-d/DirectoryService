using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.shared;
using DirectoryService.Shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;

namespace DirectoryService.Core.Departments.Queries.Get;

public class GetDepartmentsHandler : ICommandHandler<List<GetDepartmentResponse>, GetDepartmentCommand>
{
    private readonly IReadDbContext _readDbContext;
    private readonly ILogger<GetDepartmentsHandler> _logger;

    public GetDepartmentsHandler(
        IReadDbContext readDbContext,
        ILogger<GetDepartmentsHandler> logger)
    {
        _readDbContext = readDbContext;
        _logger = logger;
    }
    public async Task<Result<List<GetDepartmentResponse>, Errors>> ExecuteAsync(GetDepartmentCommand command, CancellationToken cancellationToken = default)
    {
        Dictionary<string, StringValues> parsedQuery = QueryHelpers.ParseQuery(command.Request.Search);
        IQueryable<Department> query = _readDbContext.DepartmentsRead;

        foreach (KeyValuePair<string, StringValues> keySearch in parsedQuery)
        {
            if (keySearch.Key.Equals(nameof(Department.Name), StringComparison.OrdinalIgnoreCase))
            {
                var resultName = DepartmentName.Create(keySearch.Value.ToString());
                if (resultName.IsFailure)
                    return resultName.Error.ToErrors();

                query = query.Where(p => p.Name == resultName.Value);
            }
            else if (keySearch.Key.Equals(nameof(Department.Slug), StringComparison.OrdinalIgnoreCase))
            {
                var resultSlug = Slug.Create(keySearch.Value.ToString());
                if (resultSlug.IsFailure)
                    return resultSlug.Error.ToErrors();

                query = query.Where(p => p.Slug == resultSlug.Value);
            }
        }

        if (command.Request.OrderBy != null && !command.Request.OrderBy.Trim().Equals(string.Empty))
        {
            string[] param = command.Request.OrderBy.Split(' ');
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
                    default:
                        query = query.OrderByDescending(p => p.Id);
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
                    default:
                        query = query.OrderBy(p => p.Id);
                        break;
                }
            }
        }
        else
            query = query.OrderBy(p => p.Id);

        if (command.Request.Page != null && command.Request.PageSize != null)
        {
            int skiprecords = ((int)command.Request.Page - 1) * (int)command.Request.PageSize;
            query = query
            .Skip(skiprecords)
            .Take((int)command.Request.PageSize);
        }

        var rows = await query
            .Select(d => new
            {
                d.Id,
                ChiefPositionId = d.DepartmentChiefPosition.PositionMatrixId,
                d.PathSlug,
                d.Slug,
                d.Name,
                d.ParentId,
                ChildsIds = d.Childs.Select(c => c.Id).ToList(),
                LocationsIds = d.DepartmentLocations.Select(dl => dl.LocationId).ToList(),
                PositionsIds = d.DepartmentPositions.Select(dp => dp.PositionMatrixId).ToList(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var rowsDTO = rows
            .Select(row =>
             {
                 PathSlug pathSlugFull = row.PathSlug is null
                     ? PathSlug.Create(row.Slug).Value
                     : row.PathSlug.CreateChild(row.Slug).Value;

                 return new GetDepartmentResponse(
                     row.Id.Value,
                     row.ChiefPositionId.Value,
                     pathSlugFull.Value,
                     row.Name.Value,
                     row.ParentId?.Value,
                     row.ChildsIds.Select(c => c.Value).ToList(),
                     row.LocationsIds.Select(l => l.Value).ToList(),
                     row.PositionsIds.Select(p => p.Value).ToList());
             })
            .ToList();

        return Result.Success<List<GetDepartmentResponse>, Errors>(rowsDTO);
    }
}
