using System.Data;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.Database;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace DirectoryService.Core.PositionsMatrix.Commands.ChangeParent;

public class ChangePositionMatrixParentHandler
    : ICommandHandler<ChangePositionMatrixParentResponse, ChangePositionMatrixParentCommand>
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
        FROM positionsmatrix
        WHERE id = @id
        FOR UPDATE;
        """;

    private const string ConflictSql = """
        SELECT EXISTS (
            SELECT 1
            FROM positionsmatrix
            WHERE id <> @positionMatrixId
              AND deleted_at IS NULL
              AND parentid IS NOT DISTINCT FROM @parentId
              AND (name = @name OR slug = @slug)
        );
        """;

    private const string UpdateSql = """
        WITH updated AS (
            UPDATE positionsmatrix
            SET path = CASE
                    WHEN id = @positionMatrixId THEN @newPath::ltree
                    ELSE @newPath::ltree || subpath(path, nlevel(@oldPath::ltree))
                END,
                parentid = CASE
                    WHEN id = @positionMatrixId THEN @parentId::uuid
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
        WHERE id = @positionMatrixId;
        """;

    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ILogger<ChangePositionMatrixParentHandler> _logger;

    public ChangePositionMatrixParentHandler(
        IDbConnectionFactory connectionFactory,
        ILogger<ChangePositionMatrixParentHandler> logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public async Task<Result<ChangePositionMatrixParentResponse, Errors>> ExecuteAsync(
        ChangePositionMatrixParentCommand command,
        CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = await _connectionFactory
            .CreateConnectionAsync(cancellationToken)
            .ConfigureAwait(false);
        using IDbTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

        try
        {
            IReadOnlyList<LockedPositionRow> lockedRows = await LockRowsAsync(
                connection,
                transaction,
                command,
                cancellationToken).ConfigureAwait(false);

            LockedPositionRow? position = lockedRows
                .SingleOrDefault(row => row.Id == command.PositionMatrixId);

            if (position is null || position.DeletedAt is not null)
            {
                transaction.Rollback();
                return Error.NotFound(
                    "position.not_found",
                    $"Должность '{command.PositionMatrixId}' не найдена",
                    command.PositionMatrixId).ToErrors();
            }

            if (command.ParentId == command.PositionMatrixId)
            {
                transaction.Rollback();
                return Error.Validation(
                    "position.move.parent_is_self",
                    "Нельзя переместить должность под самого себя").ToErrors();
            }

            LockedPositionRow? parent = null;
            if (command.ParentId is Guid parentId)
            {
                parent = lockedRows.SingleOrDefault(row => row.Id == parentId);

                if (parent is null)
                {
                    transaction.Rollback();
                    return Error.NotFound(
                        "position.parent.not_found",
                        $"Родительская должность '{parentId}' не найдена",
                        parentId).ToErrors();
                }

                if (parent.DeletedAt is not null)
                {
                    transaction.Rollback();
                    return Error.Conflict(
                        "position.move.parent_deleted",
                        "Родительская должность удалена").ToErrors();
                }

                if (IsDescendantOf(parent.Path, position.Path))
                {
                    transaction.Rollback();
                    return Error.Conflict(
                        "position.move.cycle",
                        "Нельзя переместить должность внутрь собственного поддерева").ToErrors();
                }
            }

            if (position.ParentId == command.ParentId)
            {
                transaction.Commit();
                return new ChangePositionMatrixParentResponse(
                    position.Id,
                    position.ParentId,
                    position.Path,
                    CountDepth(position.Path),
                    position.UpdatedAt);
            }

            string newPath = parent is null
                ? position.Slug
                : parent.Path + "." + position.Slug;

            bool hasConflict = await connection
                .ExecuteScalarAsync<bool>(new CommandDefinition(
                    ConflictSql,
                    new
                    {
                        positionMatrixId = command.PositionMatrixId,
                        parentId = command.ParentId,
                        name = position.Name,
                        slug = position.Slug,
                    },
                    transaction,
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            if (hasConflict)
            {
                transaction.Rollback();
                return GeneralErrors.AlreadyExist().ToErrors();
            }

            MovedPositionRow moved = await connection
                .QuerySingleAsync<MovedPositionRow>(new CommandDefinition(
                    UpdateSql,
                    new
                    {
                        positionMatrixId = command.PositionMatrixId,
                        parentId = command.ParentId,
                        oldPath = position.Path,
                        newPath,
                    },
                    transaction,
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            transaction.Commit();

            _logger.LogInformation(
                "Position {PositionMatrixId} moved from {OldPath} to {NewPath}",
                command.PositionMatrixId,
                position.Path,
                moved.Path);

            return new ChangePositionMatrixParentResponse(
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
                "Concurrent position move conflict for {PositionMatrixId}",
                command.PositionMatrixId);
            return Error.Conflict(
                "position.move.conflict",
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
            _logger.LogError(ex, "Failed to move position {PositionMatrixId}", command.PositionMatrixId);
            throw;
        }
    }

    private static async Task<IReadOnlyList<LockedPositionRow>> LockRowsAsync(
        IDbConnection connection,
        IDbTransaction transaction,
        ChangePositionMatrixParentCommand command,
        CancellationToken cancellationToken)
    {
        Guid[] ids = command.ParentId is null || command.ParentId == command.PositionMatrixId
            ? [command.PositionMatrixId]
            : [command.PositionMatrixId, command.ParentId.Value];

        List<LockedPositionRow> rows = [];

        foreach (Guid id in ids.OrderBy(value => value))
        {
            LockedPositionRow? row = await connection
                .QuerySingleOrDefaultAsync<LockedPositionRow>(new CommandDefinition(
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

    private sealed class LockedPositionRow
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Path { get; set; } = null!;
        public DateTime UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }

    private sealed class MovedPositionRow
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Path { get; set; } = null!;
        public int Depth { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
