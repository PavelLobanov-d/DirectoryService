using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Departments.ChangeChiefPositionMatrix;
using DirectoryService.Core.Departments.Update;
using DirectoryService.Core.Statistics;
using DirectoryService.Core.Validation;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.ChangeParent;
public class ChangeParentDepartmentHandler : ICommandHandler<bool, ChangeParentDepartmentCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly IDepartmentsService _departmentService;

    private readonly ILogger<ChangeParentDepartmentHandler> _logger;
    private readonly IStatisticsService _stats;

    public ChangeParentDepartmentHandler(
        IDepartmentsRepository departmentRepository,
        IDepartmentsService departmentService,
        ILogger<ChangeParentDepartmentHandler> logger,
        IStatisticsService stats)
    {
        _departmentService = departmentService;
        _departmentRepository = departmentRepository;
        _logger = logger;
        _stats = stats;
    }

    public async Task<Result<bool, Errors>> ExecuteAsync(
    ChangeParentDepartmentCommand command,
    CancellationToken cancellationToken = default)
    {
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

        if (department.Id == departmentParent.Id)
        {
            return GeneralErrors.Conflict("parent.himself", "Попытка подчинить самому себе").ToErrors();
        }

        //проверка на зацикливание
        var resultFind = await findChildByIdAsync(department, departmentParent.Id, cancellationToken).ConfigureAwait(false);
        if (resultFind.IsSuccess && resultFind.Value != null)
        {
            return GeneralErrors.Conflict("hierarchy.cicling", "зацикливание иерархии департаментов").ToErrors();
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

        await _stats.CreateAsync(
            department.Id.Value,
            typeof(Department).Name,
            Statistica.Level.FINE,
            Statistica.Action.DETACH,
            $"Отсоединён от {department.PathSlug}",
            department.ParentId?.Value,
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

        if (department.Move(departmentParent))
        {
            Result<bool, Errors> resultUpdate = await _departmentService.UpdateAsync(
                department.Id.Value,
                departmentDto : null,
                cancellationToken).ConfigureAwait(false);
            if (resultUpdate.IsFailure)
                return resultUpdate.Error;

            var resultSave = await _departmentService.SaveAsync(cancellationToken).ConfigureAwait(false);
            if (resultSave.IsFailure)
                return resultSave.Error.ToErrors();
            return true;
        }
        return false;
    }
    private async Task<Result<Department?, Errors>> findChildByIdAsync(
    Department department,
    DepartmentId findDepartmentId,
    CancellationToken cancellationToken = default)
    {
        Department? childFinded = null;
        var resultChilds = await _departmentRepository.GetByParentIdAsync(department.Id.Value, cancellationToken).ConfigureAwait(false);
        if (resultChilds.IsSuccess)
        {
            foreach (Department child in resultChilds.Value)
            {
                if (child.Id == findDepartmentId)
                {
                    childFinded = child;
                    break;
                }
                else
                {
                    var resultFind = await findChildByIdAsync(child, findDepartmentId, cancellationToken).ConfigureAwait(false);
                    if (resultFind.IsSuccess && resultFind.Value != null)
                    {
                        childFinded = resultFind.Value;
                        break;
                    }
                }
            }
        }
        return childFinded;
    }
}
