using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace DirectoryService.Core.PositionsMatrix.ChangeParent;

public class PositionMatrixChangeParentHandler : ICommandHandler<bool, PositionMatrixChangeParentCommand>
{
    private readonly IPositionMatrixRepository _positionMatrixRepository;
    private readonly IPositionMatrixService _positionMatrixService;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<PositionMatrixChangeParentHandler> _logger;
    private readonly IStatisticsService _stats;

    public PositionMatrixChangeParentHandler(
        IPositionMatrixRepository positionMatrixRepository,
        IPositionMatrixService positionMatrixService,
        ITransactionManager transactionManager,
        ILogger<PositionMatrixChangeParentHandler> logger,
        IStatisticsService stats)
    {
        _positionMatrixRepository = positionMatrixRepository;
        _positionMatrixService = positionMatrixService;
        _transactionManager = transactionManager;
        _logger = logger;
        _stats = stats;
        _positionMatrixRepository = positionMatrixRepository;
    }
    
    public async Task<Result<bool, Errors>> ExecuteAsync(
    PositionMatrixChangeParentCommand command,
    CancellationToken cancellationToken = default)
    {
        //поиск изменяемой должности
        var resultPositionMatrix = await _positionMatrixRepository.GetByIdAsync(command.PositionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPositionMatrix.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска должности").ToErrors();
        }
        if (resultPositionMatrix.Value == null)
        {
            _logger.LogError("Record of PositionMatrix not found {PositionMatrixId}", command.PositionMatrixId);
            return GeneralErrors.NotFound(command.PositionMatrixId).ToErrors();
        }
        PositionMatrix position = resultPositionMatrix.Value;

        //поиск новой родительской должности
        var resultParentPositionMatrix = await _positionMatrixRepository.GetByIdAsync(command.NewParentPositionId, cancellationToken).ConfigureAwait(false);
        if (resultParentPositionMatrix.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска новой родительской должности").ToErrors();
        }
        if (resultParentPositionMatrix.Value == null)
        {
            _logger.LogError("Record of PositionMatrix not found {PositionMatrixId}", command.NewParentPositionId);
            return GeneralErrors.NotFound(command.NewParentPositionId).ToErrors();
        }
        PositionMatrix positionParent = resultParentPositionMatrix.Value;

        if (position.ParentId == positionParent.Id)
            return false;

        //проверка на зацикливание
        var resultFind = await _positionMatrixService.FindChildByIdAsync(position, positionParent.Id, cancellationToken).ConfigureAwait(false);
        if (resultFind.IsFailure)
        {
            _logger.LogError("Error PositionMatrixService.findChildByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска потомка с Parent.Id").ToErrors();
        }
        if (resultFind.Value != null)
        {
            _logger.LogError("Cicling hierarchy of PositionMatrix");
            return GeneralErrors.OtherError("зацикливание иерархии должностей").ToErrors();
        }

        //проверка на уникальность Slug у родителя
        var resultIsDuplicate = await _positionMatrixRepository.HasNameSlugAsync(
            position.Name.Value,
            position.Slug.Value,
            positionParent.Id.Value,
            position.Id.Value,
            cancellationToken).ConfigureAwait(false);
        if (resultIsDuplicate.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.HasNameSlugAsync");
            return GeneralErrors.Failure("ошибка запроса").ToErrors();
        }
        if (resultIsDuplicate.Value)
        {
            _logger.LogError("Duplicate record of PositionMatrix");
            return GeneralErrors.AlreadyExist().ToErrors();
        }

        var storePositionPathSlug = position.PathSlug;
        var storePositionParentId = position.ParentId?.Value;

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        if (position.Move(positionParent))
        {
            var resultUpdateRepos = await _positionMatrixRepository.UpdateAsync(position, cancellationToken).ConfigureAwait(false);
            if (resultUpdateRepos.IsFailure)
            {
                _logger.LogError("Error _positionMatrixRepository.UpdateAsync");
                var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
                if (rollbackResult.IsFailure)
                    return rollbackResult.Error.ToErrors();

                return resultUpdateRepos.Error.ToErrors();
            }
            var resultUpdate = await _positionMatrixService.UpdateAsync(position, positionDto: null, cancellationToken).ConfigureAwait(false);
            if (resultUpdate.IsFailure)
            {
                _logger.LogError("Error PositionMatrixService.UpdateAsync");
                var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
                if (rollbackResult.IsFailure)
                    return rollbackResult.Error.ToErrors();

                return resultUpdate.Error;
            }

            if (storePositionParentId != null)
                await _stats.CreateAsync(
                    position.Id.Value,
                    typeof(PositionMatrix).Name,
                    Statistica.Level.FINE,
                    Statistica.Action.DETACH,
                    $"Отсоединён от {storePositionPathSlug}",
                    storePositionParentId,
                    typeof(PositionMatrix).Name,
                    cancellationToken).ConfigureAwait(false);

            await _stats.CreateAsync(
                position.Id.Value,
                typeof(PositionMatrix).Name,
                Statistica.Level.FINE,
                Statistica.Action.ATTACH,
                $"Присоединён к {positionParent.PathSlugFull}",
                positionParent.Id.Value,
                typeof(PositionMatrix).Name,
                cancellationToken).ConfigureAwait(false);

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

            return resultUpdate.Value;
        }
        return true;
    }
}
