using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.DepartmentChiefPositions;
using DirectoryService.Core.DepartmentLocations;
using DirectoryService.Core.DepartmentPositions;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.Delete;

public class DeleteDepartmentHandler : ICommandHandler<bool, DeleteDepartmentCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly IDepartmentPositionsRepository _departmentPositionsRepository;
    private readonly IDepartmentLocationsRepository _departmentLocationsRepository;
    private readonly IDepartmentsChiefPositionRepository _departmentChiefPositionsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<DeleteDepartmentHandler> _logger;
    private readonly IStatisticsService _stats;

    public DeleteDepartmentHandler(
        IDepartmentsRepository departmentRepository,
        IDepartmentPositionsRepository departmentPositionsRepository,
        IDepartmentLocationsRepository departmentLocationsRepository,
        IDepartmentsChiefPositionRepository departmentChiefPositionsRepository,
        ITransactionManager transactionManager,
        ILogger<DeleteDepartmentHandler> logger,
        IStatisticsService stats)
    {
        _departmentRepository = departmentRepository;
        _departmentPositionsRepository = departmentPositionsRepository;
        _departmentLocationsRepository = departmentLocationsRepository;
        _departmentChiefPositionsRepository = departmentChiefPositionsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
        _stats = stats;
    }

    public async Task<Result<bool, Errors>> ExecuteAsync(DeleteDepartmentCommand command, CancellationToken cancellationToken = default)
    {
        var resultDepartment = await _departmentRepository.GetByIdAsync(command.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (resultDepartment.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса").ToErrors();
        }
        if (resultDepartment.Value == null)
        {
            _logger.LogError("Record of Department not found {DepartmentId}", command.DepartmentId);
            return GeneralErrors.NotFound(command.DepartmentId, "Department").ToErrors();
        }
        var result = await DeleteAsync(resultDepartment.Value, cancellationToken).ConfigureAwait(false);

        return result;
    }

    private async Task<Result<bool, Errors>> DeleteAsync(Department department, CancellationToken cancellationToken = default)
    {
        // Проверка на существование потомков
        var childrenResult = await _departmentRepository.GetByParentIdAsync(department.Id.Value, cancellationToken).ConfigureAwait(false);
        if (childrenResult.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByParentIdAsync for {DepartmentId}", department.Id);
            return GeneralErrors.Failure("ошибка запроса").ToErrors();
        }

        if (childrenResult.Value.Count > 0)
        {
            _logger.LogWarning("Department {DepartmentId} has child departments. Deletion denied", department.Id);
            return Error.Conflict("rules.failure", "есть зависимые департаменты. Удаление запрещено").ToErrors();
        }

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        // При удалении подразделения его связи удаляются в той же транзакции.
        foreach (DepartmentPosition link in department.DepartmentPositions)
        {
            var resultLinkDelete = await _departmentPositionsRepository.DeleteAsync(link, cancellationToken).ConfigureAwait(false);
            if (resultLinkDelete.IsFailure)
                return await RollbackAsync(transactionScope, "ошибка удаления связи подразделение-должность", cancellationToken).ConfigureAwait(false);
        }

        foreach (DepartmentLocation link in department.DepartmentLocations)
        {
            var resultLinkDelete = await _departmentLocationsRepository.DeleteAsync(link, cancellationToken).ConfigureAwait(false);
            if (resultLinkDelete.IsFailure)
                return await RollbackAsync(transactionScope, "ошибка удаления связи подразделение-локация", cancellationToken).ConfigureAwait(false);
        }

        if (department.DepartmentChiefPosition != null)
        {
            var resultChiefDelete = await _departmentChiefPositionsRepository
                .DeleteAsync(department.DepartmentChiefPosition, cancellationToken)
                .ConfigureAwait(false);
            if (resultChiefDelete.IsFailure)
                return await RollbackAsync(transactionScope, "ошибка удаления связи подразделение-руководитель", cancellationToken).ConfigureAwait(false);
        }

        var deleteResult = await _departmentRepository.DeleteAsync(department, cancellationToken).ConfigureAwait(false);
        if (deleteResult.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.DeleteAsync for {DepartmentId}", department.Id);
            return await RollbackAsync(transactionScope, "ошибка удаления департамента", cancellationToken).ConfigureAwait(false);
        }

        if (deleteResult.Value)
        {
            await _stats.CreateAsync(
                department.Id.Value,
                typeof(Department).Name,
                Statistica.Level.INFO,
                Statistica.Action.DELETE,
                "Удаление",
                department.ParentId?.Value,
                department.ParentId != null ? typeof(Department).Name : null,
                cancellationToken).ConfigureAwait(false);

            _logger.LogInformation("Deleting of Department {DepartmentId}", department.Id);
        }

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (saveResult.IsFailure)
        {
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return saveResult.Error.ToErrors();
        }

        var commitResult = await transactionScope.CommitAsync(cancellationToken).ConfigureAwait(false);
        if (commitResult.IsFailure)
            return commitResult.Error.ToErrors();

        return deleteResult.Value;
    }

    private static async Task<Result<bool, Errors>> RollbackAsync(
        ITransactionScope transactionScope,
        string message,
        CancellationToken cancellationToken)
    {
        var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
        if (rollbackResult.IsFailure)
            return rollbackResult.Error.ToErrors();

        return GeneralErrors.Failure(message).ToErrors();
    }
}
