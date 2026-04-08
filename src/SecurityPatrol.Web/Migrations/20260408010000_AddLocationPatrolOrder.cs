using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityPatrol.Web.Migrations
{
    public partial class AddLocationPatrolOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PatrolOrder",
                table: "Locations",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PatrolOrder",
                table: "Locations");
        }
    }
}
