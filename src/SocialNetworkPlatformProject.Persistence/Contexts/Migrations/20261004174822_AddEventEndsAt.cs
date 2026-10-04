using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class AddEventEndsAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EndsAt",
                table: "Events",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            // Existing events were saved as the local wall-clock time the organizer typed (Türkiye, UTC+3 all year).
            // From now on StartsAt/EndsAt are UTC, so convert old rows, then give them the default 3-hour duration.
            migrationBuilder.Sql("UPDATE [Events] SET [StartsAt] = DATEADD(hour, -3, [StartsAt])");
            migrationBuilder.Sql("UPDATE [Events] SET [EndsAt] = DATEADD(hour, 3, [StartsAt])");

            migrationBuilder.CreateIndex(
                name: "IX_Events_EndsAt",
                table: "Events",
                column: "EndsAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Events_EndsAt",
                table: "Events");

            // Back to local wall-clock time.
            migrationBuilder.Sql("UPDATE [Events] SET [StartsAt] = DATEADD(hour, 3, [StartsAt])");

            migrationBuilder.DropColumn(
                name: "EndsAt",
                table: "Events");
        }
    }
}
