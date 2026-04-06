using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityPatrol.Web.Migrations
{
    public partial class AddFloorPlans : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FloorPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FloorId = table.Column<int>(type: "int", nullable: false),
                    OriginalFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StoredFileName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Label = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorPlans_Floors_FloorId",
                        column: x => x.FloorId,
                        principalTable: "Floors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddColumn<int>(
                name: "FloorPlanId",
                table: "Locations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MapX",
                table: "Locations",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MapY",
                table: "Locations",
                type: "float",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlans_FloorId",
                table: "FloorPlans",
                column: "FloorId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_FloorPlanId",
                table: "Locations",
                column: "FloorPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_FloorPlans_FloorPlanId",
                table: "Locations",
                column: "FloorPlanId",
                principalTable: "FloorPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Locations_FloorPlans_FloorPlanId",
                table: "Locations");

            migrationBuilder.DropTable(
                name: "FloorPlans");

            migrationBuilder.DropIndex(
                name: "IX_Locations_FloorPlanId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "FloorPlanId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "MapX",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "MapY",
                table: "Locations");
        }
    }
}
