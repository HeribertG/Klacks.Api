using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Brings every live container (shift_type 1) into the shape of the owner decision: a container has no order, it is one
    /// row with status 2 (OriginalShift) and no original_id. Real rows and rows of a scenario (kind 0) are converted, each
    /// inside its own world, so a scenario clone of a pair ends in the same shape as its real source and keeps pointing at
    /// the surviving row. Rows of a variant (kind 1) are left alone: a variant holds order families, needs a customer and
    /// refuses containers.
    /// A sealed row with exactly one live child, a plannable row of the same world: the plannable row stays with its id,
    /// because templates, works and references point at it, and loses original_id and any link to the sealed row; the
    /// sealed row is soft-deleted with its group links, default expenses and required qualifications, with an audit stamp,
    /// because raw SQL passes neither the soft-delete filter nor OnBeforeSaving.
    /// A scenario clone in another world that points at the converted sealed row through scenario_source_shift_id is
    /// re-pointed at the surviving plannable row, so a clone of only the sealed row still has a live source when it is accepted.
    /// A sealed row without a live child becomes status 2 itself.
    /// A sealed row that still has cut pieces, several children, or a live work, template, override or template reference
    /// is not touched and raises a warning: no writer creates that state and the row to keep cannot be decided here.
    /// Draft containers (status 0) stay drafts until they are sealed. Idempotent; a database without such rows sees no change.
    /// The literals are frozen on purpose: a migration must keep meaning what it meant when it was written.
    /// Down is a no-op: the plannable rows do not record their sealed row any more.
    /// </summary>
    public partial class ContainersHaveNoOrder : Migration
    {
        public const string ConversionSql = """
            DO $$
            DECLARE
                actor CONSTANT text := 'migration:ContainersHaveNoOrder';
                skipped uuid;
            BEGIN
                CREATE TEMP TABLE container_order_rows ON COMMIT DROP AS
                SELECT sealed.id AS sealed_id,
                       sealed.analyse_token AS sealed_analyse_token,
                       (SELECT count(*)
                        FROM shift child
                        WHERE child.original_id = sealed.id
                          AND NOT child.is_deleted
                          AND child.analyse_token IS NOT DISTINCT FROM sealed.analyse_token) AS child_count,
                       (SELECT child.id
                        FROM shift child
                        WHERE child.original_id = sealed.id
                          AND NOT child.is_deleted
                          AND child.status = 2
                          AND child.analyse_token IS NOT DISTINCT FROM sealed.analyse_token
                        ORDER BY child.id
                        LIMIT 1) AS plannable_id,
                       (EXISTS (SELECT 1 FROM work w WHERE w.shift_id = sealed.id AND NOT w.is_deleted)
                        OR EXISTS (SELECT 1 FROM container_template t WHERE t.container_id = sealed.id AND NOT t.is_deleted)
                        OR EXISTS (SELECT 1 FROM container_shift_overrides o WHERE o.container_id = sealed.id AND NOT o.is_deleted)
                        OR EXISTS (SELECT 1 FROM container_template_item i WHERE i.shift_id = sealed.id AND NOT i.is_deleted)
                        OR EXISTS (SELECT 1 FROM container_shift_override_items i WHERE i.shift_id = sealed.id AND NOT i.is_deleted)) AS is_referenced
                FROM shift sealed
                WHERE sealed.shift_type = 1
                  AND sealed.status = 1
                  AND NOT sealed.is_deleted
                  AND (sealed.analyse_token IS NULL
                       OR EXISTS (SELECT 1 FROM analyse_scenarios scenario
                                  WHERE scenario.token = sealed.analyse_token AND scenario.kind = 0));

                FOR skipped IN
                    SELECT sealed_id FROM container_order_rows
                    WHERE child_count > 1
                       OR (child_count = 1 AND plannable_id IS NULL)
                       OR (child_count = 1 AND is_referenced)
                LOOP
                    RAISE WARNING 'Container % keeps its sealed row: it has cut pieces, several children or live references', skipped;
                END LOOP;

                DELETE FROM container_order_rows
                WHERE child_count > 1
                   OR (child_count = 1 AND plannable_id IS NULL)
                   OR (child_count = 1 AND is_referenced);

                UPDATE shift plannable
                SET original_id = NULL,
                    parent_id = NULLIF(plannable.parent_id, row.sealed_id),
                    root_id = NULLIF(plannable.root_id, row.sealed_id),
                    lft = CASE WHEN plannable.root_id = row.sealed_id THEN NULL ELSE plannable.lft END,
                    rgt = CASE WHEN plannable.root_id = row.sealed_id THEN NULL ELSE plannable.rgt END,
                    update_time = now(),
                    current_user_updated = actor
                FROM container_order_rows row
                WHERE plannable.id = row.plannable_id;

                UPDATE shift clone
                SET scenario_source_shift_id = row.plannable_id, update_time = now(), current_user_updated = actor
                FROM container_order_rows row
                WHERE clone.scenario_source_shift_id = row.sealed_id
                  AND row.plannable_id IS NOT NULL
                  AND NOT clone.is_deleted
                  AND clone.analyse_token IS DISTINCT FROM row.sealed_analyse_token;

                UPDATE group_item item
                SET is_deleted = TRUE, deleted_time = now(), current_user_deleted = actor
                FROM container_order_rows row
                WHERE item.shift_id = row.sealed_id AND row.plannable_id IS NOT NULL AND NOT item.is_deleted;

                UPDATE shift_expenses expense
                SET is_deleted = TRUE, deleted_time = now(), current_user_deleted = actor
                FROM container_order_rows row
                WHERE expense.shift_id = row.sealed_id AND row.plannable_id IS NOT NULL AND NOT expense.is_deleted;

                UPDATE shift_required_qualification requirement
                SET is_deleted = TRUE, deleted_time = now(), current_user_deleted = actor
                FROM container_order_rows row
                WHERE requirement.shift_id = row.sealed_id AND row.plannable_id IS NOT NULL AND NOT requirement.is_deleted;

                UPDATE shift sealed
                SET is_deleted = TRUE, deleted_time = now(), current_user_deleted = actor
                FROM container_order_rows row
                WHERE sealed.id = row.sealed_id AND row.plannable_id IS NOT NULL;

                UPDATE shift sealed
                SET status = 2, update_time = now(), current_user_updated = actor
                FROM container_order_rows row
                WHERE sealed.id = row.sealed_id AND row.child_count = 0;

                DROP TABLE container_order_rows;
            END $$;
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(ConversionSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
