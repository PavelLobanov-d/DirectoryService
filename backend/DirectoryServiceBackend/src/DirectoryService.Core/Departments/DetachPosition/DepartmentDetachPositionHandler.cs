using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.DepartmentPositions;
using DirectoryService.Core.Departments.DetachLocation;
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
    private readonly IDepartmentsService _departmentService;
    private readonly ILogger<DepartmentsService> _logger;
    private readonly IStatisticsService _stats;

    public DepartmentDetachPositionHandler(
        IDepartmentPositionsRepository departmentPositionsRepository,
        IDepartmentsService departmentService,
        ILogger<DepartmentsService> logger,
        IStatisticsService stats)
    {
        _departmentPositionsRepository = departmentPositionsRepository;
        _departmentService = departmentService;
        _logger = logger;
        _stats = stats;
    }
    public async Task<Result<bool, Errors>> ExecuteAsync(
    DepartmentDetachPositionCommand command,
    CancellationToken cancellationToken = default)
    {
        //поиск линка
        var resultLink = await _departmentPositionsRepository.GetByIdAsync(command.DepartmentPositionId, cancellationToken).ConfigureAwait(false);
        if (resultLink.IsFailure)
        {
            _logger.LogError("Error _departmentPositionsRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска связи департамент-должность").ToErrors();
        }
        if (resultLink.Value == null)
        {
            return GeneralErrors.NotFound(command.DepartmentPositionId, "DepartmentPosition").ToErrors();
        }
        DepartmentPosition dp = resultLink.Value;

        var resultDelete = await _departmentPositionsRepository.DeleteAsync(dp, cancellationToken).ConfigureAwait(false);
        if (resultDelete.IsFailure)
        {
            _logger.LogError("Error _departmentPositionsRepository.DeleteAsync");
            return GeneralErrors.Failure("ошибка удаления связи департамент-должность").ToErrors();
        }
        if (resultDelete.Value)
        {
            await _stats.CreateAsync(
            dp.PositionMatrixId.Value,
            typeof(PositionMatrix).Name,
            Statistica.Level.INFO,
            Statistica.Action.DETACH,
            $"Отсоединена должность {resultLink.Value.PositionMatrix.Name.Value}",
            dp.DepartmentId.Value,
            typeof(Department).Name,
            cancellationToken).ConfigureAwait(false);
        }
        var resultSave = await _departmentService.SaveAsync(cancellationToken).ConfigureAwait(false);
        if (resultSave.IsFailure)
            return resultSave.Error.ToErrors();

        return resultDelete.Value;
    }
}
