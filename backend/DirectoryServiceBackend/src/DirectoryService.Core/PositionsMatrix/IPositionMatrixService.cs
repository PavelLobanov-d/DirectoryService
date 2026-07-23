using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Contracts.PositionsMatrix;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;

namespace DirectoryService.Core.PositionsMatrix;

public interface IPositionMatrixService
{
    public Task<Result<Guid, Errors>> CreateAsync(CreatePositionMatrixDto positionMatrixDto, CancellationToken cancellationToken = default);
    public Task<Result<bool, Error>> DeleteAsync(Guid positionMatrixId, CancellationToken cancellationToken = default);
    public Task<Result<PositionMatrix?, Error>> GetByIdAsync(Guid positionMatrixId, CancellationToken cancellationToken = default);
    public Task<Result<List<PositionMatrix>, Error>> GetByParentIdAsync(Guid parentPositionMatrixId, CancellationToken cancellationToken = default);
    public Task<Result<List<PositionMatrix>, Error>> GetAsync(SelectDto request, CancellationToken cancellationToken = default);
    public Task<Result<bool, Errors>> UpdateAsync(
        Guid positionMatrixId,
        UpdatePositionMatrixDto? positionDto,
        CancellationToken cancellationToken = default);
    public Task<Result<bool, Errors>> UpdateAsync(
        PositionMatrix positionMatrix,
        UpdatePositionMatrixDto? positionDto,
        CancellationToken cancellationToken = default);
    public Task<Result<bool, Error>> ChangeParentAsync(
        Guid positionMatrixId,
        Guid newParentPositionId,
        CancellationToken cancellationToken = default);
    public Task<Result<PositionMatrix?, Error>> FindChildByIdAsync(
        PositionMatrix positionMatrix,
        PositionMatrixId findPositionId,
        CancellationToken cancellationToken = default);
    public Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default);
}
