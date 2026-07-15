using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMacroFunctionTypeUniqueness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Macro.Type is repurposed: the previous frontend classification (0/1, "shift and
            // employments" / "work rules") never drove any live behavior — its only consumer, the
            // automation-rules Conductor subsystem, is not wired up anywhere — so it is reset to
            // Custom (0) for every macro before the new function semantics (Custom/Standard/
            // StandardAdditive) are applied.
            migrationBuilder.Sql("UPDATE public.macro SET type = 0;");

            // Backfill for existing installations: the seeded "AllShift" macro (fixed row id, never
            // matched by name — the dev DB accumulates duplicate "AllShift" rows with different ids
            // from test runs) becomes the automatic Standard default for new shifts of category Shift.
            migrationBuilder.Sql(
                @"UPDATE public.macro
SET type = 1, category = 1
WHERE id = 'a3edd3f5-c31c-4746-a9a0-c613d14ffd23';");

            // Idempotent safety net before the unique index is built: should more than one active
            // macro per category already carry the same non-Custom function type, keep only the
            // oldest row's type and demote the rest back to Custom.
            migrationBuilder.Sql(
                @"WITH ranked AS (
    SELECT id,
           ROW_NUMBER() OVER (PARTITION BY category, type ORDER BY create_time ASC, id ASC) AS rn
    FROM public.macro
    WHERE is_deleted = false AND type <> 0
)
UPDATE public.macro m
SET type = 0
FROM ranked r
WHERE m.id = r.id AND r.rn > 1;");

            migrationBuilder.CreateIndex(
                name: "ix_macro_category_type",
                table: "macro",
                columns: new[] { "category", "type" },
                unique: true,
                filter: "type <> 0 AND is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_macro_category_type",
                table: "macro");
        }
    }
}
