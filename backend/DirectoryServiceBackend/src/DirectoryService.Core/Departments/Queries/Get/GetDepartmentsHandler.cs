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

public class GetDepartmentsHandler : ICommandHandler<GetDepartmentsResponse, GetDepartmentCommand>
{
    private const int DEFAULT_PAGE_SIZE = 20;
    private const int MAX_PAGE_SIZE = 100;

    private readonly IReadDbContext _readDbContext;
    private readonly ILogger<GetDepartmentsHandler> _logger;

    public GetDepartmentsHandler(
        IReadDbContext readDbContext,
        ILogger<GetDepartmentsHandler> logger)
    {
        _readDbContext = readDbContext;
        _logger = logger;
    }

    public async Task<Result<GetDepartmentsResponse, Errors>> ExecuteAsync(
        GetDepartmentCommand command,
        CancellationToken cancellationToken = default)
    {
        Dictionary<string, StringValues> parsedQuery = QueryHelpers.ParseQuery(command.Request.Search);
        IQueryable<Department> query = _readDbContext.DepartmentsRead;

        foreach (KeyValuePair<string, StringValues> keySearch in parsedQuery)
        {
            string searchValue = keySearch.Value.ToString();
            if (string.IsNullOrWhiteSpace(searchValue))
                continue;

            string cleanKey = keySearch.Key.Trim();
            string pattern = $"%{EscapeLike(searchValue.ToLowerInvariant())}%";

            if (cleanKey.Equals(nameof(Department.Name), StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => EF.Functions.Like(
                    ((string)d.Name).ToLower(),
                    pattern,
                    "\\"));
            }
            else if (cleanKey.Equals(nameof(Department.Slug), StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => EF.Functions.Like(
                    ((string)d.Slug).ToLower(),
                    pattern,
                    "\\"));
            }
            else
            {
                return GeneralErrors.ValueIsInvalid(cleanKey).ToErrors();
            }
        }

        if (command.Request.Page is < 1)
            return GeneralErrors.ValueIsInvalid(nameof(command.Request.Page)).ToErrors();

        if (command.Request.PageSize is < 1 or > MAX_PAGE_SIZE)
            return GeneralErrors.ValueIsInvalid(nameof(command.Request.PageSize)).ToErrors();

        string? orderBy = command.Request.OrderBy?.Trim();
        if (!string.IsNullOrEmpty(orderBy))
        {
            string[] param = orderBy.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (param.Length > 2)
                return GeneralErrors.ValueIsInvalid(nameof(command.Request.OrderBy)).ToErrors();

            string field = param[0].ToLowerInvariant();
            string direction = param.Length > 1 ? param[1].ToLowerInvariant() : string.Empty;

            bool ascending = direction.Length == 0 || direction.StartsWith("asc", StringComparison.OrdinalIgnoreCase);
            bool descending = direction.StartsWith("desc", StringComparison.OrdinalIgnoreCase);

            if (!ascending && !descending)
                return GeneralErrors.ValueIsInvalid(nameof(command.Request.OrderBy)).ToErrors();

            switch (field)
            {
                case "name":
                    query = descending
                        ? query.OrderByDescending(d => d.Name).ThenBy(d => d.Id)
                        : query.OrderBy(d => d.Name).ThenBy(d => d.Id);
                    break;
                case "slug":
                    query = descending
                        ? query.OrderByDescending(d => d.Slug).ThenBy(d => d.Id)
                        : query.OrderBy(d => d.Slug).ThenBy(d => d.Id);
                    break;
                default:
                    return GeneralErrors.ValueIsInvalid(nameof(command.Request.OrderBy)).ToErrors();
            }
        }
        else
        {
            query = query.OrderBy(d => d.Id);
        }

        long totalCount = await query.LongCountAsync(cancellationToken).ConfigureAwait(false);

        int page = command.Request.Page ?? 1;
        int pageSize = command.Request.PageSize ?? DEFAULT_PAGE_SIZE;
        query = query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var rows = await query
            .Select(d => new
            {
                d.Id,
                ChiefPositionId = d.DepartmentChiefPosition != null
                    ? d.DepartmentChiefPosition.PositionMatrixId
                    : null,
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

        var items = rows
            .Select(row =>
            {
                PathSlug pathSlugFull = row.PathSlug is null
                    ? PathSlug.Create(row.Slug).Value
                    : row.PathSlug.CreateChild(row.Slug).Value;

                return new GetDepartmentResponse(
                    row.Id.Value,
                    row.ChiefPositionId?.Value,
                    pathSlugFull.Value,
                    row.Name.Value,
                    row.ParentId?.Value,
                    row.ChildsIds.Select(c => c.Value).ToList(),
                    row.LocationsIds.Select(l => l.Value).ToList(),
                    row.PositionsIds.Select(p => p.Value).ToList());
            })
            .ToList();

        return new GetDepartmentsResponse(items, totalCount);
    }

    private static string EscapeLike(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);
}
