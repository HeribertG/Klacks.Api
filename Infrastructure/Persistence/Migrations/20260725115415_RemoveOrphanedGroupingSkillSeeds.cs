using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveOrphanedGroupingSkillSeeds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM agent_skills
                WHERE name IN (
                    'propose_customer_grouping',
                    'apply_customer_grouping',
                    'propose_employee_grouping',
                    'apply_employee_grouping'
                );");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally irreversible: the deleted rows are seed data replaced by
            // propose_grouping/apply_grouping, not user data — skill-seeds.json is the source of truth
            // and no longer defines these names, so there is nothing meaningful to restore.
        }
    }
}
