using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;
using System.Reflection.Metadata.Ecma335;

namespace DirectoryService.Core.Departments.Update;
public class UpdateDepartmentHandler : ICommandHandler<bool, UpdateDepartmentCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly UpdateDepartmentValidator _updateValidator;
    private readonly ILogger<UpdateDepartmentHandler> _logger;
    private readonly IStatisticsService _stats;

    public UpdateDepartmentHandler(
        IDepartmentsRepository departmentRepository,
        ITransactionManager transactionManager,
        UpdateDepartmentValidator updateValidator,
        ILogger<UpdateDepartmentHandler> logger,
        IStatisticsService stats)
    {
        _departmentRepository = departmentRepository;
        _transactionManager = transactionManager;
        _updateValidator = updateValidator;
        _logger = logger;
        _stats = stats;
    }
    public async Task<Result<bool, Errors>> ExecuteAsync(
        UpdateDepartmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var resultDepartment = await _departmentRepository.GetByIdAsync(command.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (resultDepartment.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска департамента").ToErrors();
        }
        if (resultDepartment.Value == null)
        {
            _logger.LogError("Record of Department not found {DepartmentId}", command.DepartmentId);
            return GeneralErrors.NotFound(command.DepartmentId, "Department").ToErrors();
        }

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        Department department = resultDepartment.Value;
        var result = await UpdateAsync(department, command.DepartmentDto, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();
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
        Department department,
        UpdateDepartmentDto? departmentDto,
        CancellationToken cancellationToken = default)
    {
        bool result = false;
        DepartmentName? newName = null;
        Slug? newSlug = null;
        PathSlug? newPathSlug = null;

        if (departmentDto != null)
        {
            //валидация входящих параметров
            var validationResult = await _updateValidator.ValidateAsync(departmentDto, cancellationToken).ConfigureAwait(false);
            if (!validationResult.IsValid)
            {
                return new Errors(validationResult);
            }
            //валидация бизнес-правил
            if (departmentDto.NewName != null || departmentDto.NewSlug != null)
            {
                var resultIsDuplicate = await _departmentRepository.HasNameSlugAsync(
                    departmentDto.NewName ?? department.Name.Value,
                    departmentDto.NewSlug ?? department.Slug.Value,
                    department.ParentId?.Value,
                    department.Id.Value,
                    cancellationToken).ConfigureAwait(false);

                if (resultIsDuplicate.IsFailure)
                {
                    _logger.LogError("Error _departmentRepository.HasNameSlugAsync");
                    return resultIsDuplicate.Error.ToErrors();
                }
                if (resultIsDuplicate.Value)
                {
                    return GeneralErrors.AlreadyExist().ToErrors();
                }
            }
            if (departmentDto.NewName != null && !department.Name.Value.Equals(departmentDto.NewName, StringComparison.OrdinalIgnoreCase))
            {
                var resultNewName = DepartmentName.Create(departmentDto.NewName);
                if (resultNewName.IsSuccess)
                    newName = resultNewName.Value;
                await _stats.CreateAsync(
                    department.Id.Value,
                    typeof(Department).Name,
                    Statistica.Level.INFO,
                    Statistica.Action.UPDATE,
                    $"Название изменено с {department.Name} на {newName}",
                    cancellationToken).ConfigureAwait(false);
            }
            if (departmentDto.NewSlug != null && !department.Slug.Value.Equals(departmentDto.NewSlug, StringComparison.Ordinal))
            {
                var resultNewNewSlug = Slug.Create(departmentDto.NewSlug);
                if (resultNewNewSlug.IsSuccess)
                    newSlug = resultNewNewSlug.Value;
                await _stats.CreateAsync(
                    department.Id.Value,
                    typeof(Department).Name,
                    Statistica.Level.FINE,
                    Statistica.Action.UPDATE,
                    $"Идентификатор изменен с {department.Slug} на {newSlug}",
                cancellationToken).ConfigureAwait(false);
            }
        }

        if (department.ParentId != null)
        {
            if (department.Parent == null)
            {
                var resultDepartment = await _departmentRepository.GetByIdAsync(department.ParentId.Value, cancellationToken).ConfigureAwait(false);
                if (resultDepartment.IsFailure)
                {
                    _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
                    return GeneralErrors.Failure("ошибка поиска родительской должности").ToErrors();
                }
                if (resultDepartment.Value != null)
                {
                    department.SetParent(resultDepartment.Value);
                }
                else
                {
                    _logger.LogError("Record of Department not found {DepartmentId}", department.ParentId);
                    return GeneralErrors.NotFound(department.ParentId.Value, "Department").ToErrors();
                }
            }
            if (department.PathSlug != department.Parent?.PathSlugFull)
            {
                newPathSlug = department.Parent?.PathSlugFull;
            }
        }
        //потомки
        var resultChilds = await _departmentRepository.GetByParentIdAsync(department.Id.Value, cancellationToken).ConfigureAwait(false);
        if (resultChilds.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByParentIdAsync");
            return resultChilds.Error.ToErrors();
        }

        if (resultChilds.Value.Count > 0)
        {
            department.AddChilds(resultChilds.Value);
        }

        if (department.Update(newName, newSlug, newDepartmentChiefPosition: null))
        {
            var resultPositions = await _departmentRepository.UpdateAsync(department, cancellationToken).ConfigureAwait(false);
            if (resultPositions.IsFailure)
            {
                _logger.LogError("Error _departmentRepository.UpdateAsync");
                return resultPositions.Error.ToErrors();
            }
            result = resultPositions.Value;
        }

        if (!result)
        {
            _logger.LogInformation("DepartmentService.UpdateAsync обновление не требуется");
            return false;
        }

        //обновляем потомков
        if ((newSlug != null || newPathSlug != null) && department.Childs.Count > 0)
        {
            foreach (Department child in department.Childs)
            {
                var resultUpdateChild = await UpdateAsync(child, departmentDto: null, cancellationToken).ConfigureAwait(false);
                if (resultUpdateChild.IsFailure)
                    return resultUpdateChild.Error;
            }
        }

        return true;
    }
}
