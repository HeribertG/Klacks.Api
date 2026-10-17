using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Physically removes the soft-deleted skill_phrase rows. The former delete-and-insert replacement
    /// left one tombstone per unchanged phrase on every seed version bump and language pack install;
    /// SkillPhraseRepository now diffs instead. No foreign key references skill_phrase.id, and every
    /// reader filters is_deleted, so the rows carry no information. Down is a no-op: tombstones are
    /// not restorable and nothing depends on them.
    /// </summary>
    public partial class PurgeSkillPhraseTombstones : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DELETE FROM skill_phrase WHERE is_deleted = true;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
