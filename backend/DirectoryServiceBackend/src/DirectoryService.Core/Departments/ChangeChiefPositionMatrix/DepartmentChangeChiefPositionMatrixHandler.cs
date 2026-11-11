using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.DepartmentChiefPositions;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.ChangeChiefPositionMatrix;

public class DepartmentChangeChiefPositionMatrixHandler : ICommandHandler<bool, DepartmentChangeChiefPositionMatrixCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly IPositionMatrixService _positionMatrixService;
    private readonly IDepartmentsChiefPositionRepository _departmentChiefPositionsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<DepartmentChangeChiefPositionMatrixHandler> _logger;
    private readonly IStatisticsService _stats;

    public DepartmentChangeChiefPositionMatrixHandler(
        IDepartmentsRepository departmentRepository,
        IPositionMatrixService positionMatrixService,
        IDepartmentsChiefPositionRepository departmentChiefPositionsRepository,
        ITransactionManager transactionManager,
        ILogger<DepartmentChangeChiefPositionMatrixHandler> logger,
        IStatisticsService stats)
    {
        _departmentChiefPositionsRepository = departmentChiefPositionsRepository;
        _departmentRepository = departmentRepository;
        _transactionManager = transactionManager;
        _logger = logger;
        _positionMatrixService = positionMatrixService;
        _stats = stats;
    }
    public async Task<Result<bool, Errors>> ExecuteAsync(
    DepartmentChangeChiefPositionMatrixCommand command,
    CancellationToken cancellationToken = default)
    {
        // 1. Поиск изменяемого департамента
        var departmentResult = await _departmentRepository.GetByIdAsync(command.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (departmentResult.IsFailure)
        {
            _logger.LogError("Failed to get department {DepartmentId}: {Error}", command.DepartmentId, departmentResult.Error);
            return GeneralErrors.Failure("Ошибка запроса поиска департамента").ToErrors();
        }
        if (departmentResult.Value is null)
            return GeneralErrors.NotFound(command.DepartmentId, nameof(Department)).ToErrors();

        var department = departmentResult.Value;

        // 2. Поиск новой должности руководителя
        var newPositionResult = await _positionMatrixService.GetByIdAsync(command.NewChiefPositionMatrixId, cancellationToken).ConfigureAwait(false);
        if (newPositionResult.IsFailure)
            return newPositionResult.Error.ToErrors();

        if (newPositionResult.Value is null)
            return GeneralErrors.NotFound(command.NewChiefPositionMatrixId, nameof(PositionMatrix)).ToErrors();

        var newChiefPositionMatrix = newPositionResult.Value;

        if (department.ChiefPositionMatrix.Id == newChiefPositionMatrix.Id)
            return false;

        // 3. Проверка иерархии должностей относительно родительского департамента
        var hierarchyCheckResult = await ValidateHierarchyAsync(department, newChiefPositionMatrix, cancellationToken).ConfigureAwait(false);
        if (hierarchyCheckResult.IsFailure)
            return hierarchyCheckResult.Error;

        // 4. Поиск текущей привязки начальника департамента
        var currentChiefPositionResult = await _departmentChiefPositionsRepository
            .GetByDepartmentIdAsync(department.Id.Value, cancellationToken).ConfigureAwait(false);

        if (currentChiefPositionResult.IsFailure)
        {
            _logger.LogError("Failed to get chief position for department {DepartmentId}: {Error}", department.Id.Value, currentChiefPositionResult.Error);
            return GeneralErrors.Failure("Ошибка поиска привязки должности начальника департамента").ToErrors();
        }
        if (currentChiefPositionResult.Value is null)
        {
            _logger.LogError("Record of DepartmentChiefPosition not found for department {DepartmentId}", department.Id.Value);
            return GeneralErrors.NotFound(department.Id.Value, nameof(DepartmentChiefPosition)).ToErrors();
        }

        // 5. Выполнение изменений в транзакции
        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        var deleteResult = await _departmentChiefPositionsRepository
            .DeleteAsync(currentChiefPositionResult.Value, cancellationToken).ConfigureAwait(false);

        if (deleteResult.IsFailure)
        {
            _logger.LogError("Failed to delete old chief position for department {DepartmentId}: {Error}", department.Id.Value, deleteResult.Error);
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return deleteResult.Error.ToErrors();
        }

        // Используем ОДИН экземпляр новой привязки
        var newDepartmentChiefPosition = DepartmentChiefPosition.Create(department.Id, newChiefPositionMatrix);

        var addResult = await _departmentChiefPositionsRepository.AddAsync(newDepartmentChiefPosition, cancellationToken).ConfigureAwait(false);
        if (addResult.IsFailure)
        {
            _logger.LogError("Failed to add new chief position for department {DepartmentId}: {Error}", department.Id.Value, addResult.Error);
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return addResult.Error.ToErrors();
        }

        department.Update(newName: null, newSlug: null, newDepartmentChiefPosition);

        await _stats.CreateAsync(
            objectId: department.Id.Value,
            typeof(Department).Name,
            Statistica.Level.FINE,
            Statistica.Action.UPDATE,
            $"Изменена должность начальника на {newChiefPositionMatrix.Name}",
            parentId: null,
            parentTypeName: null,
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

        return true;
    }

    private async Task<UnitResult<Errors>> ValidateHierarchyAsync(
        Department department,
        PositionMatrix newChiefPositionMatrix,
        CancellationToken cancellationToken)
    {
        if (department.ParentId is null && newChiefPositionMatrix.ParentId is not null)
            return GeneralErrors.OtherError("У головного департамента должен быть начальник высшего уровня").ToErrors();

        if (department.ParentId is not null && newChiefPositionMatrix.ParentId is null)
            return GeneralErrors.OtherError("У подчинённого департамента должен быть начальник, подчинённый начальнику родительского департамента").ToErrors();

        if (department.ParentId is null)
            return UnitResult.Success<Errors>();

        var parentDepartmentResult = await _departmentRepository.GetByIdAsync(department.ParentId.Value, cancellationToken).ConfigureAwait(false);
        if (parentDepartmentResult.IsFailure)
        {
            _logger.LogError("Failed to get parent department {ParentId}: {Error}", department.ParentId.Value, parentDepartmentResult.Error);
            return GeneralErrors.Failure("Ошибка запроса поиска родительского департамента").ToErrors();
        }
        if (parentDepartmentResult.Value is null)
            return GeneralErrors.NotFound(department.ParentId.Value, nameof(Department)).ToErrors();

        var parentChiefResult = await _departmentChiefPositionsRepository
            .GetByDepartmentIdAsync(parentDepartmentResult.Value.Id.Value, cancellationToken).ConfigureAwait(false);

        if (parentChiefResult.IsFailure)
        {
            _logger.LogError("Failed to get chief position for parent department {ParentId}: {Error}", parentDepartmentResult.Value.Id.Value, parentChiefResult.Error);
            return GeneralErrors.Failure("Ошибка поиска должности начальника родительского департамента").ToErrors();
        }
        if (parentChiefResult.Value is null)
        {
            _logger.LogError("Record of DepartmentChiefPosition not found for parent department {ParentId}", parentDepartmentResult.Value.Id.Value);
            return GeneralErrors.NotFound(parentDepartmentResult.Value.Id.Value, nameof(DepartmentChiefPosition)).ToErrors();
        }

        var parentPositionResult = await _positionMatrixService
            .GetByIdAsync(parentChiefResult.Value.PositionMatrixId.Value, cancellationToken).ConfigureAwait(false);

        if (parentPositionResult.IsFailure)
            return parentPositionResult.Error.ToErrors();

        if (parentPositionResult.Value is null)
        {
            _logger.LogError("Record of PositionMatrix not found {PositionMatrixId}", parentChiefResult.Value.PositionMatrixId.Value);
            return GeneralErrors.NotFound(parentChiefResult.Value.PositionMatrixId.Value, nameof(PositionMatrix)).ToErrors();
        }

        var childCheckResult = await _positionMatrixService
            .FindChildByIdAsync(parentPositionResult.Value, newChiefPositionMatrix.Id, cancellationToken).ConfigureAwait(false);

        if (childCheckResult.IsFailure)
            return childCheckResult.Error.ToErrors();

        if (childCheckResult.Value is null)
            return GeneralErrors.ValueIsInvalid("Должность начальника департамента должна быть зависима от должности начальника родительского департамента").ToErrors();

        return UnitResult.Success<Errors>();
    }
}