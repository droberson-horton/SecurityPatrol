using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityPatrol.Web.Migrations
{
    public partial class AddFloorPlanPinRadius : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PinRadius",
                table: "FloorPlans",
                type: "int",
                nullable: false,
                defaultValue: 18);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PinRadius",
                table: "FloorPlans");
        }
    }
}
