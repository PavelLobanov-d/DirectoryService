using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.DepartmentChiefPositions;
using DirectoryService.Core.Departments.LinkLocation;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.LinkPosition;

public class DepartmentLinkPositionHandler : ICommandHandler<CreatedObjectGuid, DepartmentLinkPositionCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly IDepartmentsService _departmentService;
    private readonly IPositionMatrixService _positionMatrixService;
    private readonly IDepartmentsChiefPositionRepository _departmentChiefPositionsRepository;
    private readonly ILogger<DepartmentLinkPositionHandler> _logger;
    private readonly IStatisticsService _stats;

    public DepartmentLinkPositionHandler(
        IDepartmentsRepository departmentRepository,
        IDepartmentsService departmentService,
        IPositionMatrixService positionMatrixService,
        IDepartmentsChiefPositionRepository departmentChiefPositionsRepository,
        ILogger<DepartmentLinkPositionHandler> logger,
        IStatisticsService stats)
    {
        _departmentChiefPositionsRepository = departmentChiefPositionsRepository;
        _departmentRepository = departmentRepository;
        _departmentService = departmentService;
        _positionMatrixService = positionMatrixService;
        _logger = logger;
        _stats = stats;
    }
    public async Task<Result<CreatedObjectGuid, Errors>> ExecuteAsync(
        DepartmentLinkPositionCommand command,
        CancellationToken cancellationToken = default)
    {
        //поиск департамента
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

        //поиск присоединяемой должности
        var resultPositionMatrix = await _positionMatrixService.GetByIdAsync(command.PositionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPositionMatrix.IsFailure)
            return resultPositionMatrix.Error.ToErrors();

        if (resultPositionMatrix.Value == null)
        {
            return GeneralErrors.NotFound(command.PositionMatrixId, "PositionMatrix").ToErrors();
        }
        PositionMatrix positionMatrix = resultPositionMatrix.Value;

        //поиск должности начальника
        var resultChiefDepartament = await _departmentChiefPositionsRepository.GetByDepartmentIdAsync(
            department.Id.Value,
            cancellationToken).ConfigureAwait(false);
        if (resultChiefDepartament.IsFailure)
        {
            _logger.LogError("Error _departmentChiefPositionsRepository.GetByDepartmentIdAsync");
            return GeneralErrors.Failure("ошибка поиска должности начальника департамента").ToErrors();
        }
        if (resultChiefDepartament.Value == null)
        {
            _logger.LogError("Record of DepartmentChiefPositionMatrix not found {DepartmentId}", department.Id);
            return GeneralErrors.NotFound(department.Id.Value, "DepartmentChiefPosition").ToErrors();
        }
        DepartmentChiefPosition departamentChief = resultChiefDepartament.Value;

        //проверка на подчинённость должности начальника
        if (positionMatrix.ParentId == null)
            return GeneralErrors.ValueIsInvalid("нельзя прикрепить должность высшего уровня").ToErrors();

        var resultChild = await _positionMatrixService.FindChildByIdAsync(departamentChief.PositionMatrix, positionMatrix.Id, cancellationToken).ConfigureAwait(false);
        if (resultChild.IsFailure)
            return resultChild.Error.ToErrors();
        if (resultChild.Value == null)
            return GeneralErrors.ValueIsInvalid("прикрепляемая должность должна быть зависима от должности начальника департамента").ToErrors();

        DepartmentPosition link;
        try
        {
            link = department.LinkPosition(positionMatrix);
        }
        catch (DSException ex)
        {
            return GeneralErrors.OtherError(ex.Message).ToErrors();
        }

        await _stats.CreateAsync(
            positionMatrix.Id.Value,
            positionMatrix.GetType().Name,
            Statistica.Level.INFO,
            Statistica.Action.ATTACH,
            $"Присоединена должность {positionMatrix.Name.Value}",
            department.Id.Value,
            typeof(Department).Name,
            cancellationToken).ConfigureAwait(false);

        var resultSave = await _departmentService.SaveAsync(cancellationToken).ConfigureAwait(false);
        if (resultSave.IsFailure)
            return resultSave.Error.ToErrors();

        return new CreatedObjectGuid(link.Id.Value);
    }
}
