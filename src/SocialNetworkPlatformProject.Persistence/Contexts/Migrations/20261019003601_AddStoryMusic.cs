using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    public partial class AddStoryMusic : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MusicArtist",
                table: "Stories",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MusicArtworkUrl",
                table: "Stories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MusicTitle",
                table: "Stories",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MusicUrl",
                table: "Stories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MusicArtist",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "MusicArtworkUrl",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "MusicTitle",
                table: "Stories");

            migrationBuilder.DropColumn(
                name: "MusicUrl",
                table: "Stories");
        }
    }
}
