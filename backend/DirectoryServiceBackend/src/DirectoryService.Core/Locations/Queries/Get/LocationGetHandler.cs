using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts;
using DirectoryService.Contracts.Locations;
using DirectoryService.Core.Database;
using Microsoft.Extensions.Logging;
using System.Data;

namespace DirectoryService.Core.Locations.Queries.Get;

public class LocationGetHandler : ICommandHandler<PagedResult<LocationListItemDto>, LocationGetCommand>
{
    private const int DEFAULT_PAGE_SIZE = 20;
    private const int MAX_PAGE_SIZE = 100;

    private static readonly IReadOnlyDictionary<string, string> SortExpressions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["name"] = "lower(name)",
            ["createdat"] = "created_at",
            ["created_at"] = "created_at",
            ["date"] = "created_at",
            ["departmentcount"] = "department_count",
            ["department_count"] = "department_count",
        };

    private const string FilteredLocationsCte = """
        WITH filtered_locations AS (
            SELECT l.id,
                   l.name,
                   l.address,
                   l.created_at,
                   count(dl.id)::int AS department_count
            FROM locations AS l
            LEFT JOIN department_locations AS dl ON dl.location_id = l.id
            WHERE l.deleted_at IS NULL
              AND (@searchPattern IS NULL OR l.name ILIKE @searchPattern ESCAPE '\')
            GROUP BY l.id, l.name, l.address, l.created_at
            HAVING (@minDepartmentCount IS NULL OR count(dl.id) >= @minDepartmentCount)
        )
        """;

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<LocationGetHandler> _logger;

    public LocationGetHandler(
        IDbConnectionFactory connectionFactory,
        ILogger<LocationGetHandler> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Result<PagedResult<LocationListItemDto>, Errors>> ExecuteAsync(
        LocationGetCommand command,
        CancellationToken cancellationToken = default)
    {
        GetLocationsRequest request = command.Request;

        int page = request.Page ?? 1;
        if (page < 1)
            return GeneralErrors.ValueIsInvalid(nameof(request.Page)).ToErrors();

        int pageSize = request.PageSize ?? DEFAULT_PAGE_SIZE;
        if (pageSize is < 1 or > MAX_PAGE_SIZE)
            return GeneralErrors.ValueIsInvalid(nameof(request.PageSize)).ToErrors();

        if (request.MinDepartmentCount is < 0)
            return GeneralErrors.ValueIsInvalid(nameof(request.MinDepartmentCount)).ToErrors();

        string sortBy = request.SortBy?.Trim() ?? "name";
        if (!SortExpressions.TryGetValue(sortBy, out string? sortExpression))
            return GeneralErrors.ValueIsInvalid(nameof(request.SortBy)).ToErrors();

        string sortDir = request.SortDir?.Trim().ToLowerInvariant() ?? "asc";
        string sqlSortDirection;
        if (sortDir is "" or "asc")
            sqlSortDirection = "ASC";
        else if (sortDir == "desc")
            sqlSortDirection = "DESC";
        else
            return GeneralErrors.ValueIsInvalid(nameof(request.SortDir)).ToErrors();

        string? searchPattern = string.IsNullOrWhiteSpace(request.Search)
            ? null
            : $"%{EscapeLike(request.Search.Trim())}%";

        long offset = ((long)page - 1) * pageSize;

        string sql = $"""
            {FilteredLocationsCte},
            total AS (
                SELECT count(*) AS total_count
                FROM filtered_locations
            )
            SELECT t.total_count AS "TotalCount",
                   p.id AS "Id",
                   p.name AS "Name",
                   p.address AS "Address",
                   p.created_at AS "CreatedAt",
                   p.department_count AS "DepartmentCount"
            FROM total AS t
            LEFT JOIN LATERAL (
                SELECT id,
                       name,
                       address,
                       created_at,
                       department_count
                FROM filtered_locations
                ORDER BY {sortExpression} {sqlSortDirection}, id ASC
                LIMIT @pageSize OFFSET @offset
            ) AS p ON true;
            """;

        var parameters = new
        {
            searchPattern,
            minDepartmentCount = request.MinDepartmentCount,
            pageSize,
            offset,
        };

        using IDbConnection connection = await _connectionFactory
            .CreateConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        List<LocationPageRow> rows = (await connection
            .QueryAsync<LocationPageRow>(sql, parameters)
            .ConfigureAwait(false))
            .ToList();

        long totalCount = rows.Count > 0 ? rows[0].TotalCount : 0;
        List<LocationListItemDto> items = rows
            .Where(row => row.Id.HasValue)
            .Select(row => new LocationListItemDto(
                row.Id!.Value,
                row.Name!,
                row.Address!,
                row.CreatedAt!.Value,
                row.DepartmentCount!.Value))
            .ToList();

        _logger.LogInformation(
            "Locations page {Page} size {PageSize} returned {Count} of {TotalCount}",
            page,
            pageSize,
            items.Count,
            totalCount);

        return new PagedResult<LocationListItemDto>(items, totalCount, page, pageSize);
    }

    private static string EscapeLike(string value) =>
        value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

    private sealed class LocationPageRow
    {
        public long TotalCount { get; set; }
        public Guid? Id { get; set; }
        public string? Name { get; set; }
        public string? Address { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? DepartmentCount { get; set; }
    }
}
