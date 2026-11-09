using CSharpFunctionalExtensions;
using DirectoryService.Shared;
using DirectoryService.Domain.PositionsMatrix;
using Microsoft.Extensions.Logging;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.GetByParentId;

public class PositionMatrixGetByParentIdHandler : ICommandHandler<List<PositionMatrix>, PositionMatrixGetByParentIdCommand>
{
    private readonly IPositionMatrixRepository _positionMatrixRepository;
    private readonly ILogger<PositionMatrixGetByParentIdHandler> _logger;

    public PositionMatrixGetByParentIdHandler(
        IPositionMatrixRepository positionMatrixRepository,
        ILogger<PositionMatrixGetByParentIdHandler> logger)
    {
        _positionMatrixRepository = positionMatrixRepository;
        _logger = logger;
    }
    public async Task<Result<List<PositionMatrix>, Errors>> ExecuteAsync(PositionMatrixGetByParentIdCommand command, CancellationToken cancellationToken = default)
    {
        var resultPositions = await _positionMatrixRepository.GetByParentIdAsync(command.ParentPositionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPositions.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByParentIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска родительской должности").ToErrors();
        }
        return resultPositions.Value;
    }
}
