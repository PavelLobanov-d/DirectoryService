using CSharpFunctionalExtensions;
using DirectoryService.Shared;
using DirectoryService.Domain.PositionsMatrix;
using Microsoft.Extensions.Logging;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.GetById;

public class PositionMatrixGetByIdHandler : ICommandHandler<PositionMatrix?, PositionMatrixGetByIdCommand>
{
    private readonly IPositionMatrixRepository _positionMatrixRepository;
    private readonly ILogger<PositionMatrixGetByIdHandler> _logger;
    public PositionMatrixGetByIdHandler(
        IPositionMatrixRepository positionMatrixRepository,
        ILogger<PositionMatrixGetByIdHandler> logger)
    {
        _positionMatrixRepository = positionMatrixRepository;
        _logger = logger;
    }
    public async Task<Result<PositionMatrix?, Errors>> ExecuteAsync(PositionMatrixGetByIdCommand command, CancellationToken cancellationToken = default)
    {
        var resultPosition = await _positionMatrixRepository.GetByIdAsync(command.PositionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPosition.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetByIdAsync");
            return GeneralErrors.Failure("ошибка запроса поиска должностей").ToErrors();
        }
        return resultPosition.Value;
    }
}
