using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    public partial class AddMessageMedia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Messages",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Messages");
        }
    }
}
