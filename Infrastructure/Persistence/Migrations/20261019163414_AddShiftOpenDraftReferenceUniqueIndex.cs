using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShiftOpenDraftReferenceUniqueIndex : Migration
    {
        // Fails (deleting nothing) when the index could not be built, naming the offending
        // (source_system_id, external_order_reference, count) groups so an operator can resolve them.
        public const string DuplicateOpenDraftCheckSql = """
            DO $$
            DECLARE
                duplicate_groups integer;
                duplicate_list text;
            BEGIN
                SELECT count(*) INTO duplicate_groups FROM (
                    SELECT source_system_id, external_order_reference
                    FROM shift
                    WHERE source_system_id IS NOT NULL
                      AND external_order_reference IS NOT NULL
                      AND is_deleted = false
                      AND status = 0
                    GROUP BY source_system_id, external_order_reference
                    HAVING count(*) > 1
                ) groups;

                IF duplicate_groups > 0 THEN
                    SELECT string_agg(format('(source_system_id=%L, external_order_reference=%L, open_drafts=%s)', source_system_id, external_order_reference, draft_count), '; ')
                    INTO duplicate_list
                    FROM (
                        SELECT source_system_id, external_order_reference, count(*) AS draft_count
                        FROM shift
                        WHERE source_system_id IS NOT NULL
                          AND external_order_reference IS NOT NULL
                          AND is_deleted = false
                          AND status = 0
                        GROUP BY source_system_id, external_order_reference
                        HAVING count(*) > 1
                        ORDER BY count(*) DESC, source_system_id, external_order_reference
                        LIMIT 50
                    ) listed;

                    RAISE EXCEPTION 'Cannot create unique index ux_shift_open_draft_source_system_id_external_order_reference: % order reference(s) have more than one open draft (status 0, not deleted). Resolve them manually (soft-delete or close the surplus drafts), then re-run the migration. First 50: %', duplicate_groups, duplicate_list;
                END IF;
            END
            $$;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(DuplicateOpenDraftCheckSql);

            migrationBuilder.CreateIndex(
                name: "ux_shift_open_draft_source_system_id_external_order_reference",
                table: "shift",
                columns: new[] { "source_system_id", "external_order_reference" },
                unique: true,
                filter: "external_order_reference IS NOT NULL AND is_deleted = false AND status = 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ux_shift_open_draft_source_system_id_external_order_reference",
                table: "shift");
        }
    }
}
