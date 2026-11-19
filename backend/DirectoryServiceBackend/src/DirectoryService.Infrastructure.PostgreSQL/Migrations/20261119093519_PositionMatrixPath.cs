using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class PositionMatrixPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "path",
                table: "positionsmatrix",
                type: "ltree",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE positionsmatrix
                SET path = (COALESCE(pathslug::text || '.', '') || slug::text)::ltree;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "path",
                table: "positionsmatrix",
                type: "ltree",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "ltree",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "pathslug",
                table: "positionsmatrix");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pathslug",
                table: "positionsmatrix",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE positionsmatrix
                SET pathslug = CASE
                    WHEN nlevel(path) > 1
                        THEN subpath(path, 0, nlevel(path) - 1)::text
                    ELSE NULL
                END;
                """);

            migrationBuilder.DropColumn(
                name: "path",
                table: "positionsmatrix");
        }
    }
}
