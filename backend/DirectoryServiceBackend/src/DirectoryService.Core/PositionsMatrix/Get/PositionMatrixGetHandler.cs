using CSharpFunctionalExtensions;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Shared;
using DirectoryService.Domain.PositionsMatrix;
using Microsoft.Extensions.Logging;
using DirectoryService.Core.Abstractions;

namespace DirectoryService.Core.PositionsMatrix.Get;

public class PositionMatrixGetHandler : ICommandHandler<List<PositionMatrixDto>, PositionMatrixGetCommand>
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
    public async Task<Result<List<PositionMatrixDto>, Errors>> ExecuteAsync(PositionMatrixGetCommand command, CancellationToken cancellationToken = default)
    {
        var resultPositions = await _positionMatrixRepository.GetAsync(command.Request, cancellationToken).ConfigureAwait(false);
        if (resultPositions.IsFailure)
        {
            _logger.LogError("Error _positionMatrixRepository.GetAsync");
            return GeneralErrors.Failure("ошибка чтения списка должностей").ToErrors();
        }

        return resultPositions.Value
            .Select(ToDto)
            .ToList();
    }

    private static PositionMatrixDto ToDto(PositionMatrix positionMatrix) =>
        new(
            positionMatrix.Id.Value,
            positionMatrix.Name.Value,
            positionMatrix.Slug.Value,
            positionMatrix.PathSlug?.Value,
            positionMatrix.ParentId?.Value);
}
