using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace DirectoryService.Core.PositionsMatrix.Create;

public class PositionMatrixCreateHandler : ICommandHandler<CreatedObjectGuid, PositionMatrixCreateCommand>
{
    private readonly IPositionMatrixRepository _positionMatrixRepository;
    private readonly CreatePositionMatrixValidator _createValidator;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<PositionMatrixCreateHandler> _logger;
    private readonly IStatisticsService _stats;
    public PositionMatrixCreateHandler(
        IPositionMatrixRepository positionMatrixRepository,
        CreatePositionMatrixValidator createValidator,
        ITransactionManager transactionManager,
        ILogger<PositionMatrixCreateHandler> logger,
        IStatisticsService stats)
    {
        _positionMatrixRepository = positionMatrixRepository;
        _createValidator = createValidator;
        _transactionManager = transactionManager;
        _logger = logger;
        _stats = stats;
    }
    public async Task<Result<CreatedObjectGuid, Errors>> ExecuteAsync(PositionMatrixCreateCommand command, CancellationToken cancellationToken = default)
    {
        //валидация входящих параметров
        ValidationResult validationResult = await _createValidator.ValidateAsync(command.PositionMatrixDto, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            return new Errors(validationResult);
        }
        //валидация бизнес-правил
        //проверка на уникальность Slug у родителя
        var resultIsDuplicate = await _positionMatrixRepository.HasNameSlugAsync(
            command.PositionMatrixDto.Name,
            command.PositionMatrixDto.Slug,
            command.PositionMatrixDto.ParentPositionMatrixId,
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

        var resultPositionName = PositionName.Create(command.PositionMatrixDto.Name);
        if (resultPositionName.IsFailure)
        {
            _logger.LogError("Error PositionName.Create");
            return GeneralErrors.ValueIsInvalid("название должности").ToErrors();
        }
        var resultSlug = Slug.Create(command.PositionMatrixDto.Slug);
        if (resultSlug.IsFailure)
        {
            _logger.LogError("Error Slug.Create");
            return GeneralErrors.ValueIsInvalid("Slug").ToErrors();
        }

        PositionMatrix? parent = null;
        if (command.PositionMatrixDto.ParentPositionMatrixId != null)
        {
            var resultParent = await _positionMatrixRepository.GetByIdAsync((Guid)command.PositionMatrixDto.ParentPositionMatrixId, cancellationToken).ConfigureAwait(false);
            if (resultParent.IsFailure)
            {
                _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
                return GeneralErrors.Failure("ошибка запроса").ToErrors();
            }
            if (resultParent.Value == null)
            {
                _logger.LogError("Record of PositionMatrix not found {PositionMatrixId}", command.PositionMatrixDto.ParentPositionMatrixId);
                return GeneralErrors.NotFound((Guid)command.PositionMatrixDto.ParentPositionMatrixId).ToErrors();
            }
            parent = resultParent.Value;
        }

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        PositionMatrix positionMatrix = PositionMatrix.Create(resultPositionName.Value, resultSlug.Value, parent);
        var resultAdd = await _positionMatrixRepository.AddAsync(positionMatrix, cancellationToken).ConfigureAwait(false);
        if (resultAdd.IsFailure)
        {
            _logger.LogError("Error creating record of PositionMatrix");
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return resultAdd.Error.ToErrors();
        }
        _logger.LogInformation("PositionMatrix created with Id {PositionMatrixId}", resultAdd.Value);

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

        return new CreatedObjectGuid(resultAdd.Value);
    }
}
