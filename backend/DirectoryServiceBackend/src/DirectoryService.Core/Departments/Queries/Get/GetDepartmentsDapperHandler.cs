using System.Data;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Domain.Departments;
using DirectoryService.Shared;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;

namespace DirectoryService.Core.Departments.Queries.Get;

/// <summary>
/// Чтение списка подразделений через Dapper.
/// Полный путь ищется рекурсивным CTE по ltree-якорю.
/// </summary>
public class GetDepartmentsDapperHandler : ICommandHandler<GetDepartmentsResponse, GetDepartmentsDapperCommand>
{
    private const int DEFAULT_PAGE_SIZE = 20;
    private const int MAX_PAGE_SIZE = 100;

    private readonly IDbConnectionFactory _connectionFactory;

    public GetDepartmentsDapperHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<GetDepartmentsResponse, Errors>> ExecuteAsync(
        GetDepartmentsDapperCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Request.Page is < 1)
            return GeneralErrors.ValueIsInvalid(nameof(command.Request.Page)).ToErrors();

        if (command.Request.PageSize is < 1 or > MAX_PAGE_SIZE)
            return GeneralErrors.ValueIsInvalid(nameof(command.Request.PageSize)).ToErrors();

        List<string> conditions = ["d.deleted_at IS NULL"];
        var parameters = new DynamicParameters();
        string cte = string.Empty;

        Dictionary<string, StringValues> parsedQuery = QueryHelpers.ParseQuery(command.Request.Search);

        foreach (KeyValuePair<string, StringValues> keySearch in parsedQuery)
        {
            string searchValue = keySearch.Value.ToString();
            if (string.IsNullOrWhiteSpace(searchValue))
                continue;

            string cleanKey = keySearch.Key.Trim();

            if (cleanKey.Equals(nameof(Department.Name), StringComparison.OrdinalIgnoreCase))
            {
                conditions.Add("""lower(d.name) LIKE @namePattern ESCAPE '\'""");
                parameters.Add("namePattern", BuildPattern(searchValue));
            }
            else if (cleanKey.Equals(nameof(Department.Slug), StringComparison.OrdinalIgnoreCase))
            {
                conditions.Add("""lower(d.slug) LIKE @slugPattern ESCAPE '\'""");
                parameters.Add("slugPattern", BuildPattern(searchValue));
            }
            else if (cleanKey.Equals("path", StringComparison.OrdinalIgnoreCase))
            {
                string path = searchValue.Trim().ToLowerInvariant();
                if (!DepartmentSearch.IsValidPath(path))
                    return new GetDepartmentsResponse([], 0);

                cte = """
                    WITH RECURSIVE subtree AS (
                        SELECT anchor.id
                        FROM departments anchor
                        WHERE anchor.pathslugfull = @path::ltree
                          AND anchor.deleted_at IS NULL
                        UNION ALL
                        SELECT child.id
                        FROM departments child
                        JOIN subtree parent ON parent.id = child.parentid
                        WHERE child.deleted_at IS NULL
                    )
                    """;
                conditions.Add("d.id IN (SELECT id FROM subtree)");
                parameters.Add("path", path);
            }
            else
            {
                return GeneralErrors.ValueIsInvalid(cleanKey).ToErrors();
            }
        }

        if (!TryBuildOrderBy(command.Request.OrderBy, out string orderBy))
            return GeneralErrors.ValueIsInvalid(nameof(command.Request.OrderBy)).ToErrors();

        int page = command.Request.Page ?? 1;
        int pageSize = command.Request.PageSize ?? DEFAULT_PAGE_SIZE;

        parameters.Add("skip", (page - 1) * pageSize);
        parameters.Add("take", pageSize);

        string where = $"WHERE {string.Join(" AND ", conditions)}";

        string countSql = $"""
            {cte}
            SELECT count(*)
            FROM departments d
            {where};
            """;

        string itemsSql = $"""
            {cte}
            SELECT
                d.id,
                d.name,
                d.pathslugfull::text AS pathslugfull,
                d.parentid,
                dcp.positionmatrix_id AS chiefpositionid,
                ARRAY(SELECT c.id FROM departments c WHERE c.parentid = d.id AND c.deleted_at IS NULL) AS childsids,
                ARRAY(SELECT dl.location_id FROM department_locations dl WHERE dl.department_id = d.id) AS locationsids,
                ARRAY(SELECT dp.position_id FROM department_positions dp WHERE dp.department_id = d.id) AS positionsids
            FROM departments d
            LEFT JOIN department_chiefpositions dcp ON dcp.department_id = d.id
            {where}
            {orderBy}
            OFFSET @skip LIMIT @take;
            """;

        using IDbConnection connection = await _connectionFactory
            .CreateConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        using SqlMapper.GridReader grid = await connection
            .QueryMultipleAsync(new CommandDefinition(
                $"{countSql}\n{itemsSql}",
                parameters,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        long totalCount = await grid.ReadSingleAsync<long>().ConfigureAwait(false);
        IEnumerable<DepartmentRow> rows = await grid.ReadAsync<DepartmentRow>().ConfigureAwait(false);

        List<GetDepartmentResponse> items = rows
            .Select(row => new GetDepartmentResponse(
                row.Id,
                row.ChiefPositionId,
                row.PathSlugFull,
                row.Name,
                row.ParentId,
                row.ChildsIds?.ToList() ?? [],
                row.LocationsIds?.ToList() ?? [],
                row.PositionsIds?.ToList() ?? []))
            .ToList();

        return new GetDepartmentsResponse(items, totalCount);
    }

    private static string BuildPattern(string value) =>
        $"%{DepartmentSearch.EscapeLike(value.ToLowerInvariant().Trim())}%";

    private static bool TryBuildOrderBy(string? orderBy, out string sql)
    {
        sql = "ORDER BY d.id";

        if (string.IsNullOrWhiteSpace(orderBy))
            return true;

        string[] parts = orderBy.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 2)
            return false;

        string field = parts[0].ToLowerInvariant();
        string direction = parts.Length > 1 ? parts[1].ToLowerInvariant() : string.Empty;

        bool ascending = direction.Length == 0
            || direction.StartsWith("asc", StringComparison.OrdinalIgnoreCase);
        bool descending = direction.StartsWith("desc", StringComparison.OrdinalIgnoreCase);

        if (!ascending && !descending)
            return false;

        string column = field switch
        {
            "name" => "d.name",
            "slug" => "d.slug",
            _ => string.Empty,
        };

        if (column.Length == 0)
            return false;

        sql = $"ORDER BY {column} {(descending ? "DESC" : "ASC")}, d.id";
        return true;
    }

    private sealed class DepartmentRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string PathSlugFull { get; set; } = null!;
        public Guid? ParentId { get; set; }
        public Guid? ChiefPositionId { get; set; }
        public Guid[]? ChildsIds { get; set; }
        public Guid[]? LocationsIds { get; set; }
        public Guid[]? PositionsIds { get; set; }
    }
}
