using CSharpFunctionalExtensions;
using DirectoryService.Core.Database;
using DirectoryService.Shared;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;

namespace DirectoryService.Infrastructure.PostgreSQL.Database;

public sealed class TransactionScope : ITransactionScope
{
    private readonly IDbContextTransaction _transaction;
    private readonly ILogger<TransactionScope> _logger;
    private bool _disposed;

    public TransactionScope(IDbContextTransaction transaction, ILogger<TransactionScope> logger)
    {
        _transaction = transaction;
        _logger = logger;
        _disposed = false;
    }

    public async Task<UnitResult<Error>> CommitAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _transaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to commit transaction");
            return Error.Failure("transaction.commit.failed", "Failed to commit transaction");
        }
    }

    public async Task<UnitResult<Error>> RollBackAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _transaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
            return UnitResult.Success<Error>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback transaction");
            return Error.Failure("transaction.rollback.failed", "Failed to rollback transaction");
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        try
        {
            _transaction.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispose transaction");
        }
        finally
        {
            _disposed = true;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            await _transaction.DisposeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to dispose transaction asynchronously");
        }
        finally
        {
            _disposed = true;
        }
    }
}
