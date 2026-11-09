using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.DepartmentLocations;
using DirectoryService.Core.Departments.Delete;
using DirectoryService.Core.Locations;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.DetachLocation;

public class DepartmentDetachLocationHandler : ICommandHandler<bool, DepartmentDetachLocationCommand>
{
    private readonly IDepartmentLocationsRepository _departmentLocationsRepository;
    private readonly IDepartmentsService _departmentService;
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly ILocationsService _locationsService;
    private readonly ILogger<DepartmentDetachLocationHandler> _logger;
    private readonly IStatisticsService _stats;

    public DepartmentDetachLocationHandler(
        IDepartmentLocationsRepository departmentLocationsRepository,
        IDepartmentsService departmentService,
        IDepartmentsRepository departmentRepository,
        ILocationsService locationsService,
        ILogger<DepartmentDetachLocationHandler> logger,
        IStatisticsService stats)
    {
        _departmentLocationsRepository = departmentLocationsRepository;
        _departmentService = departmentService;
        _departmentRepository = departmentRepository;
        _locationsService = locationsService;
        _logger = logger;
        _stats = stats;
    }
    public async Task<Result<bool, Errors>> ExecuteAsync(
       DepartmentDetachLocationCommand command,
       CancellationToken cancellationToken = default)
    {
        //поиск линка
        var resultLocations = await _departmentLocationsRepository.GetLocationsByDepartmentIdAsync(command.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (resultLocations.IsFailure)
        {
            _logger.LogError("Error _departmentLocationsRepository.GetLocationsByDepartmentIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска связанных локаций").ToErrors();
        }
        DepartmentLocation? link = resultLocations.Value?.SingleOrDefault(v => v.LocationId.Value == command.LocationId);

        if (link == null)
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
                return GeneralErrors.NotFound(command.DepartmentId, "DepartmentId").ToErrors();
            }
            //поиск локации
            var resultLocation = await _locationsService.GetByIdAsync(command.LocationId, cancellationToken).ConfigureAwait(false);
            if (resultLocation.IsFailure)
                return resultLocation.Error.ToErrors();
            if (resultLocation.Value == null)
            {
                return GeneralErrors.NotFound(command.LocationId, "Location").ToErrors();
            }
            return GeneralErrors.ValueIsRequired($"Нет связи департамента {resultDepartment.Value.Name} и локации {resultLocation.Value.Name}").ToErrors();
        }

        Department department = link.Department;
        Location location = link.Location;

        department.DetachLocation(location);

        var resultDelete = await _departmentLocationsRepository.DeleteAsync(link, cancellationToken).ConfigureAwait(false);
        if (resultDelete.IsFailure)
        {
            _logger.LogError("Error _departmentLocationsRepository.DeleteAsync");
            return GeneralErrors.Failure("ошибка удаления связи департамент-локация").ToErrors();
        }
        if (resultDelete.Value)
        {
            await _stats.CreateAsync(
                department.Id.Value,
                department.GetType().Name,
                Statistica.Level.INFO,
                Statistica.Action.DETACH,
                $"Отсоединена локация {location.Name.Value}",
                location.Id.Value,
                location.GetType().Name,
            cancellationToken).ConfigureAwait(false);
        }
        if (resultDelete.IsSuccess)
        {
            var resultSave = await _departmentService.SaveAsync(cancellationToken).ConfigureAwait(false);
            if (resultSave.IsFailure)
                return resultSave.Error.ToErrors();
        }
        return resultDelete.Value;
    }
}
