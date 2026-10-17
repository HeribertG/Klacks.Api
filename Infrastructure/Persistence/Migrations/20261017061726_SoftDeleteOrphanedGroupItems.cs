using Klacks.Api.Domain.Constants;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Soft-deletes every live group_item (client membership, shift assignment, scenario membership) whose group
    /// is already soft-deleted. Before deleting a group also removed its items, such dangling memberships made a
    /// person count as "in a group" whose group nobody can see, hiding the person from every restricted user and
    /// from the "without group" filter. Idempotent: only rows still marked live are touched. Down is a no-op: the
    /// rows point at deleted groups and restoring them would bring the hidden-person state back.
    /// </summary>
    public partial class SoftDeleteOrphanedGroupItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                UPDATE group_item gi
                SET is_deleted = true,
                    deleted_time = now(),
                    current_user_deleted = '{AuditActorNames.System}'
                WHERE gi.is_deleted = false
                  AND EXISTS (SELECT 1 FROM ""group"" g WHERE g.id = gi.group_id AND g.is_deleted = true);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
