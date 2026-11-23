using CSharpFunctionalExtensions;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Core.Database;
using DirectoryService.Domain.PositionsMatrix;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Core.PositionsMatrix.Queries.GetById;

public class PositionMatrixGetByIdHandler : ICommandHandler<GetPositionMatrixResponse, PositionMatrixGetByIdCommand>
{
    private readonly IReadDbContext _readDbContext;
    private readonly ILogger<PositionMatrixGetByIdHandler> _logger;

    public PositionMatrixGetByIdHandler(
        IReadDbContext readDbContext,
        ILogger<PositionMatrixGetByIdHandler> logger)
    {
        _readDbContext = readDbContext;
        _logger = logger;
    }

    public async Task<Result<GetPositionMatrixResponse, Errors>> ExecuteAsync(
        PositionMatrixGetByIdCommand command,
        CancellationToken cancellationToken = default)
    {
        var positionId = new PositionMatrixId(command.PositionMatrixId);

        var row = await _readDbContext.PositionsRead
            .Where(p => p.Id == positionId)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Path,
                p.ParentId,
                ChildsIds = p.Childs.Select(c => c.Id).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            _logger.LogInformation("PositionMatrix {PositionMatrixId} not found", command.PositionMatrixId);
            return GeneralErrors.NotFound(command.PositionMatrixId, "Position").ToErrors();
        }

        return new GetPositionMatrixResponse(
            row.Id.Value,
            row.Name.Value,
            row.Path.Value,
            row.ParentId?.Value,
            row.ChildsIds.Select(c => c.Value).ToList());
    }
}
