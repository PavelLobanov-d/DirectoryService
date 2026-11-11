using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.DepartmentLocations;
using DirectoryService.Core.Locations;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;
using System.Transactions;

namespace DirectoryService.Core.Departments.LinkLocation;

public class DepartmentLinkLocationHandler : ICommandHandler<CreatedObjectGuid, DepartmentLinkLocationCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly ILocationsService _locationsService;
    private readonly IDepartmentLocationsRepository _departmentLocationsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<DepartmentLinkLocationHandler> _logger;
    private readonly IStatisticsService _stats;

    public DepartmentLinkLocationHandler(
        IDepartmentsRepository departmentRepository,
        ILocationsService locationsService,
        IDepartmentLocationsRepository departmentLocationsRepository,
        ITransactionManager transactionManager,
        ILogger<DepartmentLinkLocationHandler> logger,
        IStatisticsService stats)
    {
        _departmentRepository = departmentRepository;
        _locationsService = locationsService;
        _departmentLocationsRepository = departmentLocationsRepository;
        _transactionManager = transactionManager;
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

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;

        DepartmentLocation link;
        try
        {
            link = department.LinkLocation(location);
        }
        catch (DSException ex)
        {
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

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

        return new CreatedObjectGuid(link.Id.Value);
    }
}
