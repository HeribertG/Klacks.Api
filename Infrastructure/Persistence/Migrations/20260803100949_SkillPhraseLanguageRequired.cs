using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class SkillPhraseLanguageRequired : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_skill_phrase_owner_kind_owner_name_language_kind_phrase",
                table: "skill_phrase");

            // A null language used to mean two different things, and the difference matters: the
            // keyword matcher accepts "mul" as an anchor, which lets a two-letter phrase match on a
            // word boundary, and must never accept a phrase that merely lacks a language tag.
            //
            // Seeded skill keywords stored as null came from the reserved "mul" key in
            // skill-seeds.json, which SkillSeedLoader mapped to an empty string and the repository
            // then stored as null. Those are genuinely identical in every language (sso, imap, ldap).
            //
            // Everything else that is null is only untagged, not neutral: recipe trigger stems come
            // from a DSL with no language dimension and mix languages inside one list ("erstell",
            // "create", "add"), and administrator input was written as "und" into the jsonb column
            // while reaching this table as null.
            //
            // Both statements match no rows on a fresh database, where the seed writes the final
            // values directly. They exist so an already populated database converges on the same
            // state instead of collapsing all 1146 rows onto the default.
            migrationBuilder.Sql(@"
                UPDATE skill_phrase SET language = 'mul'
                WHERE language IS NULL AND owner_kind = 'Skill' AND source = 'Seed';");

            migrationBuilder.Sql(@"
                UPDATE skill_phrase SET language = 'und'
                WHERE language IS NULL;");

            // The unique index carries no source column, so a row rewritten above can now collide
            // with one that already held the target tag. The index is recreated at the end of this
            // migration and would fail on such a pair; the oldest row wins.
            migrationBuilder.Sql(@"
                DELETE FROM skill_phrase a
                USING skill_phrase b
                WHERE a.is_deleted = false AND b.is_deleted = false
                  AND a.owner_kind = b.owner_kind AND a.owner_name = b.owner_name
                  AND a.language = b.language AND a.kind = b.kind AND a.phrase = b.phrase
                  AND a.id > b.id;");

            migrationBuilder.AlterColumn<string>(
                name: "language",
                table: "skill_phrase",
                type: "text",
                nullable: false,
                defaultValue: "und",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_skill_phrase_owner_kind_owner_name_language_kind_phrase",
                table: "skill_phrase",
                columns: new[] { "owner_kind", "owner_name", "language", "kind", "phrase" },
                unique: true,
                filter: "is_deleted = false");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_skill_phrase_owner_kind_owner_name_language_kind_phrase",
                table: "skill_phrase");

            migrationBuilder.AlterColumn<string>(
                name: "language",
                table: "skill_phrase",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldDefaultValue: "und");

            migrationBuilder.CreateIndex(
                name: "ix_skill_phrase_owner_kind_owner_name_language_kind_phrase",
                table: "skill_phrase",
                columns: new[] { "owner_kind", "owner_name", "language", "kind", "phrase" },
                unique: true,
                filter: "is_deleted = false")
                .Annotation("Npgsql:NullsDistinct", false);
        }
    }
}
