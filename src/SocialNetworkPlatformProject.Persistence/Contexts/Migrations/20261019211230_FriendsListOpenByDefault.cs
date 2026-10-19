using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class FriendsListOpenByDefault : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The switch was off for everybody (it only existed as a default). Open it for existing accounts too, so friends of
            // friends can be found; each person can still close it in settings.
            migrationBuilder.Sql("UPDATE AspNetUsers SET ShowFriendsList = 1");
        

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
