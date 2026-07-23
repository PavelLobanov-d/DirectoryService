using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class correctDPC4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_department_chiefpositions_positionsmatrix_positionmatrix_id",
                table: "department_chiefpositions");

            migrationBuilder.AddForeignKey(
                name: "FK_department_chiefpositions_positionsmatrix_positionmatrix_id",
                table: "department_chiefpositions",
                column: "positionmatrix_id",
                principalTable: "positionsmatrix",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_department_chiefpositions_positionsmatrix_positionmatrix_id",
                table: "department_chiefpositions");

            migrationBuilder.AddForeignKey(
                name: "FK_department_chiefpositions_positionsmatrix_positionmatrix_id",
                table: "department_chiefpositions",
                column: "positionmatrix_id",
                principalTable: "positionsmatrix",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
