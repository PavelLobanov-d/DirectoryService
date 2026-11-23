using Microsoft.Extensions.Logging;
using Npgsql;
using System.Data;

namespace DirectoryService.Infrastructure.PostgreSQL.Database;

public class NpgsqlConnectionFactory : IDisposable, IAsyncDisposable, IDbConnectionFactory
{
    private readonly NpgsqlDataSource _dataSource;
    public NpgsqlConnectionFactory(string connectionString)
    {
        var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.UseLoggerFactory(CreateloggerFactory());
        _dataSource = dataSourceBuilder.Build();
    }
    public async Task<IDbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default)
    {
        return await _dataSource.OpenConnectionAsync().ConfigureAwait(false);
    }
    void IDisposable.Dispose() => ((IDisposable)_dataSource).Dispose();
    ValueTask IAsyncDisposable.DisposeAsync() => ((IAsyncDisposable)_dataSource).DisposeAsync();
    private static ILoggerFactory CreateloggerFactory() =>
        LoggerFactory.Create(builder => builder.AddConsole());
}
