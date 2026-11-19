using System.Data;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Shared;

namespace DirectoryService.Core.Departments.Queries.Tree;

public class GetDepartmentTreeHandler
    : ICommandHandler<List<DepartmentTreeItemResponse>, GetDepartmentTreeCommand>
{
    private readonly IDbConnectionFactory _connectionFactory;

    public GetDepartmentTreeHandler(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Result<List<DepartmentTreeItemResponse>, Errors>> ExecuteAsync(
        GetDepartmentTreeCommand command,
        CancellationToken cancellationToken = default)
    {
        string sql = $"""
            SELECT
                {DepartmentTreeSql.ItemColumns}
            FROM departments d
            WHERE d.deleted_at IS NULL
              AND d.parentid IS NULL
            ORDER BY d.name, d.id;
            """;

        using IDbConnection connection = await _connectionFactory
            .CreateConnectionAsync(cancellationToken)
            .ConfigureAwait(false);

        IEnumerable<DepartmentTreeRow> rows = await connection
            .QueryAsync<DepartmentTreeRow>(new CommandDefinition(
                sql,
                cancellationToken: cancellationToken))
            .ConfigureAwait(false);

        return rows.Select(row => row.ToItemResponse()).ToList();
    }
}
