using System.Data;
using CSharpFunctionalExtensions;
using Dapper;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.Commands.ChangeParent;

public class ChangeDepartmentParentHandler
    : ICommandHandler<ChangeDepartmentParentResponse, ChangeDepartmentParentCommand>
{
    private const string DepartmentSql = """
        SELECT id,
               parentid,
               name,
               slug,
               path::text AS path,
               updated_at AS updatedat
        FROM departments
        WHERE id = @departmentId
          AND deleted_at IS NULL;
        """;

    private const string ParentSql = """
        SELECT id,
               path::text AS path,
               deleted_at AS deletedat
        FROM departments
        WHERE id = @parentId;
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
        using IDbTransaction transaction = connection.BeginTransaction();

        try
        {
            DepartmentRow? department = await connection
                .QuerySingleOrDefaultAsync<DepartmentRow>(new CommandDefinition(
                    DepartmentSql,
                    new { departmentId = command.DepartmentId },
                    transaction,
                    cancellationToken: cancellationToken))
                .ConfigureAwait(false);

            if (department is null)
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

            ParentRow? parent = null;
            if (command.ParentId is Guid parentId)
            {
                parent = await connection
                    .QuerySingleOrDefaultAsync<ParentRow>(new CommandDefinition(
                        ParentSql,
                        new { parentId },
                        transaction,
                        cancellationToken: cancellationToken))
                    .ConfigureAwait(false);

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
        catch (Exception ex)
        {
            try
            {
                transaction.Rollback();
            }
            catch (Exception rollbackException)
            {
                _logger.LogError(rollbackException, "Failed to rollback department move transaction");
            }

            _logger.LogError(ex, "Failed to move department {DepartmentId}", command.DepartmentId);
            throw;
        }
    }

    private static bool IsDescendantOf(string candidatePath, string ancestorPath) =>
        candidatePath == ancestorPath
        || candidatePath.StartsWith(ancestorPath + ".", StringComparison.Ordinal);

    private static int CountDepth(string path) =>
        path.Count(c => c == '.');

    private sealed class DepartmentRow
    {
        public Guid Id { get; set; }
        public Guid? ParentId { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string Path { get; set; } = null!;
        public DateTime UpdatedAt { get; set; }
    }

    private sealed class ParentRow
    {
        public Guid Id { get; set; }
        public string Path { get; set; } = null!;
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
