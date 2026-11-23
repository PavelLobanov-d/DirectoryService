using System.Data;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Database;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DirectoryService.Core.Departments.Commands.ChangeParent;

public class ChangeDepartmentParentHandler
    : ICommandHandler<ChangeDepartmentParentResponse, ChangeDepartmentParentCommand>
{
    // Read Committed достаточно: строки двигаемого узла и нового родителя
    // блокируются через FOR UPDATE, проверки выполняются после блокировки.
    private const string LockSql = """
        SELECT id,
               parentid,
               name,
               slug,
               path::text AS path,
               updated_at AS updatedat,
               deleted_at AS deletedat
        FROM departments
        WHERE id = @id
        FOR UPDATE;
        """;

    private const string ConflictSql = """
        SELECT EXISTS (
            SELECT 1
            FROM departments
            WHERE id <> @departmentId
              AND deleted_at IS NULL
              AND parentid IS NOT DISTINCT FROM @parentId
              AND (name = @name OR slug = @slug)
        );
        """;

    private const string UpdateSql = """
        WITH updated AS (
            UPDATE departments
            SET path = CASE
                    WHEN id = @departmentId THEN @newPath::ltree
                    ELSE @newPath::ltree || subpath(path, nlevel(@oldPath::ltree))
                END,
                parentid = CASE
                    WHEN id = @departmentId THEN @parentId::uuid
                    ELSE parentid
                END,
                updated_at = now()
            WHERE path <@ @oldPath::ltree
            RETURNING id, parentid, path, updated_at
        )
        SELECT id,
               parentid,
               path::text AS path,
               nlevel(path) - 1 AS depth,
               updated_at AS updatedat
        FROM updated
        WHERE id = @departmentId;
        """;

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<ChangeDepartmentParentHandler> _logger;

    public ChangeDepartmentParentHandler(
        IDbConnectionFactory connectionFactory,
        ILogger<ChangeDepartmentParentHandler> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Result<ChangeDepartmentParentResponse, Errors>> ExecuteAsync(
        ChangeDepartmentParentCommand command,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await _connectionFactory
            .CreateConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        using IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            IReadOnlyList<LockedDepartmentRow> lockedRows = await LockRowsAsync(
                connection,
                transaction,
                command,
                cancellationToken).ConfigureAwait(false);

            LockedDepartmentRow? department = lockedRows
                .SingleOrDefault(row => row.Id == command.DepartmentId);

            if (department is null || department.DeletedAt is not null)
            {
                transaction.Rollback();
                return Error.NotFound(
                    "department.not_found",
                    $"Подразделение '{command.DepartmentId}' не найдено",
                    command.DepartmentId).ToErrors();
            }

            if (command.ParentId == command.DepartmentId)
            {
                transaction.Rollback();
                return Error.Validation(
                    "department.move.parent_is_self",
                    "Нельзя переместить подразделение под самого себя").ToErrors();
            }

            LockedDepartmentRow? parent = null;
            if (command.ParentId is Guid parentId)
            {
                parent = lockedRows.SingleOrDefault(row => row.Id == parentId);

                if (parent is null)
                {
                    transaction.Rollback();
                    return Error.NotFound(
                        "department.parent.not_found",
                        $"Родительское подразделение '{parentId}' не найдено",
                        parentId).ToErrors();
                }

                if (parent.DeletedAt is not null)
                {
                    transaction.Rollback();
                    return Error.Conflict(
                        "department.move.parent_deleted",
                        "Родительское подразделение удалено").ToErrors();
                }

                if (IsDescendantOf(parent.Path, department.Path))
                {
                    transaction.Rollback();
                    return Error.Conflict(
                        "department.move.cycle",
                        "Нельзя переместить подразделение внутрь собственного поддерева").ToErrors();
                }
            }

            if (department.ParentId == command.ParentId)
            {
                transaction.Commit();
                return new ChangeDepartmentParentResponse(
                    department.Id,
                    department.ParentId,
                    department.Path,
                    CountDepth(department.Path),
                    department.UpdatedAt);
            }

            string newPath = parent is null
                ? department.Slug
                : parent.Path + "." + department.Slug;

            bool hasConflict = await connection
                .ExecuteScalarAsync<bool>(new CommandDefinition(
                    ConflictSql,
                    new
                    {
                        departmentId = command.DepartmentId,
                        parentId = command.ParentId,
                        name = department.Name,
                        slug = department.Slug,
                    },
                    transaction,
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            if (hasConflict)
            {
                transaction.Rollback();
                return GeneralErrors.AlreadyExist().ToErrors();
            }

            MovedRow moved = await connection
                .QuerySingleAsync<MovedRow>(new CommandDefinition(
                    UpdateSql,
                    new
                    {
                        departmentId = command.DepartmentId,
                        parentId = command.ParentId,
                        oldPath = department.Path,
                        newPath,
                    },
                    transaction,
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            transaction.Commit();

            _logger.LogInformation(
                "Department {DepartmentId} moved from {OldPath} to {NewPath}",
                command.DepartmentId,
                department.Path,
                moved.Path);

            return new ChangeDepartmentParentResponse(
                moved.Id,
                moved.ParentId,
                moved.Path,
                moved.Depth,
                moved.UpdatedAt);
        }
        catch (PostgresException ex) when (ex.SqlState is "40P01" or "40001" or "55P03")
        {
            TryRollback(transaction);
            _logger.LogWarning(
                ex,
                "Concurrent department move conflict for {DepartmentId}",
                command.DepartmentId);
            return Error.Conflict(
                "department.move.conflict",
                "Конфликт параллельного перемещения. Обновите дерево и повторите запрос").ToErrors();
        }
        catch (PostgresException ex) when (ex.SqlState == "23505")
        {
            TryRollback(transaction);
            return GeneralErrors.AlreadyExist().ToErrors();
        }
        catch (Exception ex)
        {
            TryRollback(transaction);
            _logger.LogError(ex, "Failed to move department {DepartmentId}", command.DepartmentId);
            throw;
        }
    }

    private static async Task<IReadOnlyList<LockedDepartmentRow>> LockRowsAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        ChangeDepartmentParentCommand command,
        CancellationToken cancellationToken)
    {
        Guid[] ids = command.ParentId is null || command.ParentId == command.DepartmentId
            ? [command.DepartmentId]
            : [command.DepartmentId, command.ParentId.Value];

        List<LockedDepartmentRow> rows = [];

        foreach (Guid id in ids.OrderBy(value => value))
        {
            LockedDepartmentRow? row = await connection
                .QuerySingleOrDefaultAsync<LockedDepartmentRow>(new CommandDefinition(
                    LockSql,
                    new { id },
                    transaction,
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            if (row is not null)
                rows.Add(row);
        }

        return rows;
    }

    private static void TryRollback(IDbTransaction transaction)
    {
        try
        {
            transaction.Rollback();
        }
        catch
        {
            // Транзакция уже завершена или соединение закрыто.
        }
    }

    private static bool IsDescendantOf(string candidatePath, string ancestorPath) =>
        candidatePath == ancestorPath
        || candidatePath.StartsWith(ancestorPath + ".", StringComparison.Ordinal);

    private static int CountDepth(string path) =>
        path.Count(c => c == '.');

    private sealed class LockedDepartmentRow
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Path { get; set; } = null!;
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    private sealed class MovedRow
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Path { get; set; } = null!;
        public int Depth { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
