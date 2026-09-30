using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Gives the demo split shifts that the seeder wrote without any group link the group links of their
    /// original order. Before 2026-09-22 ShiftSeed tracked the groups of the three 24h pieces but never
    /// added the piece ids to the list GenerateInsertScriptForShiftGroupItems writes links for, so 60
    /// seeded SplitShift rows stayed without a group and were invisible in every group filter.
    /// Only seeder rows are touched (created by the seeder's actor name, status SplitShift, not deleted,
    /// no scenario) and only when the piece never held any group link, not even a deleted one, so a group
    /// a user removed on purpose is never brought back. Idempotent: a second run finds nothing.
    /// </summary>
    public partial class BackfillSeededSplitShiftGroups : Migration
    {
        private const string BackfillSql = @"
INSERT INTO group_item (id, shift_id, group_id, valid_from, valid_until, create_time, current_user_created, is_deleted)
SELECT gen_random_uuid(), piece.id, link.group_id, link.valid_from, link.valid_until, now(), 'Anonymus', false
FROM shift piece
JOIN group_item link
  ON link.shift_id = piece.original_id
 AND link.is_deleted = false
 AND link.analyse_token IS NULL
JOIN ""group"" g
  ON g.id = link.group_id
 AND g.is_deleted = false
WHERE piece.status = 3
  AND piece.is_deleted = false
  AND piece.analyse_token IS NULL
  AND piece.current_user_created = 'Anonymus'
  AND NOT EXISTS (SELECT 1 FROM group_item existing WHERE existing.shift_id = piece.id);";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(BackfillSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
