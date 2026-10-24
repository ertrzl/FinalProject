using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SocialNetworkPlatformProject.Persistence.Contexts.Migrations
{
    public partial class AddConversationDirectKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DirectKey",
                table: "Conversations",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.Sql(
                @"UPDATE c
                  SET DirectKey = pairs.LowId + ':' + pairs.HighId
                  FROM Conversations c
                  JOIN (
                      SELECT ConversationId,
                             MIN(LOWER(CONVERT(varchar(36), UserId)) COLLATE Latin1_General_100_BIN2) AS LowId,
                             MAX(LOWER(CONVERT(varchar(36), UserId)) COLLATE Latin1_General_100_BIN2) AS HighId,
                             COUNT(*) AS Participants
                      FROM ConversationParticipants
                      GROUP BY ConversationId
                  ) pairs ON pairs.ConversationId = c.Id
                  WHERE pairs.Participants = 2 AND pairs.LowId <> pairs.HighId");

            migrationBuilder.Sql(
                @"WITH ranked AS (
                      SELECT Id, DirectKey, ROW_NUMBER() OVER (PARTITION BY DirectKey ORDER BY CreatedAt, Id) AS Position
                      FROM Conversations
                      WHERE DirectKey IS NOT NULL
                  )
                  UPDATE m
                  SET ConversationId = keeper.Id
                  FROM Messages m
                  JOIN ranked dup ON dup.Id = m.ConversationId AND dup.Position > 1
                  JOIN ranked keeper ON keeper.DirectKey = dup.DirectKey AND keeper.Position = 1");

            migrationBuilder.Sql(
                @"WITH ranked AS (
                      SELECT Id, ROW_NUMBER() OVER (PARTITION BY DirectKey ORDER BY CreatedAt, Id) AS Position
                      FROM Conversations
                      WHERE DirectKey IS NOT NULL
                  )
                  DELETE FROM Conversations WHERE Id IN (SELECT Id FROM ranked WHERE Position > 1)");

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_DirectKey",
                table: "Conversations",
                column: "DirectKey",
                unique: true,
                filter: "[DirectKey] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Conversations_DirectKey",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "DirectKey",
                table: "Conversations");
        }
    }
}
