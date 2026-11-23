using CSharpFunctionalExtensions;
using DirectoryService.Contracts.Departments;
using DirectoryService.Core.Database;
using DirectoryService.Domain.Departments;
using DirectoryService.Domain.shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.Departments.Queries.GetByParentId;

public class GetDepartmentByParentIdHandler : ICommandHandler<List<GetDepartmentResponse>, GetDepartmentByParentIdCommand>
{
    private readonly IReadDbContext _readDbContext;
    private readonly ILogger<GetDepartmentByParentIdHandler> _logger;

    public GetDepartmentByParentIdHandler(
        IReadDbContext readDbContext,
        ILogger<GetDepartmentByParentIdHandler> logger)
    {
        _readDbContext = readDbContext;
        _logger = logger;
    }

    public async Task<Result<List<GetDepartmentResponse>, Errors>> ExecuteAsync(
        GetDepartmentByParentIdCommand command,
        CancellationToken cancellationToken = default)
    {
        var parentId = new DepartmentId(command.ParentDepartmentId);

        bool parentExists = await _readDbContext.DepartmentsRead
            .AnyAsync(d => d.Id == parentId, cancellationToken)
            .ConfigureAwait(false);
        if (!parentExists)
        {
            _logger.LogInformation("Department {DepartmentId} not found", command.ParentDepartmentId);
            return GeneralErrors.NotFound(command.ParentDepartmentId, "Department").ToErrors();
        }

        var rows = await _readDbContext.DepartmentsRead
            .Where(d => d.ParentId == parentId)
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
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(row => new GetDepartmentResponse(
                    row.Id.Value,
                    row.ChiefPositionId.Value,
                    row.Path.Value,
                    row.Name.Value,
                    row.ParentId?.Value,
                    row.ChildsIds.Select(c => c.Value).ToList(),
                    row.LocationsIds.Select(l => l.Value).ToList(),
                    row.PositionsIds.Select(p => p.Value).ToList()))
            .ToList();
    }
}
