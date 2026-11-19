using CSharpFunctionalExtensions;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.Abstractions;
using DirectoryService.Core.Database;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.PositionsMatrix.Queries.GetByParentId;

public class PositionMatrixGetByParentIdHandler : ICommandHandler<List<GetPositionMatrixResponse>, PositionMatrixGetByParentIdCommand>
{
    private readonly IReadDbContext _readDbContext;
    private readonly ILogger<PositionMatrixGetByParentIdHandler> _logger;

    public PositionMatrixGetByParentIdHandler(
        IReadDbContext readDbContext,
        ILogger<PositionMatrixGetByParentIdHandler> logger)
    {
        _readDbContext = readDbContext;
        _logger = logger;
    }

    public async Task<Result<List<GetPositionMatrixResponse>, Errors>> ExecuteAsync(
        PositionMatrixGetByParentIdCommand command,
        CancellationToken cancellationToken = default)
    {
        var parentId = new PositionMatrixId(command.ParentPositionMatrixId);

        bool parentExists = await _readDbContext.PositionsRead
            .AnyAsync(p => p.Id == parentId, cancellationToken)
            .ConfigureAwait(false);
        if (!parentExists)
        {
            _logger.LogInformation("PositionMatrix {PositionMatrixId} not found", command.ParentPositionMatrixId);
            return GeneralErrors.NotFound(command.ParentPositionMatrixId, "Position").ToErrors();
        }

        var rows = await _readDbContext.PositionsRead
            .Where(p => p.ParentId == parentId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Path,
                p.ParentId,
                ChildsIds = p.Childs.Select(c => c.Id).ToList(),
            })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return rows
            .Select(row => new GetPositionMatrixResponse(
                row.Id.Value,
                row.Name.Value,
                row.Path.Value,
                row.ParentId?.Value,
                row.ChildsIds.Select(c => c.Value).ToList()))
            .ToList();
    }
}
