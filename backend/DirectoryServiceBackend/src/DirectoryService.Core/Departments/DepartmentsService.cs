using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.DepartmentChiefPositions;
using DirectoryService.Core.DepartmentPositions;
using DirectoryService.Core.Locations;
using DirectoryService.Core.DepartmentLocations;
using DirectoryService.Core.PositionsMatrix;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.DepartmentChiefPositions;
using DirectoryService.Domain.DepartmentPositions;
using DirectoryService.Domain.DepartmentLocations;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.Locations;
using DirectoryService.Domain.shared;
using DirectoryService.Domain.Statistics;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments;

public class DepartmentsService : IDepartmentsService
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly IPositionMatrixService _positionMatrixService;
    private readonly ILocationsService _locationsService;
    private readonly IDepartmentsChiefPositionRepository _departmentChiefPositionsRepository;
    private readonly IDepartmentPositionsRepository _departmentPositionsRepository;
    private readonly IDepartmentLocationsRepository _departmentLocationsRepository;
    private readonly CreateDepartmentValidator _createValidator;
    private readonly UpdateDepartmentValidator _updateValidator;
    private readonly ILogger<DepartmentsService> _logger;
    private readonly IStatisticsService _stats;
    public DepartmentsService(
        IDepartmentsRepository departmentRepository,
        IPositionMatrixService positionMatrixService,
        ILocationsService locationsService,
        IDepartmentsChiefPositionRepository departmentChiefPositionsRepository,
        IDepartmentPositionsRepository departmentPositionsRepository,
        IDepartmentLocationsRepository departmentLocationsRepository,
        CreateDepartmentValidator createValidator,
        UpdateDepartmentValidator updateValidator,
        IStatisticsService stats,
        ILogger<DepartmentsService> logger)
    {
        _departmentRepository = departmentRepository;
        _positionMatrixService = positionMatrixService;
        _locationsService = locationsService;
        _departmentChiefPositionsRepository = departmentChiefPositionsRepository;
        _departmentPositionsRepository = departmentPositionsRepository;
        _departmentLocationsRepository = departmentLocationsRepository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
        _stats = stats;
        _logger = logger;
    }

    public async Task<Result<Guid, Errors>> CreateAsync(CreateDepartmentDto departmentDto, CancellationToken cancellationToken = default)
    {
        //валидация входящих параметров
        ValidationResult validationResult = await _createValidator.ValidateAsync(departmentDto, cancellationToken).ConfigureAwait(false);
        if (!validationResult.IsValid)
        {
            return new Errors(validationResult);
        }
        //валидация бизнес-правил
        //проверка на уникальность Slug у родителя
        var resultIsDuplicate = await _departmentRepository.HasNameSlugAsync(
            departmentDto.Name,
            departmentDto.Slug,
            departmentDto.ParentDepartmentId,
            excludeId: null,
            cancellationToken).ConfigureAwait(false);
        if (resultIsDuplicate.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.HasNameSlugAsync");
            return GeneralErrors.Failure("ошибка запроса").ToErrors();
        }
        if (resultIsDuplicate.Value)
        {
            _logger.LogError("Duplicate record of Department");
            return GeneralErrors.AlreadyExist().ToErrors();
        }

        var resultDepartmentName = DepartmentName.Create(departmentDto.Name);
        if (resultDepartmentName.IsFailure)
        {
            return GeneralErrors.ValueIsInvalid("название департамента").ToErrors();
        }
        var resultSlug = Slug.Create(departmentDto.Slug);
        if (resultSlug.IsFailure)
        {
            return GeneralErrors.ValueIsInvalid("Slug").ToErrors();
        }

        Department? parent = null;

        /*
        //проверка на единственный департамент с нулевым родительским Id
        if (departmentDto.ParentDepartmentId == null)
        {
            var resultCheck = await _departmentRepository.GetByParentIdAsync(parentDepartmentId: null, cancellationToken).ConfigureAwait(false);
            if (resultCheck.IsFailure)
            {
                _logger.LogError("Error _departmentRepository.GetByParentIdAsync");
                return GeneralErrors.Failure("ошибка запроса").ToErrors();
            }
            if (resultCheck.Value.Any())
            {
                _logger.LogError("Top Department already exist");
                return GeneralErrors.AlreadyExist().ToErrors();
            }
        }
        */
        if (departmentDto.ParentDepartmentId != null)
        {
            var resultParent = await _departmentRepository.GetByIdAsync((Guid)departmentDto.ParentDepartmentId, cancellationToken).ConfigureAwait(false);
            if (resultParent.IsFailure)
            {
                _logger.LogError("Error _departmentRepository.GetByIdAsync");
                return GeneralErrors.Failure("ошибка запроса").ToErrors();
            }
            else if (resultParent.Value == null)
            {
                return GeneralErrors.NotFound((Guid)departmentDto.ParentDepartmentId).ToErrors();
            }
            parent = resultParent.Value;
        }        

        //должность начальника
        PositionMatrix positionChief;
        var resultPositionChief = await _positionMatrixService.GetByIdAsync(departmentDto.ChiefPositionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPositionChief.IsFailure)
            return resultPositionChief.Error.ToErrors();

        if (resultPositionChief.Value == null)
        {
            _logger.LogError("Record of PositionMatrix not found {1}", departmentDto.ChiefPositionMatrixId);
            return GeneralErrors.NotFound(departmentDto.ChiefPositionMatrixId).ToErrors();
        }
        else
            positionChief = resultPositionChief.Value;

        //проверка на подчинённость должности начальника
        if (parent == null && positionChief.ParentId != null)
        {
            return GeneralErrors.OtherError("у головного департамента должен быть начальник высшего уровня").ToErrors();
        }
        else if (parent != null && positionChief.ParentId == null)
        {
            return GeneralErrors.OtherError("у подчинённого департамента должен быть начальник, подчинённый начальнику родительского департамента").ToErrors();
        }
        else if (parent != null)
        {
            var resultParentDepartmentChief = await _departmentChiefPositionsRepository.GetByDepartmentIdAsync(parent.Id.Value, cancellationToken).ConfigureAwait(false);
            if (resultParentDepartmentChief.IsFailure)
                return resultParentDepartmentChief.Error.ToErrors();
            if (resultParentDepartmentChief.Value == null)
            {
                _logger.LogError("Record of DepartmentChiefPosition not found {1}", parent.Id.Value);
                return GeneralErrors.NotFound(parent.Id.Value).ToErrors();
            }

            PositionMatrix positionChiefParent;
            var resultParentChief = await _positionMatrixService.GetByIdAsync(
                resultParentDepartmentChief.Value.PositionMatrixId.Value,
                cancellationToken).ConfigureAwait(false);
            if (resultParentChief.IsFailure)
                return resultParentChief.Error.ToErrors();

            if (resultParentChief.Value == null)
            {
                _logger.LogError("Record of PositionMatrix not found {1}", resultParentDepartmentChief.Value.PositionMatrixId.Value);
                return GeneralErrors.NotFound(resultParentDepartmentChief.Value.PositionMatrixId.Value).ToErrors();
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
                return GeneralErrors.OtherError("должность начальника департамента должна быть зависима от должности начальника родительского департамента").ToErrors();
            }
        }

        Department department = Department.Create(resultDepartmentName.Value, resultSlug.Value, parent, positionChief);

        //локации
        if (departmentDto.LocationsId.Count > 0)
        {
            var resultLocations = await _locationsService.GetByIdsAsync(departmentDto.LocationsId, cancellationToken).ConfigureAwait(false);
            if(resultLocations.IsFailure)
                return resultLocations.Error.ToErrors();

            department.LinkLocations(resultLocations.Value);
        }

        var resultAdd = await _departmentRepository.AddAsync(department, cancellationToken).ConfigureAwait(false);
        if (resultAdd.IsFailure)
        {
            _logger.LogError("Error creating record of Department");
            return GeneralErrors.Failure("ошибка добавления департамента").ToErrors();
        }

        _logger.LogInformation("Department created with Id {DepartmentId}", resultAdd.Value);

        await _stats.CreateAsync(
            resultAdd.Value,
            department.GetType().Name,
            Statistica.Level.INFO,
            Statistica.Action.CREATE,
            $"Создание департамента {department.Name}",
            parent == null ? null : parent.Id.Value,
            parent == null ? null : typeof(Department).Name,
            cancellationToken).ConfigureAwait(false);

        return resultAdd.Value;
    }
    public async Task<Result<bool, Error>> DeleteAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var resultPosition = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken).ConfigureAwait(false);
        if (resultPosition.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса");
        }
        if (resultPosition.Value == null)
        {
            _logger.LogError("Record of Department not found {departmentId}", departmentId);
            return GeneralErrors.NotFound(departmentId);
        }
        var result = await DeleteAsync(resultPosition.Value, cancellationToken).ConfigureAwait(false);
        return result;
    }
    public async Task<Result<bool, Error>> DeleteAsync(Department department, CancellationToken cancellationToken = default)
    {
        //проверка на существование потомков
        var resultHasExists = await _departmentRepository.GetByParentIdAsync(department.Id.Value, cancellationToken).ConfigureAwait(false);
        if (resultHasExists.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByParentIdAsync");
            return GeneralErrors.Failure("ошибка запроса");
        }
        if (resultHasExists.Value.Any())
        {
            _logger.LogError("Has childs for {DepartmentId}", department.Id);
            return Error.Conflict("rules.failure", "есть зависимые должности. Удаление запрещено");
        }

        var result = await _departmentRepository.DeleteAsync(department, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.DeleteAsync");
            return GeneralErrors.Failure("ошибка удаления должности");
        }
        if (result.Value)
        {
            await _stats.CreateAsync(
                department.Id.Value,
                typeof(Department).Name,
                Statistica.Level.INFO,
                Statistica.Action.DELETE,
                $"Удаление",
                department.ParentId != null ? department.ParentId.Value : null,
                department.ParentId != null ? typeof(Department).Name : null,
                cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Deleting of Department {DepartmentId}", department.Id);
        }
        return result;
    }
    public async Task<Result<List<Department>, Error>> GetAsync(SelectDto request, CancellationToken cancellationToken = default)
    {
        var resultPositions = await _departmentRepository.GetAsync(request, cancellationToken).ConfigureAwait(false);
        if (resultPositions.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetAsync");
            return GeneralErrors.Failure("ошибка запроса поиска департамента");
        }
        return resultPositions.Value;
    }
    public async Task<Result<Department?, Error>> GetByIdAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        var resultPosition = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken).ConfigureAwait(false);
        if (resultPosition.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска департамента");
        }
        return resultPosition.Value;
    }
    public async Task<Result<List<Department>, Error>> GetByParentIdAsync(Guid parentDepartmentId, CancellationToken cancellationToken = default)
    {
        var resultPositions = await _departmentRepository.GetByParentIdAsync(parentDepartmentId, cancellationToken).ConfigureAwait(false);
        if (resultPositions.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByParentIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска департаментов");
        }
        return resultPositions.Value;

    }
    public async Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default)
    {
        var result = await _departmentRepository.SaveAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }
    public async Task<Result<bool, Errors>> UpdateAsync(
    Guid departmentId,
    UpdateDepartmentDto? departmentDto,
    CancellationToken cancellationToken = default)
    {
        var resultDepartment = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken).ConfigureAwait(false);
        if (resultDepartment.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска департамента").ToErrors();
        }
        if (resultDepartment.Value == null)
        {
            _logger.LogError("Record of Department not found {1}", departmentId);
            return GeneralErrors.NotFound(departmentId).ToErrors();
        }
        Department department = resultDepartment.Value;
        return await UpdateAsync(department, departmentDto, cancellationToken).ConfigureAwait(false);
    }
    public async Task<Result<bool, Errors>> UpdateAsync(
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
                    departmentDto.NewName == null ? department.Name.Value : departmentDto.NewName,
                    departmentDto.NewSlug == null ? department.Slug.Value : departmentDto.NewSlug,
                    department.ParentId == null ? null : department.ParentId.Value,
                    department.Id.Value,
                    cancellationToken).ConfigureAwait(false);

                if (resultIsDuplicate.IsFailure)
                {
                    _logger.LogError("Error _departmentRepository.HasNameSlugAsync");
                    return GeneralErrors.Failure("ошибка запроса поиска дубликатов").ToErrors();
                }
                if (resultIsDuplicate.Value)
                {
                    return GeneralErrors.AlreadyExist().ToErrors();
                }
            }
            if (departmentDto.NewName != null && !department.Name.Value.Equals(departmentDto.NewName))
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
            if (departmentDto.NewSlug != null && !department.Slug.Value.Equals(departmentDto.NewSlug))
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
                    department.SetParent(resultDepartment.Value);
                else
                {
                    _logger.LogError("Record of Department not found {1}", department.ParentId);
                    return GeneralErrors.NotFound(department.ParentId.Value).ToErrors();
                }
            }
            if (department.PathSlug != department.Parent.PathSlugFull)
            {
                newPathSlug = department.Parent.PathSlugFull;
            }
        }
        //потомки
        var resultChilds = await _departmentRepository.GetByParentIdAsync(department.Id.Value, cancellationToken).ConfigureAwait(false);
        if (resultChilds.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByParentIdAsync");
            return GeneralErrors.Failure("ошибка получения потомков").ToErrors();
        }
        else if (resultChilds.Value.Count > 0)
        {
            department.AddChilds(resultChilds.Value);
        }

        if (department.Update(newName, newSlug, null))
        {
            var resultPositions = await _departmentRepository.UpdateAsync(department, cancellationToken).ConfigureAwait(false);
            if (resultPositions.IsFailure)
            {
                _logger.LogError("Error _departmentRepository.UpdateAsync");
                return GeneralErrors.Failure("ошибка обновления должности").ToErrors();
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
                await UpdateAsync(child, null, cancellationToken).ConfigureAwait(false);
            }
        }
        return true;
    }
    public async Task<Result<bool, Errors>> ChangeParentAsync(
        Guid departmentId,
        Guid newParentDepartmentId,
        CancellationToken cancellationToken = default)
    {
        //поиск изменяемого департамента
        var resultDepartment = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken).ConfigureAwait(false);
        if (resultDepartment.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса поиска департамента").ToErrors();
        }
        if (resultDepartment.Value == null)
        {
            return GeneralErrors.NotFound(departmentId).ToErrors();
        }
        Department department = resultDepartment.Value;

        //поиск нового родительского департамента
        var resultParentDepartment = await _departmentRepository.GetByIdAsync(newParentDepartmentId, cancellationToken).ConfigureAwait(false);
        if (resultParentDepartment.IsFailure)
        {
            _logger.LogError("Request error");
            return GeneralErrors.Failure("ошибка запроса поиска нового родительского департамента").ToErrors();
        }
        if (resultParentDepartment.Value == null)
        {
            return GeneralErrors.NotFound(newParentDepartmentId).ToErrors();
        }
        Department departmentParent = resultParentDepartment.Value;

        if (department.ParentId == departmentParent.Id)
            return false;

        //проверка на зацикливание
        var resultFind = await findChildByIdAsync(department, departmentParent.Id, cancellationToken).ConfigureAwait(false);
        if (resultFind.IsSuccess && resultFind.Value != null)
        {
            return GeneralErrors.OtherError("зацикливание иерархии департаментов").ToErrors();
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
            department.ParentId.Value,
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
            var resultUpdateRepos = await _departmentRepository.UpdateAsync(department, cancellationToken).ConfigureAwait(false);
            if (resultUpdateRepos.IsFailure)
            {
                _logger.LogError("Error _departmentRepository.UpdateAsync");
                return GeneralErrors.Failure("ошибка обновления департамента").ToErrors();
            }
            var resultUpdate = await this.UpdateAsync(department, null, cancellationToken).ConfigureAwait(false);
            if (resultUpdate.IsFailure)
                return resultUpdate.Error;

            return resultUpdate.Value;
        }
        return false;
    }
    public async Task<Result<bool, Error>> ChangeChiefPositionMatrixAsync(
    Guid departmentId,
    Guid newChiefPositionMatrixId,
    CancellationToken cancellationToken = default)
    {
        //поиск изменяемого департамента
        var resultDepartment = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken).ConfigureAwait(false);
        if (resultDepartment.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска департамента");
        }
        if (resultDepartment.Value == null)
        {
            return GeneralErrors.NotFound(departmentId);
        }
        Department department = resultDepartment.Value;

        //поиск новой должности руководителя
        var resultChiefPositionMatrixId = await _positionMatrixService.GetByIdAsync(newChiefPositionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultChiefPositionMatrixId.IsFailure)
            return resultChiefPositionMatrixId.Error;

        if (resultChiefPositionMatrixId.Value == null)
        {
            return GeneralErrors.NotFound(newChiefPositionMatrixId);
        }
        PositionMatrix newChiefPositionMatrix = resultChiefPositionMatrixId.Value;

        if (department.ChiefPositionMatrix.Id == newChiefPositionMatrix.Id)
            return false;

        //проверка на подчинённость должности начальника
        if (department.ParentId == null && newChiefPositionMatrix.ParentId != null)
            return GeneralErrors.OtherError("у головного департамента должен быть начальник высшего уровня");
        else if (department.ParentId != null && newChiefPositionMatrix.ParentId == null)
            return GeneralErrors.OtherError("у подчинённого департамента должен быть начальник, подчинённый начальнику родительского департамента");
        else if (department.ParentId != null)
        {
            var resultParentDepartment = await _departmentRepository.GetByIdAsync(department.ParentId.Value, cancellationToken).ConfigureAwait(false);
            if (resultParentDepartment.IsFailure)
            {
                _logger.LogError("Error _departmentRepository.GetByIdAsync");
                return GeneralErrors.Failure("ошибка запроса поиска департамента");
            }
            if (resultParentDepartment.Value == null)
            {
                return GeneralErrors.NotFound(department.ParentId.Value);
            }
            Department departmentParent = resultParentDepartment.Value;

            var resultChiefDepartamentParent = await _departmentChiefPositionsRepository.GetByDepartmentIdAsync(
                departmentParent.Id.Value,
                cancellationToken).ConfigureAwait(false);
            if (resultChiefDepartamentParent.IsFailure)
            {
                _logger.LogError("Error _departmentChiefPositionsRepository.GetByDepartmentIdAsync");
                return GeneralErrors.Failure("ошибка поиска должности начальника родительского департамента");
            }
            if (resultChiefDepartamentParent.Value == null)
            {
                _logger.LogError("Record of DepartmentChiefPositionMatrix not found {1}", departmentParent.Id);
                return GeneralErrors.NotFound(departmentParent.Id.Value);
            }
            DepartmentChiefPosition departamentChiefParent = resultChiefDepartamentParent.Value;
            var resultPositionChiefParent = await _positionMatrixService.GetByIdAsync(
                departamentChiefParent.PositionMatrixId.Value,
                cancellationToken).ConfigureAwait(false);
            if (resultPositionChiefParent.IsFailure)
                return resultPositionChiefParent.Error;
            if (resultPositionChiefParent.Value == null)
            {
                _logger.LogError("Record of PositionMatrix not found {1}", departamentChiefParent.PositionMatrixId.Value);
                return GeneralErrors.NotFound(departmentParent.Id.Value);
            }

            var resultCheck = await _positionMatrixService.FindChildByIdAsync(
            resultPositionChiefParent.Value,
            newChiefPositionMatrix.Id,
            cancellationToken).ConfigureAwait(false);
            if (resultCheck.IsFailure)
                return resultCheck.Error;
            if (resultCheck.Value == null)
                return GeneralErrors.OtherError("должность начальника департамента должна быть зависима от должности начальника родительского департамента");
        }

        var resultDepartmentChiefPosition = await _departmentChiefPositionsRepository.GetByDepartmentIdAsync(
                department.Id.Value,
                cancellationToken).ConfigureAwait(false);
        if(resultDepartmentChiefPosition.IsFailure)
        {
            _logger.LogError("Error _departmentChiefPositionsRepository.GetByDepartmentIdAsync");
            return GeneralErrors.Failure("ошибка поиска привязки должности начальника департамента");
        }
        else if (resultDepartmentChiefPosition.Value == null)
        {
            _logger.LogError("Record of DepartmentChiefPosition not found {1}", department.Id.Value);
            return GeneralErrors.NotFound(department.Id.Value);
        }
        var resultDelete = await _departmentChiefPositionsRepository.DeleteAsync(
                resultDepartmentChiefPosition.Value,
                cancellationToken).ConfigureAwait(false);
        if (resultDelete.IsFailure)
        {
            _logger.LogError("Error _departmentChiefPositionsRepository.DeleteAsync");
            return GeneralErrors.Failure("ошибка удаления привязки должности начальника департамента");
        }
        DepartmentChiefPosition newDCP = new DepartmentChiefPosition(department, newChiefPositionMatrix);
        var resultAdd = await _departmentChiefPositionsRepository.AddAsync(newDCP).ConfigureAwait(false);
        if (resultAdd.IsFailure)
        {
            _logger.LogError("Error _departmentChiefPositionsRepository.AddAsync");
            return GeneralErrors.Failure("ошибка создания привязки должности начальника департамента");
        }
        DepartmentChiefPosition newDepartmentChiefPosition = DepartmentChiefPosition.Create(department.Id, newChiefPositionMatrix);
        department.Update(null, null, newDepartmentChiefPosition);

        await _stats.CreateAsync(
            department.Id.Value,
            typeof(Department).Name,
            Statistica.Level.FINE,
            Statistica.Action.UPDATE,
            $"Изменена должность начальника на {newChiefPositionMatrix.Name}",
            null,
            null,
            cancellationToken).ConfigureAwait(false);

        return true;
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
    public async Task<Result<Guid, Error>> LinkPositionAsync(
        Guid departmentId,
        Guid positionMatrixId,
        CancellationToken cancellationToken = default)
    {
        //поиск департамента
        var resultDepartment = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken).ConfigureAwait(false);
        if (resultDepartment.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска департамента");
        }
        if (resultDepartment.Value == null)
        {
            return GeneralErrors.NotFound(departmentId);
        }
        Department department = resultDepartment.Value;

        //поиск присоединяемой должности
        var resultPositionMatrix = await _positionMatrixService.GetByIdAsync(positionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPositionMatrix.IsFailure)
            return resultPositionMatrix.Error;

        if (resultPositionMatrix.Value == null)
        {
            return GeneralErrors.NotFound(positionMatrixId);
        }
        PositionMatrix positionMatrix = resultPositionMatrix.Value;

        //поиск должности начальника
        var resultChiefDepartament = await _departmentChiefPositionsRepository.GetByDepartmentIdAsync(
            department.Id.Value,
            cancellationToken).ConfigureAwait(false);
        if (resultChiefDepartament.IsFailure)
        {
            _logger.LogError("Error _departmentChiefPositionsRepository.GetByDepartmentIdAsync");
            return GeneralErrors.Failure("ошибка поиска должности начальника департамента");
        }
        if (resultChiefDepartament.Value == null)
        {
            _logger.LogError("Record of DepartmentChiefPositionMatrix not found {1}", department.Id);
            return GeneralErrors.NotFound(department.Id.Value);
        }
        DepartmentChiefPosition departamentChief = resultChiefDepartament.Value;

        //проверка на подчинённость должности начальника
        if (positionMatrix.ParentId == null)
            return GeneralErrors.OtherError("нельзя прикрепить должность высшего уровня");

        var resultChild = await _positionMatrixService.FindChildByIdAsync(departamentChief.PositionMatrix, positionMatrix.Id, cancellationToken).ConfigureAwait(false);
        if (resultChild.IsFailure)
            return resultChild.Error;
        if(resultChild.Value == null)
            return GeneralErrors.OtherError("прикрепляемая должность должна быть зависима от должности начальника департамента");

        DepartmentPosition link;
        try
        {
            link = department.LinkPosition(positionMatrix);
        }
        catch(DSException ex)
        {
            return GeneralErrors.OtherError(ex.Message);
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

        return link.Id.Value;
    }

    public async Task<Result<bool, Error>> DetachPositionAsync(
    Guid departmentPositionId,
    CancellationToken cancellationToken = default)
    {
        //поиск линка
        var resultLink = await _departmentPositionsRepository.GetByIdAsync(departmentPositionId, cancellationToken).ConfigureAwait(false);
        if (resultLink.IsFailure)
        {
            _logger.LogError("Error _departmentPositionsRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска связи департамент-должность");
        }
        if (resultLink.Value == null)
        {
            return GeneralErrors.NotFound(departmentPositionId);
        }
        DepartmentPosition dp = resultLink.Value;

        var resultDelete = await _departmentPositionsRepository.DeleteAsync(dp, cancellationToken).ConfigureAwait(false);
        if (resultDelete.IsFailure)
        {
            _logger.LogError("Error _departmentPositionsRepository.DeleteAsync");
            return GeneralErrors.Failure("ошибка удаления связи департамент-должность");
        }
        if(resultDelete.Value)
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

        return resultDelete.Value;
    }

    public async Task<Result<Guid, Error>> LinkLocationAsync(
        Guid departmentId,
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        //поиск департамента
        var resultDepartment = await _departmentRepository.GetByIdAsync(departmentId, cancellationToken).ConfigureAwait(false);
        if (resultDepartment.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска департамента");
        }
        if (resultDepartment.Value == null)
        {
            return GeneralErrors.NotFound(departmentId);
        }
        Department department = resultDepartment.Value;

        //поиск присоединяемой локации
        var resultLocation = await _locationsService.GetByIdAsync(locationId, cancellationToken).ConfigureAwait(false);
        if (resultLocation.IsFailure)
            return resultLocation.Error;

        if (resultLocation.Value == null)
        {
            return GeneralErrors.NotFound(locationId);
        }
        Location location = resultLocation.Value;

        //проверка на уникальность
        var resultLocations = await _departmentLocationsRepository.GetLocationsByDepartmentIdAsync(departmentId, cancellationToken).ConfigureAwait(false);
        if (resultLocations.IsFailure)
        {
            _logger.LogError("Error _departmentLocationsRepository.GetLocationsByDepartmentIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска связанных локаций");
        }
        if (resultLocations.Value.Any(v => v.LocationId == location.Id))
            return GeneralErrors.AlreadyExist();

        DepartmentLocation link;
        try
        {
            link = department.LinkLocation(location);
        }
        catch (DSException ex)
        {
            return GeneralErrors.OtherError(ex.Message);
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

        return link.Id.Value;
    }

    public async Task<Result<bool, Error>> DetachLocationAsync(
        Guid departmentId,
        Guid locationId,
        CancellationToken cancellationToken = default)
    {
        //поиск линка
        var resultLocations = await _departmentLocationsRepository.GetLocationsByDepartmentIdAsync(departmentId, cancellationToken).ConfigureAwait(false);
        if (resultLocations.IsFailure)
        {
            _logger.LogError("Error _departmentLocationsRepository.GetLocationsByDepartmentIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска связанных локаций");
        }
        DepartmentLocation? link = resultLocations.Value.SingleOrDefault(v => v.LocationId.Value == locationId);

        if (link == null)
            return GeneralErrors.NotFound(locationId, "LocationId");

        Department department = link.Department;
        Location location = link.Location;

        department.DetachLocation(location);

        var resultDelete = await _departmentLocationsRepository.DeleteAsync(link, cancellationToken).ConfigureAwait(false);
        if (resultDelete.IsFailure)
        {
            _logger.LogError("Error _departmentLocationsRepository.DeleteAsync");
            return GeneralErrors.Failure("ошибка удаления связи департамент-локация");
        }
        if (resultDelete.Value)
        {
            await _stats.CreateAsync(
            location.Id.Value,
            location.GetType().Name,
            Statistica.Level.INFO,
            Statistica.Action.DETACH,
            $"Отсоединена локация {location.Name.Value}",
            department.Id.Value,
            department.GetType().Name,
            cancellationToken).ConfigureAwait(false);
        }
        return resultDelete.Value;
    }
}
