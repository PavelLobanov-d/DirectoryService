using CSharpFunctionalExtensions;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace DirectoryService.Core.PositionsMatrix.Update;

public class PositionMatrixUpdateHandler : ICommandHandler<bool, PositionMatrixUpdateCommand>
{
    private readonly IPositionMatrixRepository _positionMatrixRepository;
    private readonly UpdatePositionMatrixValidator _updateValidator;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<PositionMatrixUpdateHandler> _logger;
    private readonly IStatisticsService _stats;

    public PositionMatrixUpdateHandler(
        IPositionMatrixRepository positionMatrixRepository,
        UpdatePositionMatrixValidator updateValidator,
        ITransactionManager transactionManager,
        ILogger<PositionMatrixUpdateHandler> logger,
        IStatisticsService stats)
    {
        _positionMatrixRepository = positionMatrixRepository;
        _updateValidator = updateValidator;
        _transactionManager = transactionManager;
        _logger = logger;
        _stats = stats;
    }
    public async Task<Result<bool, Errors>> ExecuteAsync(
        PositionMatrixUpdateCommand command,
        CancellationToken cancellationToken = default)
    {
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

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        var result = await UpdateAsync(position, command.PositionDto, cancellationToken).ConfigureAwait(false);
        if(result.IsFailure)
        {
            _logger.LogError("Error UpdateAsync: {Message} ", result.Error.FirstOrDefault<Error>()?.Message);
            return result.Error;
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

        return result;
    }

    private async Task<Result<bool, Errors>> UpdateAsync(
        PositionMatrix positionMatrix,
        UpdatePositionMatrixDto? positionDto,
        CancellationToken cancellationToken = default)
    {
        bool result = false;
        PositionName? newName = null;
        Slug? newSlug = null;
        PathSlug? newPathSlug = null;

        if (positionDto != null)
        {
            //валидация входящих параметров
            var validationResult = await _updateValidator.ValidateAsync(positionDto, cancellationToken).ConfigureAwait(false);
            if (!validationResult.IsValid)
            {
                return new Errors(validationResult);
            }
            //валидация бизнес-правил
            if (positionDto.NewName != null || positionDto.NewSlug != null)
            {
                var resultIsDuplicate = await _positionMatrixRepository.HasNameSlugAsync(
                    positionDto.NewName ?? positionMatrix.Name.Value,
                    positionDto.NewSlug ?? positionMatrix.Slug.Value,
                    positionMatrix.ParentId?.Value,
                    positionMatrix.Id.Value,
                    cancellationToken).ConfigureAwait(false);

                if (resultIsDuplicate.IsFailure)
                {
                    _logger.LogError("Error _positionMatrixRepository.HasNameSlugAsync");
                    return GeneralErrors.Failure("ошибка запроса поиска дубликатов").ToErrors();
                }
                if (resultIsDuplicate.Value)
                {
                    return GeneralErrors.AlreadyExist().ToErrors();
                }
            }

            if (positionDto.NewName != null && !positionMatrix.Name.Value.Equals(positionDto.NewName, StringComparison.Ordinal))
            {
                var resultNewName = PositionName.Create(positionDto.NewName);
                if (resultNewName.IsSuccess)
                    newName = resultNewName.Value;
                await _stats.CreateAsync(
                    positionMatrix.Id.Value,
                    typeof(PositionMatrix).Name,
                    Statistica.Level.INFO,
                    Statistica.Action.UPDATE,
                    $"Название изменено с {positionMatrix.Name} на {newName}",
                    cancellationToken).ConfigureAwait(false);
            }
            if (positionDto.NewSlug != null && !positionMatrix.Slug.Value.Equals(positionDto.NewSlug, StringComparison.Ordinal))
            {
                var resultNewNewSlug = Slug.Create(positionDto.NewSlug);
                if (resultNewNewSlug.IsSuccess)
                    newSlug = resultNewNewSlug.Value;
                await _stats.CreateAsync(
                    positionMatrix.Id.Value,
                    typeof(PositionMatrix).Name,
                    Statistica.Level.FINE,
                    Statistica.Action.UPDATE,
                    $"Идентификатор изменен с {positionMatrix.Slug} на {newSlug}",
                cancellationToken).ConfigureAwait(false);
            }
        }

        if (positionMatrix.ParentId != null)
        {
            if (positionMatrix.Parent == null)
            {
                var resultPosition = await _positionMatrixRepository.GetByIdAsync(positionMatrix.ParentId.Value, cancellationToken).ConfigureAwait(false);
                if (resultPosition.IsFailure)
                {
                    _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
                    return resultPosition.Error.ToErrors();
                }
                if (resultPosition.Value != null)
                {
                    positionMatrix.SetParent(resultPosition.Value);
                }
                else
                {
                    _logger.LogError("Record of PositionMatrix not found {PositionMatrixId}", positionMatrix.ParentId);
                    return GeneralErrors.NotFound(positionMatrix.ParentId.Value).ToErrors();
                }
            }
            if (positionMatrix.PathSlug != positionMatrix.Parent?.PathSlugFull)
            {
                newPathSlug = positionMatrix.Parent?.PathSlugFull;
            }
        }
        //потомки
        var resultChilds = await _positionMatrixRepository.GetByParentIdAsync(positionMatrix.Id.Value, cancellationToken).ConfigureAwait(false);
        if (resultChilds.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByParentIdAsync");
            return resultChilds.Error.ToErrors();
        }
        if (resultChilds.Value.Count > 0)
        {
            positionMatrix.AddChilds(resultChilds.Value);
        }

        if (positionMatrix.Update(newName, newSlug, newPathSlug))
        {
            var resultPositions = await _positionMatrixRepository.UpdateAsync(positionMatrix, cancellationToken).ConfigureAwait(false);
            if (resultPositions.IsFailure)
            {
                _logger.LogError("Error _positionMatrixRepository.UpdateAsync");
                return resultPositions.Error.ToErrors();
            }
            result = resultPositions.Value;
        }

        if (!result)
        {
            _logger.LogInformation("PositionMatrixService.UpdateAsync обновление не требуется");
            return false;
        }

        //обновляем потомков
        if ((newSlug != null || newPathSlug != null) && positionMatrix.Childs.Count > 0)
        {
            foreach (PositionMatrix child in positionMatrix.Childs)
            {
                var resultChildUpdate = await UpdateAsync(child, positionDto: null, cancellationToken).ConfigureAwait(false);
                if(resultChildUpdate.IsFailure)
                {
                    _logger.LogError("Error _positionMatrixRepository.UpdateAsync");
                    return resultChildUpdate.Error;
                }
            }
        }
        return true;
    }
}
