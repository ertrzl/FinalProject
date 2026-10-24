using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    public partial class AddSessions : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DeviceName",
                table: "RefreshTokens",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "RefreshTokens",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SessionId",
                table: "RefreshTokens",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTime>(
                name: "SessionStartedAt",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.Sql("UPDATE RefreshTokens SET SessionId = Id, SessionStartedAt = CreatedAt;");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_SessionId",
                table: "RefreshTokens",
                column: "SessionId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_SessionId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "DeviceName",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "SessionStartedAt",
                table: "RefreshTokens");
        }
    }
}
