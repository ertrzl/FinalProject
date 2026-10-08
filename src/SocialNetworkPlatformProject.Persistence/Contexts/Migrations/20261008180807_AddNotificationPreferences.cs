using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnComments",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);   // everybody who is already here keeps getting notified

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnFriendRequests",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "NotifyOnLikes",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NotifyOnComments",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NotifyOnFriendRequests",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "NotifyOnLikes",
                table: "AspNetUsers");
        }
    }
}
