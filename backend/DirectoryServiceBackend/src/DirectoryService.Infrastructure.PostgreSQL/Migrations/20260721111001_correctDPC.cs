using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class correctDPC : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_department_chiefpositions_positionsmatrix_PositionMatrixId1",
                table: "department_chiefpositions");

            migrationBuilder.DropIndex(
                name: "IX_department_chiefpositions_PositionMatrixId1",
                table: "department_chiefpositions");

            migrationBuilder.DropColumn(
                name: "PositionMatrixId1",
                table: "department_chiefpositions");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PositionMatrixId1",
                table: "department_chiefpositions",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_department_chiefpositions_PositionMatrixId1",
                table: "department_chiefpositions",
                column: "PositionMatrixId1");

            migrationBuilder.AddForeignKey(
                name: "FK_department_chiefpositions_positionsmatrix_PositionMatrixId1",
                table: "department_chiefpositions",
                column: "PositionMatrixId1",
                principalTable: "positionsmatrix",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
