using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Departments.GetById;
using DirectoryService.Domain.Departments;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.GetByParentId;
public class GetDepartmentByParentIdHandler : ICommandHandler<List<Department>, GetDepartmentByParentIdCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly ILogger<GetDepartmentByParentIdHandler> _logger;

    public GetDepartmentByParentIdHandler(
        IDepartmentsRepository departmentRepository,
        ILogger<GetDepartmentByParentIdHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _logger = logger;
    }
    public async Task<Result<List<Department>, Errors>> ExecuteAsync(GetDepartmentByParentIdCommand command, CancellationToken cancellationToken = default)
    {
        var resultPositions = await _departmentRepository.GetByParentIdAsync(command.ParentDepartmentId, cancellationToken).ConfigureAwait(false);
        if (resultPositions.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByParentIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска департаментов").ToErrors();
        }
        return resultPositions.Value;
    }
}
