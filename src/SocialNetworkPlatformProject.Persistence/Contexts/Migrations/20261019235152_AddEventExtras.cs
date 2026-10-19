using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class AddEventExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SharedEventId",
                table: "Posts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Events",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Events",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CheckedInAt",
                table: "EventAttendees",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CheckedInByUserId",
                table: "EventAttendees",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EventPhotos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EventId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploadedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventPhotos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventPhotos_Events_EventId",
                        column: x => x.EventId,
                        principalTable: "Events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_Latitude",
                table: "Events",
                sql: "[Latitude] IS NULL OR ([Latitude] >= -90 AND [Latitude] <= 90)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_Longitude",
                table: "Events",
                sql: "[Longitude] IS NULL OR ([Longitude] >= -180 AND [Longitude] <= 180)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Events_Pin_BothOrNone",
                table: "Events",
                sql: "([Latitude] IS NULL AND [Longitude] IS NULL) OR ([Latitude] IS NOT NULL AND [Longitude] IS NOT NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_EventPhotos_EventId_CreatedAt",
                table: "EventPhotos",
                columns: new[] { "EventId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_EventPhotos_EventId_UploadedByUserId",
                table: "EventPhotos",
                columns: new[] { "EventId", "UploadedByUserId" });

            migrationBuilder.CreateIndex(
                name: "IX_EventPhotos_Url",
                table: "EventPhotos",
                column: "Url");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventPhotos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_Latitude",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_Longitude",
                table: "Events");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Events_Pin_BothOrNone",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "SharedEventId",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Events");

            migrationBuilder.DropColumn(
                name: "CheckedInAt",
                table: "EventAttendees");

            migrationBuilder.DropColumn(
                name: "CheckedInByUserId",
                table: "EventAttendees");
        }
    }
}
