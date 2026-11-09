using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Statistics;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.Statistics;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.Delete;
public class DeleteDepartmentHandler : ICommandHandler<bool, DeleteDepartmentCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly IDepartmentsService _departmentService;
    private readonly ILogger<DeleteDepartmentHandler> _logger;
    private readonly IStatisticsService _stats;

    public DeleteDepartmentHandler(
        IDepartmentsRepository departmentRepository,
        IDepartmentsService departmentService,
        ILogger<DeleteDepartmentHandler> logger,
        IStatisticsService stats)
    {
        _departmentRepository = departmentRepository;
        _departmentService = departmentService;
        _logger = logger;
        _stats = stats;
    }
    public async Task<Result<bool, Errors>> ExecuteAsync(DeleteDepartmentCommand command, CancellationToken cancellationToken = default)
    {
        var resultDepartment = await _departmentRepository.GetByIdAsync(command.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (resultDepartment.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса").ToErrors();
        }
        if (resultDepartment.Value == null)
        {
            _logger.LogError("Record of Department not found {DepartmentId}", command.DepartmentId);
            return GeneralErrors.NotFound(command.DepartmentId, "Department").ToErrors();
        }
        var result = await DeleteAsync(resultDepartment.Value, cancellationToken).ConfigureAwait(false);

        return result;
    }

    private async Task<Result<bool, Errors>> DeleteAsync(Department department, CancellationToken cancellationToken = default)
    {
        //проверка на существование потомков
        var resultHasExists = await _departmentRepository.GetByParentIdAsync(department.Id.Value, cancellationToken).ConfigureAwait(false);
        if (resultHasExists.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByParentIdAsync");
            return GeneralErrors.Failure("ошибка запроса").ToErrors();
        }
        if (resultHasExists.Value.Count > 0)
        {
            _logger.LogError("Has childs for {DepartmentId}", department.Id);
            return Error.Conflict("rules.failure", "есть зависимые департаменты. Удаление запрещено").ToErrors();
        }

        var result = await _departmentRepository.DeleteAsync(department, cancellationToken).ConfigureAwait(false);
        if (result.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.DeleteAsync");
            return GeneralErrors.Failure("ошибка удаления должности").ToErrors();
        }
        if (result.Value)
        {
            await _stats.CreateAsync(
                department.Id.Value,
                typeof(Department).Name,
                Statistica.Level.INFO,
                Statistica.Action.DELETE,
                "Удаление",
                department.ParentId?.Value,
                department.ParentId != null ? typeof(Department).Name : null,
                cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Deleting of Department {DepartmentId}", department.Id);
        }
        if (result.IsSuccess)
        {
            var resultSave = await _departmentService.SaveAsync(cancellationToken).ConfigureAwait(false);
            if (resultSave.IsFailure)
                return resultSave.Error.ToErrors();
        }
        return result.Value;
    }
}
