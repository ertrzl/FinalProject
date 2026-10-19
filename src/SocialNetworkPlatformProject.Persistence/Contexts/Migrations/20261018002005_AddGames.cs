using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class AddGames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GameId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Games",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    InviterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InviteeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PairKey = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    PlayerXId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PlayerOId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Board = table.Column<string>(type: "nvarchar(9)", maxLength: 9, nullable: false),
                    Turn = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: false),
                    Outcome = table.Column<int>(type: "int", nullable: true),
                    WinnerId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Games", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Games_InviteeId",
                table: "Games",
                column: "InviteeId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_InviterId",
                table: "Games",
                column: "InviterId");

            migrationBuilder.CreateIndex(
                name: "IX_Games_PairKey",
                table: "Games",
                column: "PairKey",
                unique: true,
                filter: "[Status] IN (0, 1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Games");

            migrationBuilder.DropColumn(
                name: "GameId",
                table: "Notifications");
        }
    }
}
