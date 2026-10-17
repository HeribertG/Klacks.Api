using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Marks every qualification that exists at update time as level-binding. Before the binding flag existed, a
    /// mandatory qualification held below the required level refused the manual booking; keeping the existing
    /// qualifications binding preserves that block instead of silently relaxing it (owner decision 2026-10-17).
    /// Qualifications created afterwards start with the default false. Down is a no-op: the previous
    /// per-qualification values are not recorded.
    /// </summary>
    public partial class PreserveLevelBlockForExistingQualifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE qualification SET is_level_binding = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
