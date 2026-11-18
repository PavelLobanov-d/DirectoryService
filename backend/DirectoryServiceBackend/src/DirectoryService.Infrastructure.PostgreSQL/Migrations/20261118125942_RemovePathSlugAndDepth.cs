using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class RemovePathSlugAndDepth : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_departments_pathslugfull",
                table: "departments");

            migrationBuilder.AddColumn<string>(
                name: "path",
                table: "departments",
                type: "ltree",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE departments
                SET path = (COALESCE(pathslug::text || '.', '') || slug::text)::ltree;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "path",
                table: "departments",
                type: "ltree",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "ltree",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "pathslugfull",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "pathslug",
                table: "departments");

            migrationBuilder.CreateIndex(
                name: "IX_departments_path",
                table: "departments",
                column: "path")
                .Annotation("Npgsql:IndexMethod", "gist");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_departments_path",
                table: "departments");

            migrationBuilder.AddColumn<string>(
                name: "pathslug",
                table: "departments",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE departments
                SET pathslug = CASE
                    WHEN nlevel(path) > 1
                        THEN subpath(path, 0, nlevel(path) - 1)::text
                    ELSE NULL
                END;
                """);

            migrationBuilder.DropColumn(
                name: "path",
                table: "departments");

            migrationBuilder.AddColumn<string>(
                name: "pathslugfull",
                table: "departments",
                type: "ltree",
                nullable: false,
                computedColumnSql: "(COALESCE(pathslug::text || '.', '') || slug::text)::ltree",
                stored: true);

            migrationBuilder.CreateIndex(
                name: "IX_departments_pathslugfull",
                table: "departments",
                column: "pathslugfull")
                .Annotation("Npgsql:IndexMethod", "gist");
        }
    }
}
