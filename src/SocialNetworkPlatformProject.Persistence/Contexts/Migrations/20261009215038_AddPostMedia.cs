using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class AddPostMedia : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostMedia",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PostId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    MediaType = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostMedia", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PostMedia_Posts_PostId",
                        column: x => x.PostId,
                        principalTable: "Posts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PostMedia_PostId_SortOrder",
                table: "PostMedia",
                columns: new[] { "PostId", "SortOrder" });

            // Every existing post with a photo or video keeps it: it becomes the first (and only) media of the post.
            migrationBuilder.Sql(@"
                INSERT INTO PostMedia (Id, PostId, Url, MediaType, SortOrder, CreatedAt)
                SELECT NEWID(), Id, MediaUrl, MediaType, 0, CreatedAt
                FROM Posts
                WHERE MediaUrl IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "MediaUrl",
                table: "Posts");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MediaType",
                table: "Posts",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "MediaUrl",
                table: "Posts",
                type: "nvarchar(max)",
                nullable: true);

            // Only one media fits in a post again: the first one stays.
            migrationBuilder.Sql(@"
                UPDATE p
                SET p.MediaUrl = m.Url, p.MediaType = m.MediaType
                FROM Posts p
                JOIN PostMedia m ON m.PostId = p.Id AND m.SortOrder = 0");

            migrationBuilder.DropTable(
                name: "PostMedia");
        }
    }
}
