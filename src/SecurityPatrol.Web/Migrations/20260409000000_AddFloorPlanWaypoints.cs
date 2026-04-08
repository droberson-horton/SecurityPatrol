using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityPatrol.Web.Migrations
{
    public partial class AddFloorPlanWaypoints : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FloorPlanWaypoints",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FloorPlanId = table.Column<int>(type: "int", nullable: false),
                    FromLocationId = table.Column<int>(type: "int", nullable: false),
                    ToLocationId = table.Column<int>(type: "int", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    X = table.Column<double>(type: "float", nullable: false),
                    Y = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FloorPlanWaypoints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FloorPlanWaypoints_FloorPlans_FloorPlanId",
                        column: x => x.FloorPlanId,
                        principalTable: "FloorPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FloorPlanWaypoints_FloorPlanId_FromLocationId_ToLocationId_OrderIndex",
                table: "FloorPlanWaypoints",
                columns: new[] { "FloorPlanId", "FromLocationId", "ToLocationId", "OrderIndex" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FloorPlanWaypoints");
        }
    }
}
