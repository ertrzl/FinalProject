using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    /// <inheritdoc />
    public partial class AuditFixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {            // One-off clean-up of rows the older code left behind (a new database has none of them):
            // - the same person's repeated reports of the same thing (the oldest is kept), which the unique index below refuses;
            // - join requests, invitations and memberships of accounts that no longer exist (they showed up as blank names
            //   in a group's request list and could be "approved").
            migrationBuilder.Sql(@"
WITH numbered AS (
    SELECT Id, ROW_NUMBER() OVER (PARTITION BY ReporterId, TargetType, TargetId ORDER BY CreatedAt, Id) AS Position
    FROM Reports)
DELETE FROM numbered WHERE Position > 1;");
            migrationBuilder.Sql("DELETE FROM GroupJoinRequests WHERE UserId NOT IN (SELECT Id FROM AspNetUsers);");
            migrationBuilder.Sql("DELETE FROM GroupInvites WHERE UserId NOT IN (SELECT Id FROM AspNetUsers) OR InvitedByUserId NOT IN (SELECT Id FROM AspNetUsers);");
            migrationBuilder.Sql("DELETE FROM GroupMembers WHERE UserId NOT IN (SELECT Id FROM AspNetUsers);");


            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId_TargetType_TargetId",
                table: "Reports");

            migrationBuilder.AlterColumn<string>(
                name: "MediaUrl",
                table: "Stories",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "MediaUrl",
                table: "Messages",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CoverImageUrl",
                table: "Events",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Stories_MediaUrl",
                table: "Stories",
                column: "MediaUrl");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId_TargetType_TargetId",
                table: "Reports",
                columns: new[] { "ReporterId", "TargetType", "TargetId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PostMedia_Url",
                table: "PostMedia",
                column: "Url");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_MediaUrl",
                table: "Messages",
                column: "MediaUrl",
                filter: "[MediaUrl] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Events_CoverImageUrl",
                table: "Events",
                column: "CoverImageUrl",
                filter: "[CoverImageUrl] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Stories_MediaUrl",
                table: "Stories");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReporterId_TargetType_TargetId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_PostMedia_Url",
                table: "PostMedia");

            migrationBuilder.DropIndex(
                name: "IX_Messages_MediaUrl",
                table: "Messages");

            migrationBuilder.DropIndex(
                name: "IX_Events_CoverImageUrl",
                table: "Events");

            migrationBuilder.AlterColumn<string>(
                name: "MediaUrl",
                table: "Stories",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "MediaUrl",
                table: "Messages",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CoverImageUrl",
                table: "Events",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReporterId_TargetType_TargetId",
                table: "Reports",
                columns: new[] { "ReporterId", "TargetType", "TargetId" });
        }
    }
}
