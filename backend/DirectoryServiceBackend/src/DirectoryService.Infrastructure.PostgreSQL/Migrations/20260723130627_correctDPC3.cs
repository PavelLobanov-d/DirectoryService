using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class correctDPC3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_department_chiefpositions_departments_department_id",
                table: "department_chiefpositions");

            migrationBuilder.AddForeignKey(
                name: "FK_department_chiefpositions_departments_department_id",
                table: "department_chiefpositions",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_department_chiefpositions_departments_department_id",
                table: "department_chiefpositions");

            migrationBuilder.AddForeignKey(
                name: "FK_department_chiefpositions_departments_department_id",
                table: "department_chiefpositions",
                column: "department_id",
                principalTable: "departments",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
