using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    public partial class FriendsListOpenByDefault : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE AspNetUsers SET ShowFriendsList = 1");
        

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
