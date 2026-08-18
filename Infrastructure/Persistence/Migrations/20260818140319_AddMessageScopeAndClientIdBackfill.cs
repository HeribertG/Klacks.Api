using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageScopeAndClientIdBackfill : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "ix_messenger_contact_type_value",
                table: "messenger_contact",
                columns: new[] { "type", "value" });

            // Backfill: PersistOutboundAsync never set client_id on outbound messages before this
            // change, so every historical outbound client message would otherwise misclassify as
            // scope=Internal once the new filter ships. Mirrors MessengerContactRepository's
            // GetByTypeAndValueAsync exactly (type+value match, not deleted, oldest contact on ties).
            migrationBuilder.Sql(@"
                UPDATE messages AS m
                SET client_id = matched.client_id
                FROM (
                    SELECT DISTINCT ON (mp.id, mc.value)
                        mp.id AS provider_id,
                        mc.value,
                        mc.client_id
                    FROM messaging_providers AS mp
                    INNER JOIN messenger_contact AS mc
                        ON mc.is_deleted = false
                        AND mc.type = CASE lower(mp.provider_type)
                            WHEN 'telegram' THEN 1
                            WHEN 'whatsapp' THEN 2
                            WHEN 'signal' THEN 3
                            WHEN 'threema' THEN 4
                            WHEN 'viber' THEN 5
                            WHEN 'line' THEN 6
                            WHEN 'kakaotalk' THEN 7
                            WHEN 'wechat' THEN 8
                            WHEN 'zalo' THEN 9
                            WHEN 'microsoftteams' THEN 10
                            WHEN 'slack' THEN 11
                            WHEN 'sms' THEN 12
                        END
                    ORDER BY mp.id, mc.value, mc.create_time ASC
                ) AS matched
                WHERE m.provider_id = matched.provider_id
                  AND m.recipient = matched.value
                  AND m.client_id IS NULL
                  AND m.direction = 1;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_messenger_contact_type_value",
                table: "messenger_contact");
        }
    }
}
