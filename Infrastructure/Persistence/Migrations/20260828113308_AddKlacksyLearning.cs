using System;
using Klacks.Api.Data.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Replaces the skill-gap subsystem with the Klacksy learning loop. skill_gap_records is dropped
    /// rather than migrated: it stored up to 1000 characters of raw user text per row, its counter reset
    /// to one whenever a record changed status, and Klacks is not in production, so there is nothing worth
    /// carrying over. The five new tables store an excerpt of at most 120 characters instead.
    /// </summary>
    public partial class AddKlacksyLearning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "skill_gap_records");

            migrationBuilder.CreateTable(
                name: "skill_learning_clusters",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    cluster_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    intent_excerpt = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    occurrence_count = table.Column<int>(type: "integer", nullable: false),
                    distinct_user_count = table.Column<int>(type: "integer", nullable: false),
                    signal_kinds_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    outcome_ref_kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    outcome_ref = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    learning_claimed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    learning_instance = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    last_error = table.Column<string>(type: "text", nullable: true),
                    attempt_count = table.Column<int>(type: "integer", nullable: false),
                    status_changed_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    first_seen_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_seen_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    learned_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    retired_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_skill_learning_clusters", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skill_learning_candidates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    variant_no = table.Column<int>(type: "integer", nullable: false),
                    kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    payload_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    routing_result_json = table.Column<string>(type: "jsonb", nullable: true),
                    execution_result_json = table.Column<string>(type: "jsonb", nullable: true),
                    error_text = table.Column<string>(type: "text", nullable: true),
                    activated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_skill_learning_candidates", x => x.id);
                    table.ForeignKey(
                        name: "fk_skill_learning_candidates_skill_learning_clusters_cluster_id",
                        column: x => x.cluster_id,
                        principalTable: "skill_learning_clusters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_learning_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    conversation_id = table.Column<string>(type: "text", nullable: true),
                    locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    intent_excerpt = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    signal = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    chosen_skill = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    expected_skill = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    toolset_json = table.Column<string>(type: "jsonb", nullable: false),
                    trajectory_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_golden = table.Column<bool>(type: "boolean", nullable: false),
                    occurred_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_skill_learning_cases", x => x.id);
                    table.ForeignKey(
                        name: "fk_skill_learning_cases_skill_learning_clusters_cluster_id",
                        column: x => x.cluster_id,
                        principalTable: "skill_learning_clusters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_learning_golden_cases",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    query = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    expected_source_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    cluster_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_skill_learning_golden_cases", x => x.id);
                    table.ForeignKey(
                        name: "fk_skill_learning_golden_cases_skill_learning_clusters_cluster",
                        column: x => x.cluster_id,
                        principalTable: "skill_learning_clusters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "skill_learning_fitness",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    candidate_id = table.Column<Guid>(type: "uuid", nullable: false),
                    window_start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    uses = table.Column<int>(type: "integer", nullable: false),
                    successes = table.Column<int>(type: "integer", nullable: false),
                    failures = table.Column<int>(type: "integer", nullable: false),
                    helpful = table.Column<int>(type: "integer", nullable: false),
                    corrections = table.Column<int>(type: "integer", nullable: false),
                    recurrences = table.Column<int>(type: "integer", nullable: false),
                    last_used_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    quote = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
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
                    table.PrimaryKey("pk_skill_learning_fitness", x => x.id);
                    table.ForeignKey(
                        name: "fk_skill_learning_fitness_skill_learning_candidates_candidate_",
                        column: x => x.candidate_id,
                        principalTable: "skill_learning_candidates",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_skill_learning_candidates_cluster_id_variant_no",
                table: "skill_learning_candidates",
                columns: new[] { "cluster_id", "variant_no" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_learning_candidates_status",
                table: "skill_learning_candidates",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_skill_learning_cases_cluster_id_occurred_at_utc",
                table: "skill_learning_cases",
                columns: new[] { "cluster_id", "occurred_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_learning_cases_user_id",
                table: "skill_learning_cases",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_skill_learning_clusters_agent_id_cluster_key",
                table: "skill_learning_clusters",
                columns: new[] { "agent_id", "cluster_key" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_skill_learning_clusters_agent_id_status",
                table: "skill_learning_clusters",
                columns: new[] { "agent_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_learning_clusters_status_last_seen_at_utc",
                table: "skill_learning_clusters",
                columns: new[] { "status", "last_seen_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_learning_clusters_status_status_changed_at_utc",
                table: "skill_learning_clusters",
                columns: new[] { "status", "status_changed_at_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_learning_fitness_candidate_id_window_start_utc",
                table: "skill_learning_fitness",
                columns: new[] { "candidate_id", "window_start_utc" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_skill_learning_golden_cases_cluster_id",
                table: "skill_learning_golden_cases",
                column: "cluster_id");

            migrationBuilder.CreateIndex(
                name: "ix_skill_learning_golden_cases_expected_source_id",
                table: "skill_learning_golden_cases",
                column: "expected_source_id");

            // The cluster centroid is a pgvector column. EF Core cannot map the vector type, so - exactly
            // as for knowledge_index - the property is [NotMapped], the column is created here and every
            // read or write of it goes through raw SQL. Nullable because nothing computes an embedding
            // before stage G2; a zero-filled default would be a fake centroid that a nearest-neighbour
            // search would happily return as a match.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
            migrationBuilder.Sql(
                "ALTER TABLE skill_learning_clusters ADD COLUMN embedding vector(768) NULL;");
            migrationBuilder.Sql(
                "CREATE INDEX skill_learning_clusters_embedding_idx " +
                "ON skill_learning_clusters USING hnsw (embedding vector_cosine_ops);");

            // The review_skill_suggestions skill is gone: its accept path was unreachable against the real
            // repository, and it operated on the table this migration drops. skill-seeds.json no longer
            // defines it, but the seed loader only upserts and never prunes, so the already-seeded catalog
            // row would survive as an entry pointing at a handler class that no longer exists.
            // agent_skill_executions cascade with the skill row; skill_phrase and skill_relations are
            // addressed by name and have no foreign key, so they are deleted explicitly.
            migrationBuilder.Sql(@"
                DELETE FROM skill_phrase
                WHERE owner_kind = 'Skill' AND owner_name = 'review_skill_suggestions';");

            migrationBuilder.Sql(@"
                DELETE FROM skill_relations
                WHERE skill_a_name = 'review_skill_suggestions'
                   OR skill_b_name = 'review_skill_suggestions';");

            migrationBuilder.Sql(@"
                DELETE FROM agent_skills
                WHERE name = 'review_skill_suggestions';");

            // Installs the default governance row for klacksy_learned_digest. Idempotent by WHERE NOT
            // EXISTS, so the rows the earlier governance migration wrote stay untouched.
            AgentTriggerGovernanceDefaultsSql.Apply(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // The deleted seed rows are deliberately not restored: skill-seeds.json is the source of truth
            // for the skill catalog and no longer defines review_skill_suggestions, so re-inserting it
            // would recreate a row without a handler. The pgvector column and its index disappear with
            // skill_learning_clusters.
            migrationBuilder.Sql(
                "DELETE FROM agent_trigger_governance " +
                "WHERE trigger_kind = 'klacksy_learned_digest' AND group_id IS NULL;");

            migrationBuilder.DropTable(
                name: "skill_learning_cases");

            migrationBuilder.DropTable(
                name: "skill_learning_fitness");

            migrationBuilder.DropTable(
                name: "skill_learning_golden_cases");

            migrationBuilder.DropTable(
                name: "skill_learning_candidates");

            migrationBuilder.DropTable(
                name: "skill_learning_clusters");

            migrationBuilder.CreateTable(
                name: "skill_gap_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    detected_intent = table.Column<string>(type: "text", nullable: false),
                    embedding = table.Column<float[]>(type: "real[]", nullable: true),
                    first_detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    last_detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    normalized_message_hash = table.Column<string>(type: "text", nullable: false),
                    occurrence_count = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    suggested_description = table.Column<string>(type: "text", nullable: true),
                    suggested_skill_name = table.Column<string>(type: "text", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_skill_gap_records", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_skill_gap_records_agent_id_occurrence_count",
                table: "skill_gap_records",
                columns: new[] { "agent_id", "occurrence_count" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_gap_records_agent_id_status",
                table: "skill_gap_records",
                columns: new[] { "agent_id", "status" });
        }
    }
}
