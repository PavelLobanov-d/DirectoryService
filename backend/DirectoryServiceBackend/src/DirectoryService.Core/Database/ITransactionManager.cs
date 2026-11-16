using CSharpFunctionalExtensions;
using DirectoryService.Shared;

namespace DirectoryService.Core.Database;

public interface ITransactionManager
{
    public Task<Result<ITransactionScope, Error>> BeginTransactionAsync(CancellationToken cancellationToken);
    public Task<UnitResult<Error>> SaveChangesAsync(CancellationToken cancellationToken);
}