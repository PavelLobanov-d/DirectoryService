using System.Data;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Database;

namespace DirectoryService.Core.Departments.Queries.Tree;

public class GetDepartmentChildrenHandler
    : ICommandHandler<List<DepartmentTreeItemResponse>, GetDepartmentChildrenCommand>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetDepartmentChildrenHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<List<DepartmentTreeItemResponse>, Errors>> ExecuteAsync(
        GetDepartmentChildrenCommand command,
        CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT EXISTS (
                SELECT 1
                FROM departments parent
                WHERE parent.id = @departmentId
                  AND parent.deleted_at IS NULL
            );

            SELECT
                {DepartmentTreeSql.ItemColumns}
            FROM departments d
            WHERE d.deleted_at IS NULL
              AND d.parentid = @departmentId
            ORDER BY d.name, d.id;
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

        bool parentExists = await grid.ReadSingleAsync<bool>().ConfigureAwait(false);
        if (!parentExists)
            return GeneralErrors.NotFound(command.DepartmentId, "Department").ToErrors();

        IEnumerable<DepartmentTreeRow> rows = await grid
            .ReadAsync<DepartmentTreeRow>()
            .ConfigureAwait(false);

        return rows.Select(row => row.ToItemResponse()).ToList();
    }
}
