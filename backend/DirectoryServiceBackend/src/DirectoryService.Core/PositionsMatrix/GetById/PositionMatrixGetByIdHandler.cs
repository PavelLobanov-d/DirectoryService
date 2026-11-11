using CSharpFunctionalExtensions;
using DirectoryService.Shared;
using DirectoryService.Domain.PositionsMatrix;
using Microsoft.Extensions.Logging;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.GetById;

public class PositionMatrixGetByIdHandler : ICommandHandler<PositionMatrix?, PositionMatrixGetByIdCommand>
{
    private readonly IPositionMatrixService _positionMatrixService;
    private readonly ILogger<PositionMatrixGetByIdHandler> _logger;
    public PositionMatrixGetByIdHandler(
        IPositionMatrixService positionMatrixService,
        ILogger<PositionMatrixGetByIdHandler> logger)
    {
        _positionMatrixService = positionMatrixService;
        _logger = logger;
    }
    public async Task<Result<PositionMatrix?, Errors>> ExecuteAsync(PositionMatrixGetByIdCommand command, CancellationToken cancellationToken = default)
    {
        var resultPosition = await _positionMatrixService.GetByIdAsync(command.PositionMatrixId, cancellationToken).ConfigureAwait(false);
        if (resultPosition.IsFailure)
            return resultPosition.Error.ToErrors();
        if(resultPosition.Value == null)
            return GeneralErrors.NotFound(command.PositionMatrixId).ToErrors();

        return resultPosition.Value;
    }
}
