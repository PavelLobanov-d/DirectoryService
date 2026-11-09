using CSharpFunctionalExtensions;
using DirectoryService.Shared;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.Statistics;
using Microsoft.Extensions.Logging;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.ChangeParent;

public class PositionMatrixChangeParentHandler : ICommandHandler<bool, PositionMatrixChangeParentCommand>
{
    private readonly IPositionMatrixRepository _positionMatrixRepository;
    private readonly IPositionMatrixService _positionMatrixService;
    private readonly ILogger<PositionMatrixChangeParentHandler> _logger;
    private readonly IStatisticsService _stats;

    public PositionMatrixChangeParentHandler(
        IPositionMatrixRepository positionMatrixRepository,
        IPositionMatrixService positionMatrixService,
        ILogger<PositionMatrixChangeParentHandler> logger,
        IStatisticsService stats)
    {
        _positionMatrixRepository = positionMatrixRepository;
        _positionMatrixService = positionMatrixService;
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

        if (position.ParentId != null)
        {
            await _stats.CreateAsync(
                position.Id.Value,
                typeof(PositionMatrix).Name,
                Statistica.Level.FINE,
                Statistica.Action.DETACH,
                $"Отсоединён от {position.PathSlug}",
                position.ParentId.Value,
                typeof(PositionMatrix).Name,
                cancellationToken).ConfigureAwait(false);
        }

        await _stats.CreateAsync(
            position.Id.Value,
            typeof(PositionMatrix).Name,
            Statistica.Level.FINE,
            Statistica.Action.ATTACH,
            $"Присоединён к {positionParent.PathSlugFull}",
            positionParent.Id.Value,
            typeof(PositionMatrix).Name,
            cancellationToken).ConfigureAwait(false);

        if (position.Move(positionParent))
        {
            var resultUpdateRepos = await _positionMatrixRepository.UpdateAsync(position, cancellationToken).ConfigureAwait(false);
            if (resultUpdateRepos.IsFailure)
            {
                _logger.LogError("Error _positionMatrixRepository.UpdateAsync");
                return resultUpdateRepos.Error.ToErrors();
            }
            var resultUpdate = await _positionMatrixService.UpdateAsync(position, positionDto: null, cancellationToken).ConfigureAwait(false);
            if (resultUpdate.IsFailure)
            {
                _logger.LogError("Error PositionMatrixService.UpdateAsync");
                return GeneralErrors.Failure("ошибка обновления должности").ToErrors();
            }
            var resultSave = await _positionMatrixService.SaveAsync(cancellationToken).ConfigureAwait(false);
            if (resultSave.IsFailure)
                return resultSave.Error.ToErrors();

            return resultUpdate.Value;
        }
        return true;
    }
}
