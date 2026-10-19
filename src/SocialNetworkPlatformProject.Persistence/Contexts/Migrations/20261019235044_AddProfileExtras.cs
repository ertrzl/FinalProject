using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostMentions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MentionedUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostMentions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostMentions_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StoryHighlights",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    CoverItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryHighlights", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StoryHighlightItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HighlightId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MediaUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MediaType = table.Column<int>(type: "int", nullable: false),
                    MusicTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MusicArtist = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    MusicUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    MusicArtworkUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SourceStoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoryCreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StoryHighlightItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StoryHighlightItems_StoryHighlights_HighlightId",
                        column: x => x.HighlightId,
                        principalTable: "StoryHighlights",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostMentions_MentionedUserId",
                table: "PostMentions",
                column: "MentionedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PostMentions_PostId_MentionedUserId",
                table: "PostMentions",
                columns: new[] { "PostId", "MentionedUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoryHighlightItems_HighlightId_SourceStoryId",
                table: "StoryHighlightItems",
                columns: new[] { "HighlightId", "SourceStoryId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StoryHighlightItems_MediaUrl",
                table: "StoryHighlightItems",
                column: "MediaUrl");

            migrationBuilder.CreateIndex(
                name: "IX_StoryHighlights_UserId_CreatedAt",
                table: "StoryHighlights",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PostMentions");

            migrationBuilder.DropTable(
                name: "StoryHighlightItems");

            migrationBuilder.DropTable(
                name: "StoryHighlights");
        }
    }
}
