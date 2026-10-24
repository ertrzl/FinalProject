using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    public partial class AddGroupPosts : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GroupId",
                table: "Posts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Posts_GroupId",
                table: "Posts",
                column: "GroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Posts_Groups_GroupId",
                table: "Posts",
                column: "GroupId",
                principalTable: "Groups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Posts_Groups_GroupId",
                table: "Posts");

            migrationBuilder.DropIndex(
                name: "IX_Posts_GroupId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Posts");
        }
    }
}
