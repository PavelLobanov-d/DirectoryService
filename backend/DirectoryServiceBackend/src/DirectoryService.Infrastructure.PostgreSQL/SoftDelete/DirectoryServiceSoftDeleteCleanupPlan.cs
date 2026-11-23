using Shared.Framework.SoftDelete;

namespace DirectoryService.Infrastructure.PostgreSQL.SoftDelete;

public static class DirectoryServiceSoftDeleteCleanupPlan
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

    private const string DeletePositionsSql = """
        DELETE FROM positionsmatrix WHERE deleted_at < @cutoff;
        """;

    private const string DeleteDepartmentsSql = """
        DELETE FROM departments WHERE deleted_at < @cutoff;
        """;

    private const string DeleteLocationsSql = """
        DELETE FROM locations WHERE deleted_at < @cutoff;
        """;

    public static SoftDeleteCleanupPlan Create() =>
        new(
        [
            new SoftDeleteCleanupStep("department_chiefpositions", DeleteChiefPositionsSql),
            new SoftDeleteCleanupStep("department_positions", DeleteDepartmentPositionsSql),
            new SoftDeleteCleanupStep("department_locations", DeleteDepartmentLocationsSql),
            new SoftDeleteCleanupStep("positionsmatrix", DeletePositionsSql),
            new SoftDeleteCleanupStep("departments", DeleteDepartmentsSql),
            new SoftDeleteCleanupStep("locations", DeleteLocationsSql),
        ]);
}
