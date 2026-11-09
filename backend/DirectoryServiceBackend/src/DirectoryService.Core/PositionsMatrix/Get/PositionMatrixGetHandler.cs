using CSharpFunctionalExtensions;
using DirectoryService.Shared;
using DirectoryService.Domain.PositionsMatrix;
using Microsoft.Extensions.Logging;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.Get;

public class PositionMatrixGetHandler : ICommandHandler<List<PositionMatrix>, PositionMatrixGetCommand>
{
    private readonly IPositionMatrixRepository _positionMatrixRepository;
    private readonly ILogger<PositionMatrixGetHandler> _logger;
    public PositionMatrixGetHandler(
        IPositionMatrixRepository positionMatrixRepository,
        ILogger<PositionMatrixGetHandler> logger)
    {
        _positionMatrixRepository = positionMatrixRepository;
        _logger = logger;
    }
    public async Task<Result<List<PositionMatrix>, Errors>> ExecuteAsync(PositionMatrixGetCommand command, CancellationToken cancellationToken = default)
    {
        var resultPositions = await _positionMatrixRepository.GetAsync(command.Request, cancellationToken).ConfigureAwait(false);
        if (resultPositions.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetAsync");
            return GeneralErrors.Failure("ошибка запроса поиска должностей").ToErrors();
        }
        return resultPositions.Value;
    }
}
