using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.PositionsMatrix.Create;
using DirectoryService.Core.PositionsMatrix.GetById;
using DirectoryService.Core.PositionsMatrix.Update;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

namespace DirectoryService.Core.PositionsMatrix;
public class PositionMatrixService : IPositionMatrixService
{
    private readonly IPositionMatrixRepository _positionMatrixRepository;
    private readonly UpdatePositionMatrixValidator _updateValidator;
    private readonly ILogger<PositionMatrixService> _logger;
    private readonly IStatisticsService _stats;
    public PositionMatrixService(
        IPositionMatrixRepository positionMatrixRepository,
        UpdatePositionMatrixValidator updateValidator,
        IStatisticsService stats,
        ILogger<PositionMatrixService> logger)
    {
        _positionMatrixRepository = positionMatrixRepository;
        _updateValidator = updateValidator;
        _stats = stats;
        _logger = logger;
    }

    public async Task<Result<PositionMatrix?, Error>> GetByIdAsync(Guid positionMatrixId, CancellationToken cancellationToken = default)
    {
        var resultPosition = await _positionMatrixRepository.GetByIdAsync(positionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPosition.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска должностей");
        }
        return resultPosition.Value;
    }

    private async Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default)
    {
        var result = await _positionMatrixRepository.SaveAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }
    public async Task<Result<bool, Errors>> UpdateAsync(
    Guid positionMatrixId,
    UpdatePositionMatrixDto? positionDto,
    CancellationToken cancellationToken = default)
    {
        var resultPositionMatrix = await GetByIdAsync(positionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPositionMatrix.IsFailure)
            return resultPositionMatrix.Error.ToErrors();

        if (resultPositionMatrix.Value == null)
        {
            _logger.LogError("Record of PositionMatrix not found {PositionMatrixId}", positionMatrixId);
            return GeneralErrors.NotFound(positionMatrixId).ToErrors();
        }
        PositionMatrix position = resultPositionMatrix.Value;
        return await UpdateAsync(position, positionDto, cancellationToken).ConfigureAwait(false);
    }
    public async Task<Result<bool, Errors>> UpdateAsync(
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

        if(positionMatrix.ParentId != null)
        {
            if (positionMatrix.Parent == null)
            {
                var resultPosition = await GetByIdAsync(positionMatrix.ParentId.Value, cancellationToken).ConfigureAwait(false);
                if(resultPosition.IsFailure)
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
            if(positionMatrix.PathSlug != positionMatrix.Parent?.PathSlugFull)
            {
                newPathSlug = positionMatrix.Parent?.PathSlugFull;
            }
        }
        //потомки
        var resultChilds = await _positionMatrixRepository.GetByParentIdAsync(positionMatrix.Id.Value, cancellationToken).ConfigureAwait(false);
        if (resultChilds.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByParentIdAsync");
            return GeneralErrors.Failure("ошибка получения потомков").ToErrors();
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
                return GeneralErrors.Failure("ошибка обновления должности").ToErrors();
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
                await UpdateAsync(child, positionDto: null, cancellationToken).ConfigureAwait(false);
            }
        }

        var resultSave = await SaveAsync(cancellationToken).ConfigureAwait(false);
        if (resultSave.IsFailure)
            return resultSave.Error.ToErrors();

        return true;
    }
    /// <summary>
    /// найти потомка с id = findPositionId
    /// </summary>
    /// <param name="positionMatrix"></param>
    /// <param name="findPositionId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<Result<PositionMatrix?, Error>> FindChildByIdAsync(
        PositionMatrix positionMatrix,
        PositionMatrixId findPositionId,
        CancellationToken cancellationToken = default)
    {
        PositionMatrix? childFound = null;
        var resultChilds = await _positionMatrixRepository.GetByParentIdAsync(positionMatrix.Id.Value, cancellationToken).ConfigureAwait(false);
        if (resultChilds.IsSuccess)
        {
            foreach (PositionMatrix child in resultChilds.Value)
            {
                if (child.Id == findPositionId)
                {
                    childFound = child;
                    break;
                }
                else
                {
                    var resultFind = await FindChildByIdAsync(child, findPositionId, cancellationToken).ConfigureAwait(false);
                    if (resultFind.IsSuccess && resultFind.Value != null)
                    {
                        childFound = resultFind.Value;
                        break;
                    }
                }
            }
        }
        else
        {
            return resultChilds.Error;
        }

        return childFound;
    }
}