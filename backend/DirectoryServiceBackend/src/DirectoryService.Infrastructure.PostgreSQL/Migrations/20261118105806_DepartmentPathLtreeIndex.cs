using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class DepartmentPathLtreeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:ltree", ",,");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_departments_pathslugfull",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "pathslugfull",
                table: "departments");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:ltree", ",,");
        }
    }
}
