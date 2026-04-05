using Microsoft.EntityFrameworkCore.Migrations;

  #nullable disable

  namespace SecurityPatrol.Web.Migrations
  {
      public partial class AddBuildingTimeZoneId : Migration
      {
          protected override void Up(MigrationBuilder migrationBuilder)
          {
              migrationBuilder.AddColumn<string>(
                  name: "TimeZoneId",
                  table: "Buildings",
                  type: "nvarchar(100)",
                  maxLength: 100,
                  nullable: true);
          }

          protected override void Down(MigrationBuilder migrationBuilder)
          {
              migrationBuilder.DropColumn(
                  name: "TimeZoneId",
                  table: "Buildings");
          }
      }
  }
  EOF
