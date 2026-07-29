using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSkillPhrase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "skill_phrase",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_kind = table.Column<string>(type: "text", nullable: false),
                    owner_name = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "text", nullable: true),
                    kind = table.Column<string>(type: "text", nullable: false),
                    phrase = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    weight = table.Column<double>(type: "double precision", nullable: false, defaultValue: 1.0),
                    source = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "Active"),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skill_phrase", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_skill_phrase_owner_kind_owner_name",
                table: "skill_phrase",
                columns: new[] { "owner_kind", "owner_name" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_phrase_owner_kind_owner_name_language_kind_phrase",
                table: "skill_phrase",
                columns: new[] { "owner_kind", "owner_name", "language", "kind", "phrase" },
                unique: true,
                filter: "is_deleted = false")
                .Annotation("Npgsql:NullsDistinct", false);

            BackfillFromLegacyColumns(migrationBuilder);
        }

        // Backfill of the four legacy phrase stores into skill_phrase. It lives inside the migration
        // rather than in a separate script so that every installation runs it exactly once.
        //
        // The literal language list below mirrors LanguagePluginConstants.CoreLanguages at the time
        // this migration was written. It is intentionally hardcoded and not read from that constant:
        // an applied migration must stay immutable, so a later change to the constant must not change
        // what this migration did on installations that already ran it.
        //
        // IMPORTANT: the resulting "source" value is a DERIVATION, not a recorded fact. A de/en/fr/it
        // phrase is assumed to be Seed, but it could equally have been written by an administrator
        // through UpdateAgentSkillSkill. The legacy columns do not record their origin, so the two
        // cases are indistinguishable from the data. Anything relying on "source" must treat Seed on
        // a core language as "seed or admin", not as proof of a seed origin.
        private static void BackfillFromLegacyColumns(MigrationBuilder migrationBuilder)
        {
            // trigger_keywords and trigger_json are plain text columns, so a malformed value would
            // abort the whole migration on a cast. The C# reader returns an empty list on a parse
            // error instead; this helper reproduces that behaviour per row.
            migrationBuilder.Sql("""
                CREATE OR REPLACE FUNCTION pg_temp.skill_phrase_try_jsonb(p_text text)
                RETURNS jsonb LANGUAGE plpgsql IMMUTABLE AS $fn$
                BEGIN
                    RETURN p_text::jsonb;
                EXCEPTION WHEN others THEN
                    RETURN NULL;
                END;
                $fn$;
                """);

            // 1. agent_skills.synonyms (jsonb object of language -> array of phrases).
            // Blank phrases are skipped, mirroring the IsNullOrWhiteSpace filter the index text applies
            // to synonyms.
            migrationBuilder.Sql("""
                WITH owners AS MATERIALIZED (
                    SELECT s.name, s.synonyms
                    FROM agent_skills s
                    WHERE s.is_deleted = false
                      AND s.synonyms IS NOT NULL
                      AND jsonb_typeof(s.synonyms) = 'object'
                ), buckets AS MATERIALIZED (
                    SELECT o.name, kv.key AS language, kv.value AS phrases
                    FROM owners o
                    CROSS JOIN LATERAL jsonb_each(o.synonyms) kv(key, value)
                    WHERE jsonb_typeof(kv.value) = 'array'
                )
                INSERT INTO skill_phrase (
                    id, owner_kind, owner_name, language, kind, phrase,
                    sort_order, weight, source, status, is_deleted, create_time)
                SELECT gen_random_uuid(), 'Skill', b.name, b.language, 'Synonym', e.phrase,
                       (e.ord - 1)::int, 1.0,
                       CASE WHEN b.language IN ('de', 'en', 'fr', 'it') THEN 'Seed' ELSE 'LanguagePack' END,
                       'Active', false, now()
                FROM buckets b
                CROSS JOIN LATERAL jsonb_array_elements_text(b.phrases) WITH ORDINALITY e(phrase, ord)
                WHERE e.phrase ~ '\S';
                """);

            // 2. agent_skills.trigger_keywords (text holding a flat JSON array, no language dimension).
            // language stays NULL: the array mixes German and English terms in one list.
            // Blank entries are NOT filtered here, because BuildEmbeddingText joins TriggerKeywords
            // without any IsNullOrWhiteSpace filter. Keeping them preserves the index text byte for byte.
            migrationBuilder.Sql("""
                WITH owners AS MATERIALIZED (
                    SELECT s.name, pg_temp.skill_phrase_try_jsonb(s.trigger_keywords) AS keywords
                    FROM agent_skills s
                    WHERE s.is_deleted = false
                ), arrays AS MATERIALIZED (
                    SELECT o.name, o.keywords
                    FROM owners o
                    WHERE o.keywords IS NOT NULL AND jsonb_typeof(o.keywords) = 'array'
                )
                INSERT INTO skill_phrase (
                    id, owner_kind, owner_name, language, kind, phrase,
                    sort_order, weight, source, status, is_deleted, create_time)
                SELECT gen_random_uuid(), 'Skill', a.name, NULL, 'Keyword', e.phrase,
                       (e.ord - 1)::int, 1.0, 'Seed', 'Active', false, now()
                FROM arrays a
                CROSS JOIN LATERAL jsonb_array_elements_text(a.keywords) WITH ORDINALITY e(phrase, ord);
                """);

            // 3. agent_recipes.synonyms, identical shape to the skill synonyms.
            migrationBuilder.Sql("""
                WITH owners AS MATERIALIZED (
                    SELECT r.name, r.synonyms
                    FROM agent_recipes r
                    WHERE r.is_deleted = false
                      AND r.synonyms IS NOT NULL
                      AND jsonb_typeof(r.synonyms) = 'object'
                ), buckets AS MATERIALIZED (
                    SELECT o.name, kv.key AS language, kv.value AS phrases
                    FROM owners o
                    CROSS JOIN LATERAL jsonb_each(o.synonyms) kv(key, value)
                    WHERE jsonb_typeof(kv.value) = 'array'
                )
                INSERT INTO skill_phrase (
                    id, owner_kind, owner_name, language, kind, phrase,
                    sort_order, weight, source, status, is_deleted, create_time)
                SELECT gen_random_uuid(), 'Recipe', b.name, b.language, 'Synonym', e.phrase,
                       (e.ord - 1)::int, 1.0,
                       CASE WHEN b.language IN ('de', 'en', 'fr', 'it') THEN 'Seed' ELSE 'LanguagePack' END,
                       'Active', false, now()
                FROM buckets b
                CROSS JOIN LATERAL jsonb_array_elements_text(b.phrases) WITH ORDINALITY e(phrase, ord)
                WHERE e.phrase ~ '\S';
                """);

            // 4. Trigger words out of agent_recipes.trigger_json, reproducing ExtractTriggerWords in
            // KnowledgeIndexSynchronizer: per condition of allOf take anyWordStart, then anySubstring,
            // then startsWith, in that order; drop blanks; then remove case-insensitive duplicates
            // keeping the first occurrence. noneOf is deliberately not read.
            //
            // Note the asymmetry against step 2: recipe keywords are stored ALREADY DEDUPED because
            // ExtractTriggerWords ends in Distinct(OrdinalIgnoreCase), while skill keywords are stored
            // RAW because BuildEmbeddingText applies no Distinct to them. Each side mirrors its own
            // C# path; the difference is intentional and must be kept when the read paths move over.
            migrationBuilder.Sql("""
                WITH owners AS MATERIALIZED (
                    SELECT r.name, pg_temp.skill_phrase_try_jsonb(r.trigger_json) AS trigger_doc
                    FROM agent_recipes r
                    WHERE r.is_deleted = false
                ), conditions AS MATERIALIZED (
                    SELECT o.name, c.ord AS condition_ord, c.cond
                    FROM owners o
                    CROSS JOIN LATERAL jsonb_array_elements(o.trigger_doc -> 'allOf') WITH ORDINALITY c(cond, ord)
                    WHERE o.trigger_doc IS NOT NULL
                      AND jsonb_typeof(o.trigger_doc) = 'object'
                      AND jsonb_typeof(o.trigger_doc -> 'allOf') = 'array'
                ), words AS (
                    SELECT c.name, c.condition_ord, 1 AS list_ord, w.ord AS word_ord, w.word
                    FROM conditions c
                    CROSS JOIN LATERAL jsonb_array_elements_text(c.cond -> 'anyWordStart') WITH ORDINALITY w(word, ord)
                    WHERE jsonb_typeof(c.cond -> 'anyWordStart') = 'array'
                    UNION ALL
                    SELECT c.name, c.condition_ord, 2, w.ord, w.word
                    FROM conditions c
                    CROSS JOIN LATERAL jsonb_array_elements_text(c.cond -> 'anySubstring') WITH ORDINALITY w(word, ord)
                    WHERE jsonb_typeof(c.cond -> 'anySubstring') = 'array'
                    UNION ALL
                    SELECT c.name, c.condition_ord, 3, w.ord, w.word
                    FROM conditions c
                    CROSS JOIN LATERAL jsonb_array_elements_text(c.cond -> 'startsWith') WITH ORDINALITY w(word, ord)
                    WHERE jsonb_typeof(c.cond -> 'startsWith') = 'array'
                ), deduped AS (
                    SELECT DISTINCT ON (w.name, lower(w.word))
                           w.name, w.word, w.condition_ord, w.list_ord, w.word_ord
                    FROM words w
                    WHERE w.word ~ '\S'
                    ORDER BY w.name, lower(w.word), w.condition_ord, w.list_ord, w.word_ord
                )
                INSERT INTO skill_phrase (
                    id, owner_kind, owner_name, language, kind, phrase,
                    sort_order, weight, source, status, is_deleted, create_time)
                SELECT gen_random_uuid(), 'Recipe', d.name, NULL, 'Keyword', d.word,
                       (row_number() OVER (
                           PARTITION BY d.name
                           ORDER BY d.condition_ord, d.list_ord, d.word_ord) - 1)::int,
                       1.0, 'Seed', 'Active', false, now()
                FROM deduped d;
                """);

            migrationBuilder.Sql("DROP FUNCTION IF EXISTS pg_temp.skill_phrase_try_jsonb(text);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "skill_phrase");
        }
    }
}
