using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class SoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "positionsmatrix",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "locations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "departments",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql("""
                CREATE UNIQUE INDEX "UX_locations_name_active"
                    ON locations (name)
                    WHERE deleted_at IS NULL;

                CREATE UNIQUE INDEX "UX_departments_parent_name_active"
                    ON departments (COALESCE(parentid, '00000000-0000-0000-0000-000000000000'::uuid), name)
                    WHERE deleted_at IS NULL;

                CREATE UNIQUE INDEX "UX_departments_parent_slug_active"
                    ON departments (COALESCE(parentid, '00000000-0000-0000-0000-000000000000'::uuid), slug)
                    WHERE deleted_at IS NULL;

                CREATE UNIQUE INDEX "UX_positionsmatrix_parent_name_active"
                    ON positionsmatrix (COALESCE(parentid, '00000000-0000-0000-0000-000000000000'::uuid), name)
                    WHERE deleted_at IS NULL;

                CREATE UNIQUE INDEX "UX_positionsmatrix_parent_slug_active"
                    ON positionsmatrix (COALESCE(parentid, '00000000-0000-0000-0000-000000000000'::uuid), slug)
                    WHERE deleted_at IS NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                DROP INDEX IF EXISTS "UX_locations_name_active";
                DROP INDEX IF EXISTS "UX_departments_parent_name_active";
                DROP INDEX IF EXISTS "UX_departments_parent_slug_active";
                DROP INDEX IF EXISTS "UX_positionsmatrix_parent_name_active";
                DROP INDEX IF EXISTS "UX_positionsmatrix_parent_slug_active";
                """);

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "positionsmatrix");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "locations");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "departments");
        }
    }
}
