using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    public partial class AddEventEndsAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndsAt",
                table: "Events",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("UPDATE [Events] SET [StartsAt] = DATEADD(hour, -3, [StartsAt])");
            migrationBuilder.Sql("UPDATE [Events] SET [EndsAt] = DATEADD(hour, 3, [StartsAt])");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EndsAt",
                table: "Events",
                column: "EndsAt");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_EndsAt",
                table: "Events");

            migrationBuilder.Sql("UPDATE [Events] SET [StartsAt] = DATEADD(hour, 3, [StartsAt])");

            migrationBuilder.DropColumn(
                name: "EndsAt",
                table: "Events");
        }
    }
}
