using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityPatrol.Web.Migrations
{
    public partial class AddRouteSortOrder : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "PatrolRoutes",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "PatrolRoutes");
        }
    }
}
