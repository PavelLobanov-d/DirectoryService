using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Departments.Get;
using DirectoryService.Domain.Departments;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.GetById;
public class GetDepartmentByIdHandler : ICommandHandler<Department?, GetDepartmentByIdCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly ILogger<GetDepartmentByIdHandler> _logger;
    public GetDepartmentByIdHandler(IDepartmentsRepository departmentRepository, ILogger<GetDepartmentByIdHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _logger = logger;
    }
    public async Task<Result<Department?, Errors>> ExecuteAsync(GetDepartmentByIdCommand command, CancellationToken cancellationToken = default)
    {
        var resultPosition = await _departmentRepository.GetByIdAsync(command.DepartmentId, cancellationToken).ConfigureAwait(false);
        if (resultPosition.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска департамента").ToErrors();
        }

        if (resultPosition.Value == null)
        {
            _logger.LogWarning("не найден Department по {DepartmentId}", command.DepartmentId);
        }

        return resultPosition.Value;
    }
}
