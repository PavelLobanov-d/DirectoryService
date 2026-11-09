using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.DepartmentLocations;
using DirectoryService.Core.Departments.GetByParentId;
using DirectoryService.Core.Locations;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.LinkLocation;

public class DepartmentLinkLocationHandler : ICommandHandler<CreatedObjectGuid, DepartmentLinkLocationCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly IDepartmentsService _departmentService;
    private readonly ILocationsService _locationsService;
    private readonly IDepartmentLocationsRepository _departmentLocationsRepository;

    private readonly ILogger<DepartmentLinkLocationHandler> _logger;
    private readonly IStatisticsService _stats;

    public DepartmentLinkLocationHandler(
        IDepartmentsRepository departmentRepository,
        IDepartmentsService departmentService,
        ILocationsService locationsService,
        IDepartmentLocationsRepository departmentLocationsRepository,
        ILogger<DepartmentLinkLocationHandler> logger,
        IStatisticsService stats)
    {
        _departmentRepository = departmentRepository;
        _departmentService = departmentService;
        _locationsService = locationsService;
        _departmentLocationsRepository = departmentLocationsRepository;
        _logger = logger;
        _stats = stats;
    }
    public async Task<Result<CreatedObjectGuid, Errors>> ExecuteAsync(
        DepartmentLinkLocationCommand command,
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
            return GeneralErrors.NotFound(command.DepartmentId).ToErrors();
        }
        Department department = resultDepartment.Value;

        //поиск присоединяемой локации
        var resultLocation = await _locationsService.GetByIdAsync(command.LocationId, cancellationToken).ConfigureAwait(false);
        if (resultLocation.IsFailure)
            return resultLocation.Error.ToErrors();

        if (resultLocation.Value == null)
        {
            return GeneralErrors.NotFound(command.LocationId, "Location").ToErrors();
        }
        Location location = resultLocation.Value;

        //проверка на уникальность
        var resultLocations = await _departmentLocationsRepository.GetLocationsByDepartmentIdAsync(command.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (resultLocations.IsFailure)
        {
            _logger.LogError("Error _departmentLocationsRepository.GetLocationsByDepartmentIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска связанных локаций").ToErrors();
        }
        if (resultLocations.Value.Exists(v => v.LocationId == location.Id))
        {
            return GeneralErrors.AlreadyExist().ToErrors();
        }

        DepartmentLocation link;
        try
        {
            link = department.LinkLocation(location);
        }
        catch (DSException ex)
        {
            return GeneralErrors.OtherError(ex.Message).ToErrors();
        }

        await _stats.CreateAsync(
            location.Id.Value,
            location.GetType().Name,
            Statistica.Level.INFO,
            Statistica.Action.ATTACH,
            $"Присоединена локация {location.Name.Value}",
            department.Id.Value,
            department.GetType().Name,
            cancellationToken).ConfigureAwait(false);

        var resultSave = await _departmentService.SaveAsync(cancellationToken).ConfigureAwait(false);
        if (resultSave.IsFailure)
            return resultSave.Error.ToErrors();

        return new CreatedObjectGuid(link.Id.Value);
    }
}
