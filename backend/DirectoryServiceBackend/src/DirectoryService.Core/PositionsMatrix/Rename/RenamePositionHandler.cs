using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.PositionsMatrix.Rename;

public class RenamePositionHandler : ICommandHandler<bool, RenamePositionCommand>
{
    private readonly IPositionMatrixRepository _positionMatrixRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<RenamePositionHandler> _logger;
    private readonly IStatisticsService _stats;

    public RenamePositionHandler(
        IPositionMatrixRepository positionMatrixRepository,
        ITransactionManager transactionManager,
        ILogger<RenamePositionHandler> logger,
        IStatisticsService stats)
    {
        _positionMatrixRepository = positionMatrixRepository;
        _transactionManager = transactionManager;
        _logger = logger;
        _stats = stats;
    }

    public async Task<Result<bool, Errors>> ExecuteAsync(
        RenamePositionCommand command,
        CancellationToken cancellationToken = default)
    {
        var resultPosition = await _positionMatrixRepository
            .GetByIdAsync(command.PositionMatrixId, cancellationToken)
            .ConfigureAwait(false);
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

        PositionMatrix positionMatrix = resultPosition.Value;
        var resultName = PositionName.Create(command.PositionDto.NewName);
        if (resultName.IsFailure)
        {
            return Error.Validation("value.is.invalid", "наименование должности не может быть пустым", nameof(command.PositionDto.NewName)).ToErrors();
        }

        if (positionMatrix.Name == resultName.Value)
        {
            _logger.LogInformation("PositionMatrix {PositionMatrixId} already has requested name", command.PositionMatrixId);
            return true;
        }

        var resultIsDuplicate = await _positionMatrixRepository.HasNameSlugAsync(
            resultName.Value.Value,
            positionMatrix.Slug.Value,
            positionMatrix.ParentId?.Value,
            positionMatrix.Id.Value,
            cancellationToken).ConfigureAwait(false);
        if (resultIsDuplicate.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.HasNameSlugAsync");
            return GeneralErrors.Failure("ошибка проверки уникальности должности").ToErrors();
        }
        if (resultIsDuplicate.Value)
        {
            _logger.LogError("Duplicate name of PositionMatrix {PositionMatrixId}", command.PositionMatrixId);
            return Error.Conflict("record.already.exist", "должность с таким наименованием уже существует").ToErrors();
        }

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        positionMatrix.Update(resultName.Value, slug: null, pathSlug: null);
        var resultUpdate = await _positionMatrixRepository.UpdateAsync(positionMatrix, cancellationToken).ConfigureAwait(false);
        if (resultUpdate.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.UpdateAsync");
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return resultUpdate.Error.ToErrors();
        }

        await _stats.CreateAsync(
            positionMatrix.Id.Value,
            typeof(PositionMatrix).Name,
            Statistica.Level.INFO,
            Statistica.Action.UPDATE,
            $"Переименована должность в {positionMatrix.Name.Value}",
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

        _logger.LogInformation("PositionMatrix {PositionMatrixId} renamed to {PositionName}", positionMatrix.Id, positionMatrix.Name.Value);
        return resultUpdate.Value;
    }
}
