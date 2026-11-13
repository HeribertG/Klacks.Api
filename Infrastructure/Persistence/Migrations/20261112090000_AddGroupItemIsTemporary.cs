using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Temporary memberships per day (owner decision 2026-11-11). Adds group_item.is_temporary (false for every existing
    /// row) and sets it on the client memberships scenarios hold (analyse_token set, client set, no shift), deleted ones
    /// included. The unique index of the real memberships (client, group) leaves temporary rows out, a new unique index
    /// allows one temporary real membership per (client, group, first day), and the scenario index gets valid_from as a
    /// fourth key, so a person borrowed on two separate days holds two day rows instead of one row spanning the days
    /// between them. Memberships that were taken over before this migration stay regular rows: nothing tells a promoted
    /// span from a membership the planner entered. The literals are frozen on purpose: a migration must keep meaning what
    /// it meant when it was written.
    /// Down restores the previous indexes and drops the column. It deletes and merges nothing: when a (client, group)
    /// pair of the real plan or a (client, group, scenario) triple holds more than one live membership row, which the
    /// previous unique indexes cannot carry, it stops with an error that counts those pairs and leaves the database
    /// unchanged. A single temporary row that remains becomes an ordinary time-bounded membership.
    /// </summary>
    public partial class AddGroupItemIsTemporary : Migration
    {
        public const string BackfillScenarioMembershipsSql = """
            UPDATE group_item SET is_temporary = true
            WHERE analyse_token IS NOT NULL AND client_id IS NOT NULL AND shift_id IS NULL;
            """;

        public const string DownGuardSql = """
            DO $$
            DECLARE
                real_pairs integer;
                scenario_triples integer;
            BEGIN
                SELECT count(*) INTO real_pairs FROM (
                    SELECT client_id, group_id
                    FROM group_item
                    WHERE client_id IS NOT NULL AND is_deleted = false AND analyse_token IS NULL
                    GROUP BY client_id, group_id
                    HAVING count(*) > 1
                ) pairs;

                SELECT count(*) INTO scenario_triples FROM (
                    SELECT client_id, group_id, analyse_token
                    FROM group_item
                    WHERE client_id IS NOT NULL AND is_deleted = false AND analyse_token IS NOT NULL
                    GROUP BY client_id, group_id, analyse_token
                    HAVING count(*) > 1
                ) triples;

                IF real_pairs > 0 OR scenario_triples > 0 THEN
                    RAISE EXCEPTION 'Cannot restore the unique indexes of group_item: % (client, group) pair(s) of the real plan and % (client, group, scenario) triple(s) hold more than one live membership row (temporary memberships per day). Nothing was changed. Resolve them manually (accept or reject the scenarios, soft-delete the surplus rows), then re-run the migration.', real_pairs, scenario_triples;
                END IF;
            END
            $$;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "is_temporary",
                table: "group_item",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql(BackfillScenarioMembershipsSql);

            migrationBuilder.DropIndex(
                name: "ix_group_item_client_id_group_id",
                table: "group_item");

            migrationBuilder.DropIndex(
                name: "ix_group_item_client_id_group_id_analyse_token",
                table: "group_item");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_client_id_group_id",
                table: "group_item",
                columns: new[] { "client_id", "group_id" },
                unique: true,
                filter: "\"client_id\" IS NOT NULL AND \"is_deleted\" = false AND \"analyse_token\" IS NULL AND \"is_temporary\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_client_id_group_id_valid_from",
                table: "group_item",
                columns: new[] { "client_id", "group_id", "valid_from" },
                unique: true,
                filter: "\"client_id\" IS NOT NULL AND \"is_deleted\" = false AND \"analyse_token\" IS NULL AND \"is_temporary\" = true");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_client_id_group_id_analyse_token_valid_from",
                table: "group_item",
                columns: new[] { "client_id", "group_id", "analyse_token", "valid_from" },
                unique: true,
                filter: "\"client_id\" IS NOT NULL AND \"is_deleted\" = false AND \"analyse_token\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DownGuardSql);

            migrationBuilder.DropIndex(
                name: "ix_group_item_client_id_group_id_analyse_token_valid_from",
                table: "group_item");

            migrationBuilder.DropIndex(
                name: "ix_group_item_client_id_group_id_valid_from",
                table: "group_item");

            migrationBuilder.DropIndex(
                name: "ix_group_item_client_id_group_id",
                table: "group_item");

            migrationBuilder.DropColumn(
                name: "is_temporary",
                table: "group_item");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_client_id_group_id",
                table: "group_item",
                columns: new[] { "client_id", "group_id" },
                unique: true,
                filter: "\"client_id\" IS NOT NULL AND \"is_deleted\" = false AND \"analyse_token\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_client_id_group_id_analyse_token",
                table: "group_item",
                columns: new[] { "client_id", "group_id", "analyse_token" },
                unique: true,
                filter: "\"client_id\" IS NOT NULL AND \"is_deleted\" = false AND \"analyse_token\" IS NOT NULL");
        }
    }
}
