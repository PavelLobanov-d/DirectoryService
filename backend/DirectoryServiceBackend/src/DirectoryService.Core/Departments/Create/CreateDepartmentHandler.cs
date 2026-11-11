using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Core.DepartmentChiefPositions;
using DirectoryService.Core.Locations;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Core.Statistics;
using DirectoryService.Core.Validation;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.Create;
public class CreateDepartmentHandler : ICommandHandler<CreatedObjectGuid, CreateDepartmentCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly IPositionMatrixService _positionMatrixService;
    private readonly ILocationsService _locationsService;
    private readonly IDepartmentsChiefPositionRepository _departmentChiefPositionsRepository;
    private readonly ITransactionManager _transactionManager;
    private readonly ILogger<CreateDepartmentHandler> _logger;
    private readonly IStatisticsService _stats;
    private readonly CreateDepartmentValidator _validator;

    public CreateDepartmentHandler(
        IDepartmentsRepository departmentRepository,
        IPositionMatrixService positionMatrixService,
        ILocationsService locationsService,
        IDepartmentsChiefPositionRepository departmentChiefPositionsRepository,
        ITransactionManager transactionManager,
        ILogger<CreateDepartmentHandler> logger,
        IStatisticsService stats,
        CreateDepartmentValidator validator)
    {
        _departmentChiefPositionsRepository = departmentChiefPositionsRepository;
        _departmentRepository = departmentRepository;
        _positionMatrixService = positionMatrixService;
        _locationsService = locationsService;
        _transactionManager = transactionManager;
        _logger = logger;
        _stats = stats;
        _validator = validator;
    }

    public async Task<Result<CreatedObjectGuid, Errors>> ExecuteAsync(CreateDepartmentCommand command, CancellationToken cancellationToken = default)
    {
        //валидация входящих параметров
        ValidationResult validationResult = await _validator.ValidateAsync(command.DepartmentDto, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            return validationResult.ToErrors();
        }
        //валидация бизнес-правил
        //проверка на уникальность Slug у родителя
        var resultIsDuplicate = await _departmentRepository.HasNameSlugAsync(
            command.DepartmentDto.Name,
            command.DepartmentDto.Slug,
            command.DepartmentDto.ParentDepartmentId,
            excludeId: null,
            cancellationToken).ConfigureAwait(false);
        if (resultIsDuplicate.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.HasNameSlugAsync");
            return resultIsDuplicate.Error.ToErrors();
        }
        if (resultIsDuplicate.Value)
        {
            _logger.LogError("Duplicate record of Department");
            return GeneralErrors.AlreadyExist().ToErrors();
        }

        var resultDepartmentName = DepartmentName.Create(command.DepartmentDto.Name);
        if (resultDepartmentName.IsFailure)
        {
            return GeneralErrors.ValueIsInvalid("название департамента").ToErrors();
        }
        var resultSlug = Slug.Create(command.DepartmentDto.Slug);
        if (resultSlug.IsFailure)
        {
            return GeneralErrors.ValueIsInvalid("Slug").ToErrors();
        }

        Department? parent = null;

        if (command.DepartmentDto.ParentDepartmentId != null)
        {
            var resultParent = await _departmentRepository.GetByIdAsync((Guid)command.DepartmentDto.ParentDepartmentId, cancellationToken).ConfigureAwait(false);
            if (resultParent.IsFailure)
            {
                _logger.LogError("Error _departmentRepository.GetByIdAsync");
                return GeneralErrors.Failure("ошибка запроса").ToErrors();
            }

            if (resultParent.Value == null)
            {
                return GeneralErrors.NotFound((Guid)command.DepartmentDto.ParentDepartmentId, "Department").ToErrors();
            }
            parent = resultParent.Value;
        }

        //должность начальника
        PositionMatrix positionChief;
        var resultPositionChief = await _positionMatrixService.GetByIdAsync(command.DepartmentDto.ChiefPositionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPositionChief.IsFailure)
            return resultPositionChief.Error.ToErrors();

        if (resultPositionChief.Value == null)
        {
            _logger.LogError("Record of PositionMatrix not found {ChiefPositionMatrixId}", command.DepartmentDto.ChiefPositionMatrixId);
            return GeneralErrors.NotFound(command.DepartmentDto.ChiefPositionMatrixId, "PositionMatrix").ToErrors();
        }
        else
            positionChief = resultPositionChief.Value;

        //проверка на подчинённость должности начальника
        if (parent == null && positionChief.ParentId != null)
        {
            return GeneralErrors.ValueIsInvalid("у головного департамента должен быть начальник высшего уровня").ToErrors();
        }
        else if (parent != null && positionChief.ParentId == null)
        {
            return GeneralErrors.ValueIsInvalid("у подчинённого департамента должен быть начальник, подчинённый начальнику родительского департамента").ToErrors();
        }
        else if (parent != null)
        {
            var resultParentDepartmentChief = await _departmentChiefPositionsRepository.GetByDepartmentIdAsync(parent.Id.Value, cancellationToken).ConfigureAwait(false);
            if (resultParentDepartmentChief.IsFailure)
                return resultParentDepartmentChief.Error.ToErrors();
            if (resultParentDepartmentChief.Value == null)
            {
                _logger.LogError("Record of DepartmentChiefPosition not found {DepartmentId}", parent.Id.Value);
                return GeneralErrors.NotFound(parent.Id.Value, "DepartmentChiefPosition").ToErrors();
            }

            PositionMatrix positionChiefParent;
            var resultParentChief = await _positionMatrixService.GetByIdAsync(
                resultParentDepartmentChief.Value.PositionMatrixId.Value,
                cancellationToken).ConfigureAwait(false);
            if (resultParentChief.IsFailure)
                return resultParentChief.Error.ToErrors();

            if (resultParentChief.Value == null)
            {
                _logger.LogError("Record of PositionMatrix not found {PositionMatrixId}", resultParentDepartmentChief.Value.PositionMatrixId.Value);
                return GeneralErrors.NotFound(resultParentDepartmentChief.Value.PositionMatrixId.Value, "PositionMatrix").ToErrors();
            }
            positionChiefParent = resultParentChief.Value;

            var resultCheck = await _positionMatrixService.FindChildByIdAsync(
            positionChiefParent,
            positionChief.Id,
            cancellationToken).ConfigureAwait(false);
            if (resultCheck.IsFailure)
                return resultCheck.Error.ToErrors();

            if (resultCheck.Value == null)
            {
                return GeneralErrors.ValueIsInvalid("должность начальника департамента должна быть зависима от должности начальника родительского департамента").ToErrors();
            }
        }

        Department department = Department.Create(resultDepartmentName.Value, resultSlug.Value, parent, positionChief);

        //локации
        if (command.DepartmentDto.LocationsId.Count > 0)
        {
            var resultLocations = await _locationsService.GetByIdsAsync(command.DepartmentDto.LocationsId, cancellationToken).ConfigureAwait(false);
            if (resultLocations.IsFailure)
                return resultLocations.Error.ToErrors();

            department.LinkLocations(resultLocations.Value);
        }

        var transactionScopeResult = await _transactionManager.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        if (transactionScopeResult.IsFailure)
            return transactionScopeResult.Error.ToErrors();

        await using var transactionScope = transactionScopeResult.Value;


        var resultAdd = await _departmentRepository.AddAsync(department, cancellationToken).ConfigureAwait(false);
        if (resultAdd.IsFailure)
        {
            _logger.LogError("Error creating record of Department");
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();

            return resultAdd.Error.ToErrors();
        }

        _logger.LogInformation("Department created with Id {DepartmentId}", resultAdd.Value);

        await _stats.CreateAsync(
            resultAdd.Value,
            department.GetType().Name,
            Statistica.Level.INFO,
            Statistica.Action.CREATE,
            $"Создание департамента {department.Name}",
            parent?.Id.Value,
            parent == null ? null : typeof(Department).Name,
            cancellationToken).ConfigureAwait(false);

        var saveResult = await _transactionManager.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (saveResult.IsFailure)
            return saveResult.Error.ToErrors();

        var commitResult = await transactionScope.CommitAsync(cancellationToken).ConfigureAwait(false);
        if (commitResult.IsFailure)
            return commitResult.Error.ToErrors();
        else
        {
            var rollbackResult = await transactionScope.RollBackAsync(cancellationToken).ConfigureAwait(false);
            if (rollbackResult.IsFailure)
                return rollbackResult.Error.ToErrors();
        }

        return new CreatedObjectGuid(resultAdd.Value);
    }
}
