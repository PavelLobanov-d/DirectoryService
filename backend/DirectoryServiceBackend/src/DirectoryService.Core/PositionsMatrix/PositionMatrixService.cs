using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.PositionsMatrix;

public class PositionMatrixService : IPositionMatrixService
{
    private readonly IPositionMatrixRepository _positionMatrixRepository;
    private readonly CreatePositionMatrixValidator _createValidator;
    private readonly UpdatePositionMatrixValidator _updateValidator;
    private readonly ILogger<PositionMatrixService> _logger;
    private readonly IStatisticsService _stats;
    public PositionMatrixService(
        IPositionMatrixRepository positionMatrixRepository,
        CreatePositionMatrixValidator createValidator,
        UpdatePositionMatrixValidator updateValidator,
        IStatisticsService stats,
        ILogger<PositionMatrixService> logger)
    {
        _positionMatrixRepository = positionMatrixRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _stats = stats;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> CreateAsync(CreatePositionMatrixDto positionMatrixDto, CancellationToken cancellationToken = default)
    {
        //валидация входящих параметров
        ValidationResult validationResult = await _createValidator.ValidateAsync(positionMatrixDto, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            return new Errors(validationResult);
        }
        //валидация бизнес-правил
        //проверка на уникальность Slug у родителя
        var resultIsDuplicate = await _positionMatrixRepository.HasNameSlugAsync(
            positionMatrixDto.Name,
            positionMatrixDto.Slug,
            positionMatrixDto.ParentPositionMatrixId,
            excludeId: null,
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

        var resultPositionName = PositionName.Create(positionMatrixDto.Name);
        if (resultPositionName.IsFailure)
        {
            _logger.LogError("Error PositionName.Create");
            return GeneralErrors.ValueIsInvalid("название должности").ToErrors();
        }
        var resultSlug = Slug.Create(positionMatrixDto.Slug);
        if (resultSlug.IsFailure)
        {
            _logger.LogError("Error Slug.Create");
            return GeneralErrors.ValueIsInvalid("Slug").ToErrors();
        }

        PositionMatrix? parent = null;
        /*
        //проверка на единственную должность с нулевым родительским Id
        if (positionMatrixDto.ParentPositionMatrixId == null)
        {
            var resultCheck = await _positionMatrixRepository.GetByParentIdAsync(parentPositionMatrixId: null, cancellationToken).ConfigureAwait(false);
            if (resultCheck.IsFailure)
            {
                _logger.LogError("Error _positionMatrixRepository.GetByParentIdAsync");
                return GeneralErrors.Failure("ошибка запроса").ToErrors();
            }
            if(resultCheck.Value.Any())
            {
                _logger.LogError("Top PositionMatrix already exist");
                return GeneralErrors.AlreadyExist().ToErrors();
            }
        }
        */
        if (positionMatrixDto.ParentPositionMatrixId != null)
        {
            var resultParent = await _positionMatrixRepository.GetByIdAsync((Guid)positionMatrixDto.ParentPositionMatrixId, cancellationToken).ConfigureAwait(false);
            if (resultParent.IsFailure)
            {
                _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
                return GeneralErrors.Failure("ошибка запроса").ToErrors();
            }
            else if (resultParent.Value == null)
            {
                _logger.LogError("Record of PositionMatrix not found {1}", positionMatrixDto.ParentPositionMatrixId);
                return GeneralErrors.NotFound((Guid)positionMatrixDto.ParentPositionMatrixId).ToErrors();
            }
            parent = resultParent.Value;
        }

        PositionMatrix positionMatrix = PositionMatrix.Create(resultPositionName.Value, resultSlug.Value, parent);
        var resultAdd = await _positionMatrixRepository.AddAsync(positionMatrix, cancellationToken).ConfigureAwait(false);
        if (resultAdd.IsFailure)
        {
            _logger.LogError("Error creating record of PositionMatrix");
            return GeneralErrors.Failure("ошибка добавления должности").ToErrors();
        }
        _logger.LogInformation("PositionMatrix created with Id {1}", resultAdd.Value);

        await _stats.CreateAsync(
            resultAdd.Value,
            positionMatrix.GetType().Name,
            Statistica.Level.INFO,
            Statistica.Action.CREATE,
            $"Создание матричной должности {positionMatrix.Name}",
            cancellationToken).ConfigureAwait(false);

        if (parent != null)
        {
            await _stats.CreateAsync(
                resultAdd.Value,
                positionMatrix.GetType().Name,
                Statistica.Level.INFO,
                Statistica.Action.ATTACH,
                $"Родительская должность: {parent.Name.Value}",
                parent.Id.Value,
                parent.GetType().Name,
                cancellationToken).ConfigureAwait(false);
        }

        return resultAdd.Value;
    }
    public async Task<Result<bool, Error>> DeleteAsync(Guid positionMatrixId, CancellationToken cancellationToken = default)
    {
        var resultPosition = await _positionMatrixRepository.GetByIdAsync(positionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPosition.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска должности");
        }
        if (resultPosition.Value == null)
        {
            _logger.LogError("Record of PositionMatrix not found {1}", positionMatrixId);
            return GeneralErrors.NotFound(positionMatrixId);
        }
        var result = await DeleteAsync(resultPosition.Value, cancellationToken).ConfigureAwait(false);
        return result;
    }
    public async Task<Result<bool, Error>> DeleteAsync(PositionMatrix positionMatrix, CancellationToken cancellationToken = default)
    {
        //проверка на существование потомков
        var resultHasExists = await _positionMatrixRepository.GetByParentIdAsync(positionMatrix.Id.Value, cancellationToken).ConfigureAwait(false);
        if (resultHasExists.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByParentIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска должностей по родительской");
        }
        if(resultHasExists.Value.Any())
        {
            return Error.Conflict("rules.failure", "есть зависимые должности. Удаление запрещено");
        }

        var result = await _positionMatrixRepository.DeleteAsync(positionMatrix, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.DeleteAsync");
            return GeneralErrors.Failure("ошибка удаления должности");
        }
        if (result.Value)
        {
            await _stats.CreateAsync(
                positionMatrix.Id.Value,
                typeof(PositionMatrix).Name,
                Statistica.Level.INFO,
                Statistica.Action.DELETE,
                $"Удаление",
                positionMatrix.ParentId != null ? positionMatrix.ParentId.Value : null,
                positionMatrix.ParentId != null ? typeof(PositionMatrix).Name : null,
                cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Deleting of PositionMatrix {1}", positionMatrix.Id);
        }
        return result;
    }
    public async Task<Result<List<PositionMatrix>, Error>> GetAsync(SelectDto request, CancellationToken cancellationToken = default)
    {
        var resultPositions = await _positionMatrixRepository.GetAsync(request, cancellationToken).ConfigureAwait(false);
        if (resultPositions.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetAsync");
            return GeneralErrors.Failure("ошибка запроса поиска должностей");
        }
        return resultPositions.Value;
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
    public async Task<Result<List<PositionMatrix>, Error>> GetByParentIdAsync(Guid parentPositionMatrixId, CancellationToken cancellationToken = default)
    {
        var resultPositions = await _positionMatrixRepository.GetByParentIdAsync(parentPositionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPositions.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByParentIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска родительской должности");
        }
        return resultPositions.Value;

    }
    public async Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default)
    {
        var result = await _positionMatrixRepository.SaveAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }
    public async Task<Result<bool, Errors>> UpdateAsync(
    Guid positionMatrixId,
    UpdatePositionMatrixDto? positionDto,
    CancellationToken cancellationToken = default)
    {
        var resultPositionMatrix = await _positionMatrixRepository.GetByIdAsync(positionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPositionMatrix.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска должности").ToErrors();
        }
        if (resultPositionMatrix.Value == null)
        {
            _logger.LogError("Record of PositionMatrix not found {1}", positionMatrixId);
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
                    positionDto.NewName == null ? positionMatrix.Name.Value : positionDto.NewName,
                    positionDto.NewSlug == null ? positionMatrix.Slug.Value : positionDto.NewSlug,
                    positionMatrix.ParentId == null ? null : positionMatrix.ParentId.Value,
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
            if (positionDto.NewName != null && !positionMatrix.Name.Value.Equals(positionDto.NewName))
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
            if (positionDto.NewSlug != null && !positionMatrix.Slug.Value.Equals(positionDto.NewSlug))
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
                var resultPosition = await _positionMatrixRepository.GetByIdAsync(positionMatrix.ParentId.Value, cancellationToken).ConfigureAwait(false);
                if(resultPosition.IsFailure)
                {
                    _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
                    return GeneralErrors.Failure("ошибка поиска родительской должности").ToErrors();
                }
                if (resultPosition.Value != null)
                    positionMatrix.SetParent(resultPosition.Value);
                else
                {
                    _logger.LogError("Record of PositionMatrix not found {1}", positionMatrix.ParentId);
                    return GeneralErrors.NotFound(positionMatrix.ParentId.Value).ToErrors();
                }
            }
            if(positionMatrix.PathSlug != positionMatrix.Parent.PathSlugFull)
            {
                newPathSlug = positionMatrix.Parent.PathSlugFull;
            }
        }
        //потомки
        var resultChilds = await _positionMatrixRepository.GetByParentIdAsync(positionMatrix.Id.Value, cancellationToken).ConfigureAwait(false);
        if (resultChilds.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByParentIdAsync");
            return GeneralErrors.Failure("ошибка получения потомков").ToErrors();
        }
        else if (resultChilds.Value.Count > 0)
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
                await UpdateAsync(child, null, cancellationToken).ConfigureAwait(false);
            }
        }
        return true;
    }
    public async Task<Result<bool, Error>> ChangeParentAsync(
        Guid positionMatrixId,
        Guid newParentPositionId,
        CancellationToken cancellationToken = default)
    {
        //поиск изменяемой должности
        var resultPositionMatrix = await _positionMatrixRepository.GetByIdAsync(positionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPositionMatrix.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска должности");
        }
        if (resultPositionMatrix.Value == null)
        {
            _logger.LogError("Record of PositionMatrix not found {1}", positionMatrixId);
            return GeneralErrors.NotFound(positionMatrixId);
        }
        PositionMatrix position = resultPositionMatrix.Value;

        //поиск новой родительской должности
        var resultParentPositionMatrix = await _positionMatrixRepository.GetByIdAsync(newParentPositionId, cancellationToken).ConfigureAwait(false);
        if (resultParentPositionMatrix.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска новой родительской должности");
        }
        if (resultParentPositionMatrix.Value == null)
        {
            _logger.LogError("Record of PositionMatrix not found {1}", newParentPositionId);
            return GeneralErrors.NotFound(newParentPositionId);
        }
        PositionMatrix positionParent = resultParentPositionMatrix.Value;

        if (position.ParentId == positionParent.Id)
            return false;

        //проверка на зацикливание
        var resultFind = await FindChildByIdAsync(position, positionParent.Id, cancellationToken).ConfigureAwait(false);
        if (resultFind.IsFailure)
        {
            _logger.LogError("Error PositionMatrixService.findChildByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска потомка с Parent.Id");
        }
        if (resultFind.Value != null)
        {
            _logger.LogError("Cicling hierarchy of PositionMatrix");
            return GeneralErrors.OtherError("зацикливание иерархии должностей");
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
            return GeneralErrors.Failure("ошибка запроса");
        }
        if (resultIsDuplicate.Value)
        {
            _logger.LogError("Duplicate record of PositionMatrix");
            return GeneralErrors.AlreadyExist();
        }

        await _stats.CreateAsync(
            position.Id.Value,
            typeof(PositionMatrix).Name,
            Statistica.Level.FINE,
            Statistica.Action.DETACH,
            $"Отсоединён от {position.PathSlug}",
            position.ParentId.Value,
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

        if (position.Move(positionParent))
        {
            var resultUpdateRepos = await _positionMatrixRepository.UpdateAsync(position, cancellationToken).ConfigureAwait(false);
            if(resultUpdateRepos.IsFailure)
            {
                _logger.LogError("Error _positionMatrixRepository.UpdateAsync");
                return GeneralErrors.Failure("ошибка обновления должности");
            }
            var resultUpdate = await this.UpdateAsync(position, null, cancellationToken).ConfigureAwait(false);
            if (resultUpdate.IsFailure)
            {
                _logger.LogError("Error PositionMatrixService.UpdateAsync");
                return GeneralErrors.Failure("ошибка обновления должности");
            }
            return resultUpdate.Value;
        }
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
            return resultChilds.Error;

        return childFound;
    }
}