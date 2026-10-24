using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    public partial class AddMarketplaceExtras : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ListingId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PreviousAmount",
                table: "Notifications",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Condition",
                table: "MarketplaceListings",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                table: "MarketplaceListings",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "DATEADD(day, 30, GETUTCDATE())");

            migrationBuilder.AddColumn<DateTime>(
                name: "FeaturedUntil",
                table: "MarketplaceListings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RenewedAt",
                table: "MarketplaceListings",
                type: "datetime2",
                nullable: true);

            migrationBuilder.Sql("UPDATE MarketplaceListings SET ExpiresAt = CASE WHEN DATEADD(day, 30, CreatedAt) > DATEADD(day, 7, GETUTCDATE()) THEN DATEADD(day, 30, CreatedAt) ELSE DATEADD(day, 7, GETUTCDATE()) END");

            migrationBuilder.CreateTable(
                name: "ListingBoosts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SellerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingBoosts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketplaceListings_Category_Status_ExpiresAt",
                table: "MarketplaceListings",
                columns: new[] { "Category", "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ListingBoosts_SellerId_CreatedAt",
                table: "ListingBoosts",
                columns: new[] { "SellerId", "CreatedAt" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingBoosts");

            migrationBuilder.DropIndex(
                name: "IX_MarketplaceListings_Category_Status_ExpiresAt",
                table: "MarketplaceListings");

            migrationBuilder.DropColumn(
                name: "ListingId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "PreviousAmount",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Condition",
                table: "MarketplaceListings");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "MarketplaceListings");

            migrationBuilder.DropColumn(
                name: "FeaturedUntil",
                table: "MarketplaceListings");

            migrationBuilder.DropColumn(
                name: "RenewedAt",
                table: "MarketplaceListings");
        }
    }
}
