using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.DepartmentChiefPositions;
using DirectoryService.Core.DepartmentPositions;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.PositionsMatrix.Delete;

public class PositionMatrixDeleteHandler : ICommandHandler<bool, PositionMatrixDeleteCommand>
{
    private readonly IPositionMatrixRepository _positionMatrixRepository;
    private readonly IDepartmentPositionsRepository _departmentPositionsRepository;
    private readonly IDepartmentsChiefPositionRepository _departmentChiefPositionsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<PositionMatrixDeleteHandler> _logger;
    private readonly IStatisticsService _stats;

    public PositionMatrixDeleteHandler(
        IPositionMatrixRepository positionMatrixRepository,
        IDepartmentPositionsRepository departmentPositionsRepository,
        IDepartmentsChiefPositionRepository departmentChiefPositionsRepository,
        ITransactionManager transactionManager,
        ILogger<PositionMatrixDeleteHandler> logger,
        IStatisticsService stats)
    {
        _positionMatrixRepository = positionMatrixRepository;
        _departmentPositionsRepository = departmentPositionsRepository;
        _departmentChiefPositionsRepository = departmentChiefPositionsRepository;
        _transactionManager = transactionManager;
        _logger = logger;
        _stats = stats;
    }

    public async Task<Result<bool, Errors>> ExecuteAsync(PositionMatrixDeleteCommand command, CancellationToken cancellationToken = default)
    {
        var resultPosition = await _positionMatrixRepository.GetByIdAsync(command.PositionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPosition.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска должности").ToErrors();
        }
        if (resultPosition.Value == null)
        {
            _logger.LogError("Record of PositionMatrix not found {PositionMatrixId}", command.PositionMatrixId);
            return GeneralErrors.NotFound(command.PositionMatrixId, "Position").ToErrors();
        }
        var result = await DeleteAsync(resultPosition.Value, cancellationToken).ConfigureAwait(false);

        return result;
    }

    private async Task<Result<bool, Errors>> DeleteAsync(PositionMatrix positionMatrix, CancellationToken cancellationToken = default)
    {
        //проверка на существование потомков
        var resultHasExists = await _positionMatrixRepository.GetByParentIdAsync(positionMatrix.Id.Value, cancellationToken).ConfigureAwait(false);
        if (resultHasExists.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByParentIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска должностей по родительской").ToErrors();
        }
        if (resultHasExists.Value.Count > 0)
        {
            return Error.Conflict("rules.failure", "есть зависимые должности. Удаление запрещено").ToErrors();
        }

        //должность руководителя подразделения нельзя удалить: подразделение не может остаться без руководителя
        var resultChiefLinks = await _departmentChiefPositionsRepository
            .GetByAsync(dcp => dcp.PositionMatrixId == positionMatrix.Id, cancellationToken)
            .ConfigureAwait(false);
        if (resultChiefLinks.IsFailure)
        {
            _logger.LogError("Error _departmentChiefPositionsRepository.GetByAsync");
            return GeneralErrors.Failure("ошибка запроса поиска связи должности с подразделением").ToErrors();
        }
        if (resultChiefLinks.Value is { Count: > 0 })
        {
            _logger.LogWarning("PositionMatrix {PositionMatrixId} is a chief position. Deletion denied", positionMatrix.Id);
            return Error.Conflict("rules.failure", "должность является руководителем подразделения. Удаление запрещено").ToErrors();
        }

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        // При удалении должности связи с подразделениями удаляются в той же транзакции.
        var resultLinks = await _departmentPositionsRepository
            .GetByAsync(dp => dp.PositionMatrixId == positionMatrix.Id, cancellationToken)
            .ConfigureAwait(false);
        if (resultLinks.IsFailure)
        {
            _logger.LogError("Error _departmentPositionsRepository.GetByAsync");
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return GeneralErrors.Failure("ошибка запроса поиска связей должности").ToErrors();
        }

        foreach (DepartmentPosition link in resultLinks.Value ?? [])
        {
            var resultLinkDelete = await _departmentPositionsRepository.DeleteAsync(link, cancellationToken).ConfigureAwait(false);
            if (resultLinkDelete.IsFailure)
            {
                _logger.LogError("Error _departmentPositionsRepository.DeleteAsync");
                var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
                if (rollbackResult.IsFailure)
                    return rollbackResult.Error.ToErrors();

                return GeneralErrors.Failure("ошибка удаления связи подразделение-должность").ToErrors();
            }
        }

        var result = await _positionMatrixRepository.DeleteAsync(positionMatrix, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.DeleteAsync");
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return result.Error.ToErrors();
        }

        if (result.Value)
        {
            await _stats.CreateAsync(
                positionMatrix.Id.Value,
                typeof(PositionMatrix).Name,
                Statistica.Level.INFO,
                Statistica.Action.DELETE,
                "Удаление",
                positionMatrix.ParentId?.Value,
                positionMatrix.ParentId != null ? nameof(PositionMatrix) : null,
                cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Deleting of PositionMatrix {PositionMatrixId}", positionMatrix.Id);
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

        return result.Value;
    }
}
