using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
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
    private readonly IDepartmentsService _departmentService;
    private readonly IPositionMatrixService _positionMatrixService;
    private readonly IDepartmentsChiefPositionRepository _departmentChiefPositionsRepository;
    private readonly ILogger<DepartmentChangeChiefPositionMatrixHandler> _logger;
    private readonly IStatisticsService _stats;

    public DepartmentChangeChiefPositionMatrixHandler(
        IDepartmentsRepository departmentRepository,
        IDepartmentsService departmentService,
        IPositionMatrixService positionMatrixService,
        IDepartmentsChiefPositionRepository departmentChiefPositionsRepository,
        ILogger<DepartmentChangeChiefPositionMatrixHandler> logger,
        IStatisticsService stats)
    {
        _departmentChiefPositionsRepository = departmentChiefPositionsRepository;
        _departmentRepository = departmentRepository;
        _departmentService = departmentService;
        _logger = logger;
        _positionMatrixService = positionMatrixService;
        _stats = stats;
    }
    public async Task<Result<bool, Errors>> ExecuteAsync(
        DepartmentChangeChiefPositionMatrixCommand command,
        CancellationToken cancellationToken = default)
    {
        //поиск изменяемого департамента
        var resultDepartment = await _departmentRepository.GetByIdAsync(command.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (resultDepartment.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска департамента").ToErrors();
        }
        if (resultDepartment.Value == null)
        {
            return GeneralErrors.NotFound(command.DepartmentId, "Department").ToErrors();
        }
        Department department = resultDepartment.Value;

        //поиск новой должности руководителя
        var resultChiefPositionMatrixId = await _positionMatrixService.GetByIdAsync(command.NewChiefPositionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultChiefPositionMatrixId.IsFailure)
            return resultChiefPositionMatrixId.Error.ToErrors();

        if (resultChiefPositionMatrixId.Value == null)
        {
            return GeneralErrors.NotFound(command.NewChiefPositionMatrixId, "PositionMatrix").ToErrors();
        }
        PositionMatrix newChiefPositionMatrix = resultChiefPositionMatrixId.Value;

        if (department.ChiefPositionMatrix.Id == newChiefPositionMatrix.Id)
            return false;

        //проверка на подчинённость должности начальника
        if (department.ParentId == null && newChiefPositionMatrix.ParentId != null)
        {
            return GeneralErrors.OtherError("у головного департамента должен быть начальник высшего уровня").ToErrors();
        }
        if (department.ParentId != null && newChiefPositionMatrix.ParentId == null)
        {
            return GeneralErrors.OtherError("у подчинённого департамента должен быть начальник, подчинённый начальнику родительского департамента").ToErrors();
        }

        if (department.ParentId != null)
        {
            var resultParentDepartment = await _departmentRepository.GetByIdAsync(department.ParentId.Value, cancellationToken).ConfigureAwait(false);
            if (resultParentDepartment.IsFailure)
            {
                _logger.LogError("Error _departmentRepository.GetByIdAsync");
                return GeneralErrors.Failure("ошибка запроса поиска департамента").ToErrors();
            }
            if (resultParentDepartment.Value == null)
            {
                return GeneralErrors.NotFound(department.ParentId.Value, "Department").ToErrors();
            }
            Department departmentParent = resultParentDepartment.Value;

            var resultChiefDepartamentParent = await _departmentChiefPositionsRepository.GetByDepartmentIdAsync(
                departmentParent.Id.Value,
                cancellationToken).ConfigureAwait(false);
            if (resultChiefDepartamentParent.IsFailure)
            {
                _logger.LogError("Error _departmentChiefPositionsRepository.GetByDepartmentIdAsync");
                return GeneralErrors.Failure("ошибка поиска должности начальника родительского департамента").ToErrors();
            }
            if (resultChiefDepartamentParent.Value == null)
            {
                _logger.LogError("Record of DepartmentChiefPositionMatrix not found {DepartmentId}", departmentParent.Id);
                return GeneralErrors.NotFound(departmentParent.Id.Value, "DepartmentChiefPositionMatrix").ToErrors();
            }
            DepartmentChiefPosition departamentChiefParent = resultChiefDepartamentParent.Value;
            var resultPositionChiefParent = await _positionMatrixService.GetByIdAsync(
                departamentChiefParent.PositionMatrixId.Value,
                cancellationToken).ConfigureAwait(false);
            if (resultPositionChiefParent.IsFailure)
                return resultPositionChiefParent.Error.ToErrors();
            if (resultPositionChiefParent.Value == null)
            {
                _logger.LogError("Record of PositionMatrix not found {PositionMatrixId}", departamentChiefParent.PositionMatrixId.Value);
                return GeneralErrors.NotFound(departmentParent.Id.Value, "DepartmentChiefPositionMatrix").ToErrors();
            }

            var resultCheck = await _positionMatrixService.FindChildByIdAsync(
            resultPositionChiefParent.Value,
            newChiefPositionMatrix.Id,
            cancellationToken).ConfigureAwait(false);
            if (resultCheck.IsFailure)
                return resultCheck.Error.ToErrors();
            if (resultCheck.Value == null)
                return GeneralErrors.ValueIsInvalid("должность начальника департамента должна быть зависима от должности начальника родительского департамента").ToErrors();
        }

        var resultDepartmentChiefPosition = await _departmentChiefPositionsRepository.GetByDepartmentIdAsync(
                department.Id.Value,
                cancellationToken).ConfigureAwait(false);
        if (resultDepartmentChiefPosition.IsFailure)
        {
            _logger.LogError("Error _departmentChiefPositionsRepository.GetByDepartmentIdAsync");
            return GeneralErrors.Failure("ошибка поиска привязки должности начальника департамента").ToErrors();
        }
        if (resultDepartmentChiefPosition.Value == null)
        {
            _logger.LogError("Record of DepartmentChiefPosition not found {DepartmentId}", department.Id.Value);
            return GeneralErrors.NotFound(department.Id.Value, "DepartmentChiefPosition").ToErrors();
        }

        var resultDelete = await _departmentChiefPositionsRepository.DeleteAsync(
                resultDepartmentChiefPosition.Value,
                cancellationToken).ConfigureAwait(false);
        if (resultDelete.IsFailure)
        {
            _logger.LogError("Error _departmentChiefPositionsRepository.DeleteAsync");
            return GeneralErrors.Failure("ошибка удаления привязки должности начальника департамента").ToErrors();
        }
        DepartmentChiefPosition newDCP = new(department, newChiefPositionMatrix);
        var resultAdd = await _departmentChiefPositionsRepository.AddAsync(newDCP, cancellationToken).ConfigureAwait(false);
        if (resultAdd.IsFailure)
        {
            _logger.LogError("Error _departmentChiefPositionsRepository.AddAsync");
            return GeneralErrors.Failure("ошибка создания привязки должности начальника департамента").ToErrors();
        }
        DepartmentChiefPosition newDepartmentChiefPosition = DepartmentChiefPosition.Create(department.Id, newChiefPositionMatrix);
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

        var resultSave = await _departmentService.SaveAsync(cancellationToken).ConfigureAwait(false);
        if (resultSave.IsFailure)
            return resultSave.Error.ToErrors();

        return true;
    }
}
