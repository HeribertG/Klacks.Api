using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupItemScenarioToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "analyse_token",
                table: "group_item",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "scenario_source_group_item_id",
                table: "group_item",
                type: "uuid",
                nullable: true);

            // Idempotent, drift-safe index rebuild: the partial unique indexes may be physically absent on
            // some databases (history says applied, but the index can be missing), so DROP IF EXISTS plus a
            // dedup of real (token-null) duplicates before re-creating them with the token-aware filter.
            // The dedup SOFT-deletes the lower-id duplicate (recoverable, matches the app's soft-delete
            // model) — never a hard DELETE on a core table; the partial indexes only cover is_deleted = false.
            migrationBuilder.Sql(@"
                UPDATE group_item SET is_deleted = true, deleted_time = now()
                 WHERE id IN (
                    SELECT gi.id FROM group_item gi
                    JOIN group_item gi2 ON gi.shift_id = gi2.shift_id AND gi.group_id = gi2.group_id
                    WHERE gi.shift_id IS NOT NULL AND gi.is_deleted = false AND gi2.is_deleted = false
                      AND gi.analyse_token IS NULL AND gi2.analyse_token IS NULL AND gi.id < gi2.id);

                UPDATE group_item SET is_deleted = true, deleted_time = now()
                 WHERE id IN (
                    SELECT gi.id FROM group_item gi
                    JOIN group_item gi2 ON gi.client_id = gi2.client_id AND gi.group_id = gi2.group_id
                    WHERE gi.client_id IS NOT NULL AND gi.is_deleted = false AND gi2.is_deleted = false
                      AND gi.analyse_token IS NULL AND gi2.analyse_token IS NULL AND gi.id < gi2.id);

                DROP INDEX IF EXISTS ix_group_item_client_id_group_id;
                DROP INDEX IF EXISTS ix_group_item_shift_id_group_id;
                DROP INDEX IF EXISTS ix_group_item_client_id_group_id_analyse_token;

                CREATE UNIQUE INDEX ix_group_item_client_id_group_id ON group_item (client_id, group_id)
                    WHERE client_id IS NOT NULL AND is_deleted = false AND analyse_token IS NULL;

                CREATE UNIQUE INDEX ix_group_item_shift_id_group_id ON group_item (shift_id, group_id)
                    WHERE shift_id IS NOT NULL AND is_deleted = false AND analyse_token IS NULL;

                CREATE UNIQUE INDEX ix_group_item_client_id_group_id_analyse_token ON group_item (client_id, group_id, analyse_token)
                    WHERE client_id IS NOT NULL AND is_deleted = false AND analyse_token IS NOT NULL;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ix_group_item_client_id_group_id;
                DROP INDEX IF EXISTS ix_group_item_shift_id_group_id;
                DROP INDEX IF EXISTS ix_group_item_client_id_group_id_analyse_token;
            ");

            migrationBuilder.DropColumn(
                name: "analyse_token",
                table: "group_item");

            migrationBuilder.DropColumn(
                name: "scenario_source_group_item_id",
                table: "group_item");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_group_item_client_id_group_id ON group_item (client_id, group_id)
                    WHERE client_id IS NOT NULL AND is_deleted = false;

                CREATE UNIQUE INDEX ix_group_item_shift_id_group_id ON group_item (shift_id, group_id)
                    WHERE shift_id IS NOT NULL AND is_deleted = false;
            ");
        }
    }
}
