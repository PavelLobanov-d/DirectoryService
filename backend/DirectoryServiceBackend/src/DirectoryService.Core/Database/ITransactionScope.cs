using CSharpFunctionalExtensions;
using DirectoryService.Shared;

namespace DirectoryService.Core.Database;

public interface ITransactionScope : IDisposable, IAsyncDisposable
{
    Task<UnitResult<Error>> CommitAsync(CancellationToken cancellationToken = default);
    Task<UnitResult<Error>> RollBackAsync(CancellationToken cancellationToken = default);
}