using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWorkSoftening : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "work_softening",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    kind = table.Column<byte>(type: "smallint", nullable: false),
                    rule_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    hint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_work_softening", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_work_softening_is_deleted_analyse_token",
                table: "work_softening",
                columns: new[] { "is_deleted", "analyse_token" });

            migrationBuilder.CreateIndex(
                name: "ix_work_softening_is_deleted_client_id_current_date_analyse_to",
                table: "work_softening",
                columns: new[] { "is_deleted", "client_id", "current_date", "analyse_token" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "work_softening");
        }
    }
}
