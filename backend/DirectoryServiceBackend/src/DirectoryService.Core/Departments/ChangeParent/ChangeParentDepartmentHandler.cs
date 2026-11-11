using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.ChangeParent;
public class ChangeParentDepartmentHandler : ICommandHandler<bool, ChangeParentDepartmentCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly IDepartmentsService _departmentService;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<ChangeParentDepartmentHandler> _logger;
    private readonly IStatisticsService _stats;

    public ChangeParentDepartmentHandler(
        IDepartmentsRepository departmentRepository,
        IDepartmentsService departmentService,
        ITransactionManager transactionManager,
        ILogger<ChangeParentDepartmentHandler> logger,
        IStatisticsService stats)
    {
        _departmentService = departmentService;
        _departmentRepository = departmentRepository;
        _transactionManager = transactionManager;
        _logger = logger;
        _stats = stats;
    }

    public async Task<Result<bool, Errors>> ExecuteAsync(
    ChangeParentDepartmentCommand command,
    CancellationToken cancellationToken = default)
    {
        if (command.DepartmentId == command.NewParentDepartmentId)
        {
            return GeneralErrors.Conflict("parent.himself", "Попытка подчинить самому себе").ToErrors();
        }

        //поиск изменяемого департамента
        var resultDepartment = await _departmentRepository.GetByIdAsync(command.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (resultDepartment.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса поиска департамента").ToErrors();
        }
        if (resultDepartment.Value == null)
        {
            return GeneralErrors.NotFound(command.DepartmentId, "Department").ToErrors();
        }
        Department department = resultDepartment.Value;

        //поиск нового родительского департамента
        var resultParentDepartment = await _departmentRepository.GetByIdAsync(command.NewParentDepartmentId, cancellationToken).ConfigureAwait(false);
        if (resultParentDepartment.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса поиска нового родительского департамента").ToErrors();
        }
        if (resultParentDepartment.Value == null)
        {
            return GeneralErrors.NotFound(command.NewParentDepartmentId, "Department").ToErrors();
        }
        Department departmentParent = resultParentDepartment.Value;

        if (department.ParentId == departmentParent.Id)
            return false;

        //проверка на зацикливание
        var resultFind = await _departmentService.FindChildByIdAsync(department, departmentParent.Id, cancellationToken).ConfigureAwait(false);
        if (resultFind.IsFailure)
        {
            _logger.LogError("Request error");
            return resultFind.Error;
        }
        if (resultFind.Value != null)
        {
            return GeneralErrors.Conflict("hierarchy.cycling", "зацикливание иерархии департаментов").ToErrors();
        }

        //проверка на уникальность Slug у родителя
        var resultIsDuplicate = await _departmentRepository.HasNameSlugAsync(
            department.Name.Value,
            department.Slug.Value,
            departmentParent.Id.Value,
            department.Id.Value,
            cancellationToken).ConfigureAwait(false);
        if (resultIsDuplicate.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса").ToErrors();
        }
        if (resultIsDuplicate.Value)
        {
            _logger.LogError("Duplicate record of Department");
            return GeneralErrors.AlreadyExist().ToErrors();
        }

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        var storeDepartmentPathSlug = department.PathSlug;
        var storeDepartmentParentId = department.ParentId?.Value ;

        if (department.Move(departmentParent))
        {
            Result<bool, Errors> resultUpdate = await _departmentService.UpdateAsync(
                department.Id.Value,
                departmentDto: null,
                cancellationToken).ConfigureAwait(false);
            if (resultUpdate.IsFailure)
                return resultUpdate.Error;

            if (storeDepartmentParentId != null)
                await _stats.CreateAsync(
                    department.Id.Value,
                    typeof(Department).Name,
                    Statistica.Level.FINE,
                    Statistica.Action.DETACH,
                    $"Отсоединён от {storeDepartmentPathSlug}",
                    storeDepartmentParentId,
                    typeof(Department).Name,
                    cancellationToken).ConfigureAwait(false);

            await _stats.CreateAsync(
                department.Id.Value,
                typeof(Department).Name,
                Statistica.Level.FINE,
                Statistica.Action.ATTACH,
                $"Присоединён к {departmentParent.PathSlugFull}",
                departmentParent.Id.Value,
                typeof(Department).Name,
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
        else
        {
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return false;
        }        
    }    
}
