using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.DepartmentPositions;
using DirectoryService.Core.Departments.DetachLocation;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace DirectoryService.Core.Departments.DetachPosition;

public class DepartmentDetachPositionHandler : ICommandHandler<bool, DepartmentDetachPositionCommand>
{
    private readonly IDepartmentPositionsRepository _departmentPositionsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<DepartmentsService> _logger;
    private readonly IStatisticsService _stats;

    public DepartmentDetachPositionHandler(
        IDepartmentPositionsRepository departmentPositionsRepository,
        ITransactionManager transactionManager,
        ILogger<DepartmentsService> logger,
        IStatisticsService stats)
    {
        _departmentPositionsRepository = departmentPositionsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
        _stats = stats;
    }
    public async Task<Result<bool, Errors>> ExecuteAsync(
    DepartmentDetachPositionCommand command,
    CancellationToken cancellationToken = default)
    {
        //поиск линка
        var resultLink = await _departmentPositionsRepository.GetByIdAsync(command.DepartmentPositionId, cancellationToken).ConfigureAwait(false);
        if (resultLink.IsFailure)
        {
            _logger.LogError("Error _departmentPositionsRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска связи департамент-должность").ToErrors();
        }
        if (resultLink.Value == null)
        {
            return GeneralErrors.NotFound(command.DepartmentPositionId, "DepartmentPosition").ToErrors();
        }
        DepartmentPosition dp = resultLink.Value;

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        var resultDelete = await _departmentPositionsRepository.DeleteAsync(dp, cancellationToken).ConfigureAwait(false);
        if (resultDelete.IsFailure)
        {
            _logger.LogError("Error _departmentPositionsRepository.DeleteAsync");
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return GeneralErrors.Failure("ошибка удаления связи департамент-должность").ToErrors();
        }
        if (resultDelete.Value)
        {
            await _stats.CreateAsync(
            dp.PositionMatrixId.Value,
            typeof(PositionMatrix).Name,
            Statistica.Level.INFO,
            Statistica.Action.DETACH,
            $"Отсоединена должность {resultLink.Value.PositionMatrix.Name.Value}",
            dp.DepartmentId.Value,
            typeof(Department).Name,
            cancellationToken).ConfigureAwait(false);
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

        return resultDelete.Value;
    }
}
