using CSharpFunctionalExtensions;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Departments.DetachPosition;
using DirectoryService.Domain.Departments;
using DirectoryService.Shared;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.Get;
public class GetDepartmentHandler : ICommandHandler<List<Department>, GetDepartmentCommand>
{
    private readonly IDepartmentsRepository _departmentRepository;
    private readonly ILogger<GetDepartmentHandler> _logger;

    public GetDepartmentHandler(
        IDepartmentsRepository departmentRepository,
        ILogger<GetDepartmentHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _logger = logger;
    }
    public async Task<Result<List<Department>, Errors>> ExecuteAsync(GetDepartmentCommand command, CancellationToken cancellationToken = default)
    {
        var resultPositions = await _departmentRepository.GetAsync(command.Request, cancellationToken).ConfigureAwait(false);
        if (resultPositions.IsFailure)
        {
            _logger.LogError("Error _departmentRepository.GetAsync");
            return GeneralErrors.Failure("ошибка запроса поиска департамента").ToErrors();
        }
        return resultPositions.Value;
    }
}
