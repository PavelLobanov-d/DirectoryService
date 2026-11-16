using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class UniqueDepartmentLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_department_positions_department_id",
                table: "department_positions");

            migrationBuilder.DropIndex(
                name: "IX_department_locations_department_id",
                table: "department_locations");

            migrationBuilder.CreateIndex(
                name: "UX_department_positions_department_position",
                table: "department_positions",
                columns: new[] { "department_id", "position_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_department_locations_department_location",
                table: "department_locations",
                columns: new[] { "department_id", "location_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "UX_department_positions_department_position",
                table: "department_positions");

            migrationBuilder.DropIndex(
                name: "UX_department_locations_department_location",
                table: "department_locations");

            migrationBuilder.CreateIndex(
                name: "IX_department_positions_department_id",
                table: "department_positions",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_department_locations_department_id",
                table: "department_locations",
                column: "department_id");
        }
    }
}
