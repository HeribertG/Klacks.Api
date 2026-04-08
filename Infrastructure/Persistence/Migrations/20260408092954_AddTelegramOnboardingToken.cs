using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramOnboardingToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"ALTER TABLE messages ADD COLUMN IF NOT EXISTS broadcast_id uuid;");
            migrationBuilder.Sql(@"ALTER TABLE messages ADD COLUMN IF NOT EXISTS client_id uuid;");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS messenger_contact (
                    id uuid NOT NULL,
                    client_id uuid NOT NULL,
                    type integer NOT NULL,
                    value character varying(200) NOT NULL,
                    description character varying(200),
                    is_deleted boolean NOT NULL,
                    create_time timestamp with time zone NOT NULL,
                    update_time timestamp with time zone,
                    CONSTRAINT pk_messenger_contact PRIMARY KEY (id)
                );");

            migrationBuilder.CreateTable(
                name: "telegram_onboarding_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    redeemed_chat_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_onboarding_token", x => x.id);
                });

            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_messages_broadcast_id ON messages (broadcast_id);");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_messages_client_id ON messages (client_id);");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_messenger_contact_client_id ON messenger_contact (client_id);");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ix_messenger_contact_client_id_type ON messenger_contact (client_id, type);");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_onboarding_token_client_id_used_at_is_deleted",
                table: "telegram_onboarding_token",
                columns: new[] { "client_id", "used_at", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_telegram_onboarding_token_token",
                table: "telegram_onboarding_token",
                column: "token",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "messenger_contact");

            migrationBuilder.DropTable(
                name: "telegram_onboarding_token");

            migrationBuilder.DropIndex(
                name: "ix_messages_broadcast_id",
                table: "messages");

            migrationBuilder.DropIndex(
                name: "ix_messages_client_id",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "broadcast_id",
                table: "messages");

            migrationBuilder.DropColumn(
                name: "client_id",
                table: "messages");
        }
    }
}
