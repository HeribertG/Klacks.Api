using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCounterRule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "counter_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<int>(type: "integer", nullable: false),
                    period = table.Column<int>(type: "integer", nullable: false),
                    threshold = table.Column<int>(type: "integer", nullable: false),
                    hours_threshold = table.Column<decimal>(type: "numeric", nullable: true),
                    scheduling_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
                    import_source_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    import_content_hash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
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
                    table.PrimaryKey("pk_counter_rule", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_counter_rule_import_source_key",
                table: "counter_rule",
                column: "import_source_key",
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "counter_rule");
        }
    }
}
