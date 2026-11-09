using CSharpFunctionalExtensions;
using DirectoryService.Shared;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.Statistics;
using Microsoft.Extensions.Logging;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.Delete;

public class PositionMatrixDeleteHandler : ICommandHandler<bool, PositionMatrixDeleteCommand>
{
    private readonly IPositionMatrixRepository _positionMatrixRepository;
    private readonly IPositionMatrixService _positionMatrixService;
    private readonly ILogger<PositionMatrixDeleteHandler> _logger;
    private readonly IStatisticsService _stats;

    public PositionMatrixDeleteHandler(
        IPositionMatrixRepository positionMatrixRepository,
        IPositionMatrixService positionMatrixService,
        ILogger<PositionMatrixDeleteHandler> logger,
        IStatisticsService stats)
    {
        _positionMatrixRepository = positionMatrixRepository;
        _positionMatrixService = positionMatrixService;
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
            return GeneralErrors.NotFound(command.PositionMatrixId).ToErrors();
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

        var result = await _positionMatrixRepository.DeleteAsync(positionMatrix, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.DeleteAsync");
            return GeneralErrors.Failure("ошибка удаления должности").ToErrors();
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
        if (result.IsSuccess)
        {
            var resultSave = await _positionMatrixService.SaveAsync(cancellationToken).ConfigureAwait(false);
            if (resultSave.IsFailure)
                return resultSave.Error.ToErrors();
        }
        return true;
    }
}
