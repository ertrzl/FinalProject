using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    public partial class AddPostVideoMedia : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ImageUrl",
                table: "Posts",
                newName: "MediaUrl");

            migrationBuilder.AddColumn<int>(
                name: "MediaType",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "Posts");

            migrationBuilder.RenameColumn(
                name: "MediaUrl",
                table: "Posts",
                newName: "ImageUrl");
        }
    }
}
