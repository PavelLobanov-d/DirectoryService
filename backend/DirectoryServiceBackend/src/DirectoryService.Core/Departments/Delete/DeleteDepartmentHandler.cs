using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace DirectoryService.Core.Departments.Delete;
public class DeleteDepartmentHandler : ICommandHandler<bool, DeleteDepartmentCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<DeleteDepartmentHandler> _logger;
    private readonly IStatisticsService _stats;

    public DeleteDepartmentHandler(
        IDepartmentsRepository departmentRepository,
        ITransactionManager transactionManager,
        ILogger<DeleteDepartmentHandler> logger,
        IStatisticsService stats)
    {
        _departmentRepository = departmentRepository;
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

        var deleteResult = await _departmentRepository.DeleteAsync(department, cancellationToken).ConfigureAwait(false);
        if (deleteResult.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.DeleteAsync for {DepartmentId}", department.Id);
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return GeneralErrors.Failure("ошибка удаления департамента").ToErrors();
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

}
