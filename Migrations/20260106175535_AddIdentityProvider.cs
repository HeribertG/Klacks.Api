using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentityProvider : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "identity_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    use_for_authentication = table.Column<bool>(type: "boolean", nullable: false),
                    use_for_client_import = table.Column<bool>(type: "boolean", nullable: false),
                    host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    port = table.Column<int>(type: "integer", nullable: true),
                    use_ssl = table.Column<bool>(type: "boolean", nullable: false),
                    base_dn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bind_dn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bind_password = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    user_filter = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    client_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    client_secret = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    authorization_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    token_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    user_info_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    scopes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_sync_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sync_count = table.Column<int>(type: "integer", nullable: true),
                    last_sync_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    attribute_mapping = table.Column<string>(type: "jsonb", nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "identity_provider_sync_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    external_dn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_sync_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active_in_source = table.Column<bool>(type: "boolean", nullable: false),
                    sync_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_identity_provider_sync_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_provider_sync_logs_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_identity_provider_sync_logs_identity_providers_identity_pro",
                        column: x => x.identity_provider_id,
                        principalTable: "identity_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_identity_provider_sync_logs_client_id_identity_provider_id",
                table: "identity_provider_sync_logs",
                columns: new[] { "client_id", "identity_provider_id" });

            migrationBuilder.CreateIndex(
                name: "ix_identity_provider_sync_logs_identity_provider_id_external_id",
                table: "identity_provider_sync_logs",
                columns: new[] { "identity_provider_id", "external_id" });

            migrationBuilder.CreateIndex(
                name: "ix_identity_providers_is_deleted_is_enabled_sort_order",
                table: "identity_providers",
                columns: new[] { "is_deleted", "is_enabled", "sort_order" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "identity_provider_sync_logs");

            migrationBuilder.DropTable(
                name: "identity_providers");
        }
    }
}
