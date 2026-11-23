using System.Data;
using System.Text.Json;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Database;

namespace DirectoryService.Core.Departments.Queries.Tree;

public class SearchDepartmentTreeHandler
    : ICommandHandler<List<DepartmentTreeSearchItemResponse>, SearchDepartmentTreeCommand>
{
    private const int MAX_RESULTS = 50;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IDbConnectionFactory _connectionFactory;

    public SearchDepartmentTreeHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<List<DepartmentTreeSearchItemResponse>, Errors>> ExecuteAsync(
        SearchDepartmentTreeCommand command,
        CancellationToken cancellationToken = default)
    {
        string query = command.Query?.Trim() ?? string.Empty;
        if (query.Length < 2)
            return GeneralErrors.ValueIsInvalid("q").ToErrors();

        string pattern = $"%{DepartmentSearch.EscapeLike(query.ToLowerInvariant())}%";

        string sql = $"""
            SELECT
                m.id,
                m.name,
                m.path::text AS path,
                nlevel(m.path) - 1 AS depth,
                EXISTS (
                    SELECT 1
                    FROM departments child
                    WHERE child.parentid = m.id
                      AND child.deleted_at IS NULL
                ) AS haschildren,
                (
                    SELECT count(*)::int
                    FROM departments child
                    WHERE child.parentid = m.id
                      AND child.deleted_at IS NULL
                ) AS childrencount,
                COALESCE((
                    SELECT jsonb_agg(
                        jsonb_build_object(
                            'id', a.id,
                            'name', a.name,
                            'path', a.path::text,
                            'depth', nlevel(a.path) - 1
                        )
                        ORDER BY nlevel(a.path)
                    )
                    FROM departments a
                    WHERE a.deleted_at IS NULL
                      AND a.path @> m.path
                      AND a.id <> m.id
                ), '[]'::jsonb)::text AS ancestorsjson
            FROM departments m
            WHERE m.deleted_at IS NULL
              AND lower(m.name) LIKE @pattern ESCAPE '\'
            ORDER BY nlevel(m.path), m.name, m.id
            LIMIT @limit;
            """;

        using IDbConnection connection = await _connectionFactory
            .CreateConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        IEnumerable<DepartmentTreeSearchRow> rows = await connection
            .QueryAsync<DepartmentTreeSearchRow>(new CommandDefinition(
                sql,
                new { pattern, limit = MAX_RESULTS },
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return rows
            .Select(row => new DepartmentTreeSearchItemResponse(
                row.Id,
                row.Name,
                row.Path,
                row.Depth,
                row.HasChildren,
                row.ChildrenCount,
                JsonSerializer.Deserialize<List<DepartmentAncestorResponse>>(
                    row.AncestorsJson,
                    JsonOptions) ?? []))
            .ToList();
    }

    private sealed class DepartmentTreeSearchRow
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string Path { get; set; } = null!;
        public int Depth { get; set; }
        public bool HasChildren { get; set; }
        public int ChildrenCount { get; set; }
        public string AncestorsJson { get; set; } = "[]";
    }
}
