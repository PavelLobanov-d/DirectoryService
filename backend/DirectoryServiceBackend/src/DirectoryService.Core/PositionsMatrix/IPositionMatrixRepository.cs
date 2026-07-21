using CSharpFunctionalExtensions;
using DirectoryService.Contracts;
using DirectoryService.Domain.PositionsMatrix;
using DirectoryService.Domain.shared;

namespace DirectoryService.Core.PositionsMatrix;

public interface IPositionMatrixRepository
{
    Task<Result<Guid, Error>> AddAsync(PositionMatrix positionMatrix, CancellationToken cancellationToken = default);
    Task<Result<List<PositionMatrix>, Error>> GetAsync(SelectDto request, CancellationToken cancellationToken = default);
    Task<Result<PositionMatrix?, Error>> GetByIdAsync(Guid positionMatrixId, CancellationToken cancellationToken = default);
    Task<Result<List<PositionMatrix>, Error>> GetByParentIdAsync(Guid? parentPositionMatrixId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> HasNameSlugAsync(string name, string slug, Guid? parentId, Guid? excludeId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> UpdateAsync(PositionMatrix positionMatrix, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> DeleteAsync(Guid positionMatrixId, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> DeleteAsync(PositionMatrix positionMatrix, CancellationToken cancellationToken = default);
    Task<Result<bool, Error>> SaveAsync(CancellationToken cancellationToken = default);
}