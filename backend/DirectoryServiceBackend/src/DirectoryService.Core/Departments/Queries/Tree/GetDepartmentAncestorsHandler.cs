using System.Data;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Database;

namespace DirectoryService.Core.Departments.Queries.Tree;

public class GetDepartmentAncestorsHandler
    : ICommandHandler<List<DepartmentAncestorResponse>, GetDepartmentAncestorsCommand>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetDepartmentAncestorsHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<List<DepartmentAncestorResponse>, Errors>> ExecuteAsync(
        GetDepartmentAncestorsCommand command,
        CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT path::text
            FROM departments
            WHERE id = @departmentId
              AND deleted_at IS NULL;

            SELECT
                {DepartmentTreeSql.AncestorColumns}
            FROM departments a
            WHERE a.deleted_at IS NULL
              AND a.path @> (
                  SELECT path
                  FROM departments
                  WHERE id = @departmentId
                    AND deleted_at IS NULL
              )
              AND a.id <> @departmentId
            ORDER BY nlevel(a.path), a.id;
            """;

        using IDbConnection connection = await _connectionFactory
            .CreateConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        using SqlMapper.GridReader grid = await connection
            .QueryMultipleAsync(new CommandDefinition(
                sql,
                new { departmentId = command.DepartmentId },
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        string? targetPath = await grid
            .ReadFirstOrDefaultAsync<string?>()
            .ConfigureAwait(false);

        if (targetPath is null)
            return GeneralErrors.NotFound(command.DepartmentId, "Department").ToErrors();

        IEnumerable<DepartmentAncestorRow> rows = await grid
            .ReadAsync<DepartmentAncestorRow>()
            .ConfigureAwait(false);

        return rows.Select(row => row.ToResponse()).ToList();
    }
}
