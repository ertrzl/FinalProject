using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class AddListingImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListingImages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ListingId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingImages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ListingImages_MarketplaceListings_ListingId",
                        column: x => x.ListingId,
                        principalTable: "MarketplaceListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingImages_ListingId_SortOrder",
                table: "ListingImages",
                columns: new[] { "ListingId", "SortOrder" });

            // Existing single photos become each listing's first (cover) photo before the old column goes away.
            migrationBuilder.Sql(
                "INSERT INTO ListingImages (Id, ListingId, Url, SortOrder, CreatedAt) " +
                "SELECT NEWID(), Id, ImageUrl, 0, SYSUTCDATETIME() FROM MarketplaceListings WHERE ImageUrl IS NOT NULL");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "MarketplaceListings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "MarketplaceListings",
                type: "nvarchar(max)",
                nullable: true);

            // Only the cover photo survives a rollback to the single-photo schema.
            migrationBuilder.Sql(
                "UPDATE l SET l.ImageUrl = i.Url FROM MarketplaceListings l " +
                "JOIN ListingImages i ON i.ListingId = l.Id AND i.SortOrder = 0");

            migrationBuilder.DropTable(
                name: "ListingImages");
        }
    }
}
