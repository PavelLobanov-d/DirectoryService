using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.shared;
using DirectoryService.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;

namespace DirectoryService.Core.Departments.Queries.GetById;
public class GetDepartmentByIdHandler : ICommandHandler<GetDepartmentResponse?, GetDepartmentByIdCommand>
{
    private readonly IReadDbContext _readDbContext;
    private readonly ILogger<GetDepartmentByIdHandler> _logger;
    public GetDepartmentByIdHandler(IReadDbContext readDbContext, ILogger<GetDepartmentByIdHandler> logger)
    {
        _readDbContext = readDbContext;
        _logger = logger;
    }

    public async Task<Result<GetDepartmentResponse, Errors>> ExecuteAsync(
       GetDepartmentByIdCommand command,
       CancellationToken cancellationToken = default)
    {
        var departmentId = new DepartmentId(command.DepartmentId);

        var row = await _readDbContext.DepartmentsRead
            .Where(d => d.Id == departmentId)
            .Select(d => new
            {
                d.Id,
                ChiefPositionId = d.DepartmentChiefPosition.PositionMatrixId,
                d.Path,
                d.Name,
                d.ParentId,
                ChildsIds = d.Childs.Select(c => c.Id).ToList(),
                LocationsIds = d.DepartmentLocations.Select(dl => dl.LocationId).ToList(),
                PositionsIds = d.DepartmentPositions.Select(dp => dp.PositionMatrixId).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            _logger.LogInformation("Department {DepartmentId} not found", command.DepartmentId);
            return GeneralErrors.NotFound(command.DepartmentId, "Department").ToErrors();
        }

        return new GetDepartmentResponse(
            row.Id.Value,
            row.ChiefPositionId.Value,
            row.Path.Value,
            row.Name.Value,
            row.ParentId?.Value,
            row.ChildsIds.Select(c => c.Value).ToList(),
            row.LocationsIds.Select(l => l.Value).ToList(),
            row.PositionsIds.Select(p => p.Value).ToList());
    }

}
