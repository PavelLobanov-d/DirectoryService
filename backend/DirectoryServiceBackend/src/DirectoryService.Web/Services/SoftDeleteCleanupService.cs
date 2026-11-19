using Dapper;
using DirectoryService.Core.Database;
using Microsoft.Extensions.Options;

namespace DirectoryService.Web.Services;

public sealed class SoftDeleteCleanupService : BackgroundService
{
    private const string DeleteChiefPositionsSql = """
        DELETE FROM department_chiefpositions
        WHERE department_id IN (
            SELECT id FROM departments WHERE deleted_at < @cutoff
        )
        OR positionmatrix_id IN (
            SELECT id FROM positionsmatrix WHERE deleted_at < @cutoff
        );
        """;

    private const string DeleteDepartmentPositionsSql = """
        DELETE FROM department_positions
        WHERE department_id IN (
            SELECT id FROM departments WHERE deleted_at < @cutoff
        )
        OR position_id IN (
            SELECT id FROM positionsmatrix WHERE deleted_at < @cutoff
        );
        """;

    private const string DeleteDepartmentLocationsSql = """
        DELETE FROM department_locations
        WHERE department_id IN (
            SELECT id FROM departments WHERE deleted_at < @cutoff
        )
        OR location_id IN (
            SELECT id FROM locations WHERE deleted_at < @cutoff
        );
        """;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<SoftDeleteCleanupOptions> _options;
    private readonly ILogger<SoftDeleteCleanupService> _logger;

    public SoftDeleteCleanupService(
        IServiceScopeFactory scopeFactory,
        IOptions<SoftDeleteCleanupOptions> options,
        ILogger<SoftDeleteCleanupService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        SoftDeleteCleanupOptions options = _options.Value;
        if (!options.Enabled)
        {
            _logger.LogInformation("Soft delete cleanup is disabled");
            return;
        }

        if (options.RetentionDays <= 0 || options.IntervalMinutes <= 0)
        {
            _logger.LogError(
                "Soft delete cleanup is misconfigured: RetentionDays={RetentionDays}, IntervalMinutes={IntervalMinutes}",
                options.RetentionDays,
                options.IntervalMinutes);
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(options.IntervalMinutes));

        do
        {
            await CleanupAsync(options.RetentionDays, stoppingToken).ConfigureAwait(false);
        }
        while (await timer.WaitForNextTickAsync(stoppingToken).ConfigureAwait(false));
    }

    private async Task CleanupAsync(int retentionDays, CancellationToken cancellationToken)
    {
        DateTime cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        _logger.LogInformation("Starting soft delete cleanup with cutoff {Cutoff}", cutoff);

        try
        {
            using IServiceScope scope = _scopeFactory.CreateScope();
            IDbConnectionFactory connectionFactory = scope.ServiceProvider
                .GetRequiredService<IDbConnectionFactory>();

            using var connection = await connectionFactory
                .CreateConnectionAsync(cancellationToken)
                .ConfigureAwait(false);

            using var transaction = connection.BeginTransaction();

            var parameters = new { cutoff };

            int chiefPositionsDeleted = await connection
                .ExecuteAsync(DeleteChiefPositionsSql, parameters, transaction)
                .ConfigureAwait(false);
            int departmentPositionsDeleted = await connection
                .ExecuteAsync(DeleteDepartmentPositionsSql, parameters, transaction)
                .ConfigureAwait(false);
            int departmentLocationsDeleted = await connection
                .ExecuteAsync(DeleteDepartmentLocationsSql, parameters, transaction)
                .ConfigureAwait(false);

            int positionsDeleted = await connection
                .ExecuteAsync(
                    "DELETE FROM positionsmatrix WHERE deleted_at < @cutoff;",
                    parameters,
                    transaction)
                .ConfigureAwait(false);
            int departmentsDeleted = await connection
                .ExecuteAsync(
                    "DELETE FROM departments WHERE deleted_at < @cutoff;",
                    parameters,
                    transaction)
                .ConfigureAwait(false);
            int locationsDeleted = await connection
                .ExecuteAsync(
                    "DELETE FROM locations WHERE deleted_at < @cutoff;",
                    parameters,
                    transaction)
                .ConfigureAwait(false);

            transaction.Commit();

            _logger.LogInformation(
                "Soft delete cleanup removed departments={Departments}, positions={Positions}, locations={Locations}, " +
                "chieflinks={ChiefLinks}, departmentpositions={DepartmentPositions}, departmentlocations={DepartmentLocations}",
                departmentsDeleted,
                positionsDeleted,
                locationsDeleted,
                chiefPositionsDeleted,
                departmentPositionsDeleted,
                departmentLocationsDeleted);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Soft delete cleanup failed");
        }
    }
}
