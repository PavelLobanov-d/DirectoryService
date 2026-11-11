using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.DepartmentPositions;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.DetachPosition;

public class DepartmentDetachPositionHandler : ICommandHandler<bool, DepartmentDetachPositionCommand>
{
    private readonly IDepartmentPositionsRepository _departmentPositionsRepository;
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly IPositionMatrixService _positionMatrixService;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<DepartmentDetachPositionHandler> _logger;
    private readonly IStatisticsService _stats;

    public DepartmentDetachPositionHandler(
        IDepartmentPositionsRepository departmentPositionsRepository,
        IDepartmentsRepository departmentRepository,
        IPositionMatrixService positionMatrixService,
        ITransactionManager transactionManager,
        ILogger<DepartmentDetachPositionHandler> logger,
        IStatisticsService stats)
    {
        _departmentPositionsRepository = departmentPositionsRepository;
        _departmentRepository = departmentRepository;
        _positionMatrixService = positionMatrixService;
        _transactionManager = transactionManager;
        _logger = logger;
        _stats = stats;
    }

    public async Task<Result<bool, Errors>> ExecuteAsync(
        DepartmentDetachPositionCommand command,
        CancellationToken cancellationToken = default)
    {
        var resultLink = await _departmentPositionsRepository
            .GetByDepartmentAndPositionAsync(command.DepartmentId, command.PositionMatrixId, cancellationToken)
            .ConfigureAwait(false);
        if (resultLink.IsFailure)
        {
            _logger.LogError("Error _departmentPositionsRepository.GetByDepartmentAndPositionAsync");
            return GeneralErrors.Failure("ошибка запроса поиска связи подразделение-должность").ToErrors();
        }

        DepartmentPosition? link = resultLink.Value;
        if (link == null)
        {
            return await BuildNotFoundErrorAsync(command, cancellationToken).ConfigureAwait(false);
        }

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        var resultDelete = await _departmentPositionsRepository.DeleteAsync(link, cancellationToken).ConfigureAwait(false);
        if (resultDelete.IsFailure)
        {
            _logger.LogError("Error _departmentPositionsRepository.DeleteAsync");
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return GeneralErrors.Failure("ошибка удаления связи подразделение-должность").ToErrors();
        }

        if (resultDelete.Value)
        {
            await _stats.CreateAsync(
                link.PositionMatrixId.Value,
                typeof(PositionMatrix).Name,
                Statistica.Level.INFO,
                Statistica.Action.DETACH,
                $"Отсоединена должность {link.PositionMatrix.Name.Value}",
                link.DepartmentId.Value,
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

        _logger.LogInformation(
            "PositionMatrix {PositionMatrixId} detached from Department {DepartmentId}",
            command.PositionMatrixId,
            command.DepartmentId);
        return resultDelete.Value;
    }

    private async Task<Result<bool, Errors>> BuildNotFoundErrorAsync(
        DepartmentDetachPositionCommand command,
        CancellationToken cancellationToken)
    {
        var resultDepartment = await _departmentRepository.GetByIdAsync(command.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (resultDepartment.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска подразделения").ToErrors();
        }
        if (resultDepartment.Value == null)
        {
            return GeneralErrors.NotFound(command.DepartmentId, "Department").ToErrors();
        }

        var resultPosition = await _positionMatrixService.GetByIdAsync(command.PositionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPosition.IsFailure)
        {
            return resultPosition.Error.ToErrors();
        }
        if (resultPosition.Value == null)
        {
            return GeneralErrors.NotFound(command.PositionMatrixId, "Position").ToErrors();
        }

        return Error.NotFound(
            "record.not.found",
            $"связь подразделения {resultDepartment.Value.Name.Value} и должности {resultPosition.Value.Name.Value} не найдена").ToErrors();
    }
}
