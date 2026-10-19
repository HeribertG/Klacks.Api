using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddErpImportFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "import_file_id",
                table: "erp_import_exception",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "erp_import_file",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    drop_point_id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_system_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    storage_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    file_name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    sha256 = table.Column<string>(type: "character(64)", fixedLength: true, maxLength: 64, nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    uploaded_by = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    received_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    heartbeat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    finished_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    attempt = table.Column<int>(type: "integer", nullable: false),
                    claimed_by = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    total_orders = table.Column<int>(type: "integer", nullable: true),
                    processed_orders = table.Column<int>(type: "integer", nullable: false),
                    new_count = table.Column<int>(type: "integer", nullable: false),
                    updated_count = table.Column<int>(type: "integer", nullable: false),
                    unchanged_count = table.Column<int>(type: "integer", nullable: false),
                    superseded_count = table.Column<int>(type: "integer", nullable: false),
                    rejected_count = table.Column<int>(type: "integer", nullable: false),
                    failure_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    duplicate_of_file_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_erp_import_file", x => x.id);
                    table.ForeignKey(
                        name: "fk_erp_import_file_erp_drop_points_drop_point_id",
                        column: x => x.drop_point_id,
                        principalTable: "erp_drop_points",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_erp_import_file_erp_import_file_duplicate_of_file_id",
                        column: x => x.duplicate_of_file_id,
                        principalTable: "erp_import_file",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "ix_erp_import_exception_import_file_id",
                table: "erp_import_exception",
                column: "import_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_erp_import_file_drop_point_id_received_at",
                table: "erp_import_file",
                columns: new[] { "drop_point_id", "received_at" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_erp_import_file_drop_point_id_sha256",
                table: "erp_import_file",
                columns: new[] { "drop_point_id", "sha256" });

            migrationBuilder.CreateIndex(
                name: "ix_erp_import_file_drop_point_id_status",
                table: "erp_import_file",
                columns: new[] { "drop_point_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_erp_import_file_duplicate_of_file_id",
                table: "erp_import_file",
                column: "duplicate_of_file_id");

            migrationBuilder.CreateIndex(
                name: "ix_erp_import_file_storage_key",
                table: "erp_import_file",
                column: "storage_key",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.AddForeignKey(
                name: "fk_erp_import_exception_erp_import_files_import_file_id",
                table: "erp_import_exception",
                column: "import_file_id",
                principalTable: "erp_import_file",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "fk_erp_import_exception_erp_import_files_import_file_id",
                table: "erp_import_exception");

            migrationBuilder.DropTable(
                name: "erp_import_file");

            migrationBuilder.DropIndex(
                name: "ix_erp_import_exception_import_file_id",
                table: "erp_import_exception");

            migrationBuilder.DropColumn(
                name: "import_file_id",
                table: "erp_import_exception");
        }
    }
}
