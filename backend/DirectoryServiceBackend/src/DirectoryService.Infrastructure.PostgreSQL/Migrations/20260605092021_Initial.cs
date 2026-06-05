using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DirectoryService.Infrastructure.PostgreSQL.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pathslug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    parentid = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department", x => x.id);
                    table.ForeignKey(
                        name: "FK_departments_departments_parentid",
                        column: x => x.parentid,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    address = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_locations", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "positionsmatrix",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    slug = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    pathslug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    parentid = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_positionmatrix", x => x.id);
                    table.ForeignKey(
                        name: "FK_positionsmatrix_positionsmatrix_parentid",
                        column: x => x.parentid,
                        principalTable: "positionsmatrix",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "statistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    objectId = table.Column<Guid>(type: "uuid", nullable: false),
                    objectTypeName = table.Column<string>(type: "text", nullable: false),
                    userName = table.Column<string>(type: "text", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    dateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    parentId = table.Column<Guid>(type: "uuid", nullable: true),
                    parentTypeName = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_statistics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "department_locations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    location_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department_locations", x => x.id);
                    table.ForeignKey(
                        name: "FK_department_locations_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_department_locations_locations_location_id",
                        column: x => x.location_id,
                        principalTable: "locations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "department_chiefpositions",
                columns: table => new
                {
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    positionmatrix_id = table.Column<Guid>(type: "uuid", nullable: false),
                    PositionMatrixId1 = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department_chiefpositions", x => new { x.department_id, x.positionmatrix_id });
                    table.ForeignKey(
                        name: "FK_department_chiefpositions_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_department_chiefpositions_positionsmatrix_PositionMatrixId1",
                        column: x => x.PositionMatrixId1,
                        principalTable: "positionsmatrix",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_department_chiefpositions_positionsmatrix_positionmatrix_id",
                        column: x => x.positionmatrix_id,
                        principalTable: "positionsmatrix",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "department_positions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    department_id = table.Column<Guid>(type: "uuid", nullable: false),
                    position_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department_positions", x => x.id);
                    table.ForeignKey(
                        name: "FK_department_positions_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_department_positions_positionsmatrix_position_id",
                        column: x => x.position_id,
                        principalTable: "positionsmatrix",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_department_chiefpositions_department_id",
                table: "department_chiefpositions",
                column: "department_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_department_chiefpositions_positionmatrix_id",
                table: "department_chiefpositions",
                column: "positionmatrix_id");

            migrationBuilder.CreateIndex(
                name: "IX_department_chiefpositions_PositionMatrixId1",
                table: "department_chiefpositions",
                column: "PositionMatrixId1");

            migrationBuilder.CreateIndex(
                name: "IX_department_locations_department_id",
                table: "department_locations",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_department_locations_location_id",
                table: "department_locations",
                column: "location_id");

            migrationBuilder.CreateIndex(
                name: "IX_department_positions_department_id",
                table: "department_positions",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_department_positions_position_id",
                table: "department_positions",
                column: "position_id");

            migrationBuilder.CreateIndex(
                name: "IX_departments_parentid",
                table: "departments",
                column: "parentid");

            migrationBuilder.CreateIndex(
                name: "IX_positionsmatrix_parentid",
                table: "positionsmatrix",
                column: "parentid");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "department_chiefpositions");

            migrationBuilder.DropTable(
                name: "department_locations");

            migrationBuilder.DropTable(
                name: "department_positions");

            migrationBuilder.DropTable(
                name: "statistics");

            migrationBuilder.DropTable(
                name: "locations");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "positionsmatrix");
        }
    }
}
