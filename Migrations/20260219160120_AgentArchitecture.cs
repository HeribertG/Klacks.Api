using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AgentArchitecture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "agent_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    default_soul_json = table.Column<string>(type: "text", nullable: false),
                    default_skills_json = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_agent_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agents",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    display_name = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_agents", x => x.id);
                    table.ForeignKey(
                        name: "fk_agents_agent_templates_template_id",
                        column: x => x.template_id,
                        principalTable: "agent_templates",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "agent_links",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    link_type = table.Column<string>(type: "text", nullable: false),
                    config = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_agent_links", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_links_agents_source_agent_id",
                        column: x => x.source_agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_agent_links_agents_target_agent_id",
                        column: x => x.target_agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "agent_memories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    importance = table.Column<int>(type: "integer", nullable: false),
                    embedding = table.Column<float[]>(type: "real[]", nullable: true),
                    is_pinned = table.Column<bool>(type: "boolean", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    supersedes_id = table.Column<Guid>(type: "uuid", nullable: true),
                    access_count = table.Column<int>(type: "integer", nullable: false),
                    last_accessed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    source = table.Column<string>(type: "text", nullable: false),
                    source_ref = table.Column<string>(type: "text", nullable: true),
                    metadata = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_agent_memories", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_memories_agent_memories_supersedes_id",
                        column: x => x.supersedes_id,
                        principalTable: "agent_memories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_agent_memories_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_sessions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<string>(type: "text", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "text", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    message_count = table.Column<int>(type: "integer", nullable: false),
                    token_count_est = table.Column<int>(type: "integer", nullable: false),
                    compaction_count = table.Column<int>(type: "integer", nullable: false),
                    active_categories = table.Column<string>(type: "text", nullable: false),
                    channel = table.Column<string>(type: "text", nullable: false),
                    last_message_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_model_id = table.Column<string>(type: "text", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_agent_sessions", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_sessions_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_skills",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    parameters_json = table.Column<string>(type: "text", nullable: false),
                    required_permission = table.Column<string>(type: "text", nullable: true),
                    execution_type = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    handler_type = table.Column<string>(type: "text", nullable: false),
                    handler_config = table.Column<string>(type: "text", nullable: false),
                    trigger_keywords = table.Column<string>(type: "text", nullable: false),
                    allowed_channels = table.Column<string>(type: "text", nullable: false),
                    max_calls_per_session = table.Column<int>(type: "integer", nullable: true),
                    always_on = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_agent_skills", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_skills_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_soul_sections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_type = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    source = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_agent_soul_sections", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_soul_sections_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_memory_tags",
                columns: table => new
                {
                    memory_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_agent_memory_tags", x => new { x.memory_id, x.tag });
                    table.ForeignKey(
                        name: "fk_agent_memory_tags_agent_memories_memory_id",
                        column: x => x.memory_id,
                        principalTable: "agent_memories",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_session_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    token_count = table.Column<int>(type: "integer", nullable: true),
                    model_id = table.Column<string>(type: "text", nullable: true),
                    function_calls = table.Column<string>(type: "text", nullable: true),
                    is_compacted = table.Column<bool>(type: "boolean", nullable: false),
                    compacted_into_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_agent_session_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_session_messages_agent_session_messages_compacted_int",
                        column: x => x.compacted_into_id,
                        principalTable: "agent_session_messages",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_agent_session_messages_agent_sessions_session_id",
                        column: x => x.session_id,
                        principalTable: "agent_sessions",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_skill_executions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    session_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    tool_name = table.Column<string>(type: "text", nullable: false),
                    parameters_json = table.Column<string>(type: "text", nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    result_message = table.Column<string>(type: "text", nullable: true),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    triggered_by = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_agent_skill_executions", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_skill_executions_agent_skills_skill_id",
                        column: x => x.skill_id,
                        principalTable: "agent_skills",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "agent_soul_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    soul_section_id = table.Column<Guid>(type: "uuid", nullable: false),
                    section_type = table.Column<string>(type: "text", nullable: false),
                    content_before = table.Column<string>(type: "text", nullable: true),
                    content_after = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    change_type = table.Column<string>(type: "text", nullable: false),
                    changed_by = table.Column<string>(type: "text", nullable: true),
                    change_reason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_agent_soul_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_agent_soul_histories_agent_soul_sections_soul_section_id",
                        column: x => x.soul_section_id,
                        principalTable: "agent_soul_sections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Indexes
            migrationBuilder.CreateIndex(name: "ix_agent_links_source_agent_id_target_agent_id_link_type", table: "agent_links", columns: ["source_agent_id", "target_agent_id", "link_type"], unique: true, filter: "is_active = true AND is_deleted = false");
            migrationBuilder.CreateIndex(name: "ix_agent_links_target_agent_id", table: "agent_links", column: "target_agent_id");
            migrationBuilder.CreateIndex(name: "ix_agent_memories_agent_id", table: "agent_memories", column: "agent_id", filter: "is_pinned = true AND is_deleted = false");
            migrationBuilder.CreateIndex(name: "ix_agent_memories_agent_id_category", table: "agent_memories", columns: ["agent_id", "category"]);
            migrationBuilder.CreateIndex(name: "ix_agent_memories_agent_id_importance", table: "agent_memories", columns: ["agent_id", "importance"]);
            migrationBuilder.CreateIndex(name: "ix_agent_memories_agent_id_source", table: "agent_memories", columns: ["agent_id", "source"]);
            migrationBuilder.CreateIndex(name: "ix_agent_memories_expires_at", table: "agent_memories", column: "expires_at", filter: "expires_at IS NOT NULL AND is_deleted = false");
            migrationBuilder.CreateIndex(name: "ix_agent_memories_supersedes_id", table: "agent_memories", column: "supersedes_id");
            migrationBuilder.CreateIndex(name: "ix_agent_memory_tags_tag", table: "agent_memory_tags", column: "tag");
            migrationBuilder.CreateIndex(name: "ix_agent_session_messages_compacted_into_id", table: "agent_session_messages", column: "compacted_into_id");
            migrationBuilder.CreateIndex(name: "ix_agent_session_messages_session_id_create_time", table: "agent_session_messages", columns: ["session_id", "create_time"], filter: "is_compacted = false AND is_deleted = false");
            migrationBuilder.CreateIndex(name: "ix_agent_sessions_agent_id_session_id", table: "agent_sessions", columns: ["agent_id", "session_id"], unique: true);
            migrationBuilder.CreateIndex(name: "ix_agent_sessions_agent_id_status", table: "agent_sessions", columns: ["agent_id", "status"]);
            migrationBuilder.CreateIndex(name: "ix_agent_sessions_user_id_update_time", table: "agent_sessions", columns: ["user_id", "update_time"]);
            migrationBuilder.CreateIndex(name: "ix_agent_skill_executions_session_id_create_time", table: "agent_skill_executions", columns: ["session_id", "create_time"]);
            migrationBuilder.CreateIndex(name: "ix_agent_skill_executions_skill_id_create_time", table: "agent_skill_executions", columns: ["skill_id", "create_time"]);
            migrationBuilder.CreateIndex(name: "ix_agent_skills_agent_id_is_enabled_sort_order", table: "agent_skills", columns: ["agent_id", "is_enabled", "sort_order"]);
            migrationBuilder.CreateIndex(name: "ix_agent_skills_agent_id_name", table: "agent_skills", columns: ["agent_id", "name"], unique: true, filter: "is_deleted = false");
            migrationBuilder.CreateIndex(name: "ix_agent_soul_histories_agent_id_create_time", table: "agent_soul_histories", columns: ["agent_id", "create_time"]);
            migrationBuilder.CreateIndex(name: "ix_agent_soul_histories_soul_section_id_create_time", table: "agent_soul_histories", columns: ["soul_section_id", "create_time"]);
            migrationBuilder.CreateIndex(name: "ix_agent_soul_sections_agent_id_section_type", table: "agent_soul_sections", columns: ["agent_id", "section_type"], unique: true, filter: "is_active = true AND is_deleted = false");
            migrationBuilder.CreateIndex(name: "ix_agent_soul_sections_agent_id_sort_order", table: "agent_soul_sections", columns: ["agent_id", "sort_order"], filter: "is_active = true AND is_deleted = false");
            migrationBuilder.CreateIndex(name: "ix_agents_is_default", table: "agents", column: "is_default", unique: true, filter: "is_default = true AND is_deleted = false");
            migrationBuilder.CreateIndex(name: "ix_agents_is_deleted_is_active", table: "agents", columns: ["is_deleted", "is_active"]);
            migrationBuilder.CreateIndex(name: "ix_agents_template_id", table: "agents", column: "template_id");

            // ═══════════════════════════════════════════════════════════════
            // DATA MIGRATION: Old tables → New Agent Architecture tables
            // ═══════════════════════════════════════════════════════════════

            migrationBuilder.Sql("""
                INSERT INTO agents (id, name, display_name, description, is_active, is_default, is_deleted, create_time)
                VALUES (
                    'a0000000-0000-0000-0000-000000000001'::uuid,
                    'klacks-assistant',
                    'Klacks Assistant',
                    'Default AI assistant for Klacks',
                    true, true, false, NOW()
                );
                """);

            migrationBuilder.Sql("""
                INSERT INTO agent_soul_sections (id, agent_id, section_type, content, sort_order, is_active, version, source, is_deleted, create_time)
                SELECT gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001'::uuid,
                    'identity', content, 0, true, 1, COALESCE(source, 'migration'), false, COALESCE(create_time, NOW())
                FROM ai_souls WHERE is_active = true AND is_deleted = false LIMIT 1;
                """);

            migrationBuilder.Sql("""
                INSERT INTO agent_soul_sections (id, agent_id, section_type, content, sort_order, is_active, version, source, is_deleted, create_time)
                SELECT gen_random_uuid(), 'a0000000-0000-0000-0000-000000000001'::uuid,
                    'boundaries', content, 1, true, 1, COALESCE(source, 'migration'), false, COALESCE(create_time, NOW())
                FROM ai_guidelines WHERE is_active = true AND is_deleted = false LIMIT 1;
                """);

            migrationBuilder.Sql("""
                INSERT INTO agent_memories (id, agent_id, category, key, content, importance, is_pinned, access_count, source, source_ref, metadata, is_deleted, create_time, update_time)
                SELECT id, 'a0000000-0000-0000-0000-000000000001'::uuid,
                    category, key, content, importance, false, 0,
                    COALESCE(source, 'migration'), NULL, '{}', is_deleted, COALESCE(create_time, NOW()), update_time
                FROM ai_memories;
                """);

            migrationBuilder.Sql("""
                INSERT INTO agent_sessions (id, agent_id, session_id, user_id, title, summary, status, message_count, token_count_est, compaction_count, active_categories, channel, last_message_at, last_model_id, is_archived, is_deleted, create_time, update_time)
                SELECT id, 'a0000000-0000-0000-0000-000000000001'::uuid,
                    conversation_id, COALESCE(user_id, 'system'), title, summary,
                    CASE WHEN is_archived THEN 'archived' ELSE 'active' END,
                    COALESCE(message_count, 0), COALESCE(total_tokens, 0), 0, '[]', 'web',
                    COALESCE(last_message_at, create_time, NOW()), last_model_id,
                    COALESCE(is_archived, false), is_deleted, COALESCE(create_time, NOW()), update_time
                FROM llm_conversations;
                """);

            migrationBuilder.Sql("""
                INSERT INTO agent_session_messages (id, session_id, role, content, token_count, model_id, function_calls, is_compacted, is_deleted, create_time, update_time)
                SELECT m.id, m.conversation_id, m.role, COALESCE(m.content, ''),
                    m.token_count, m.model_id, m.function_calls, false, m.is_deleted,
                    COALESCE(m.create_time, NOW()), m.update_time
                FROM llm_messages m
                WHERE EXISTS (SELECT 1 FROM llm_conversations c WHERE c.id = m.conversation_id);
                """);

            migrationBuilder.Sql("""
                INSERT INTO agent_skills (id, agent_id, name, description, parameters_json, required_permission, execution_type, category, is_enabled, sort_order, handler_type, handler_config, trigger_keywords, allowed_channels, always_on, version, is_deleted, create_time, update_time)
                SELECT id, 'a0000000-0000-0000-0000-000000000001'::uuid,
                    name, description, parameters_json, required_permission, execution_type, category,
                    is_enabled, sort_order, 'internal', '{}', '[]', '[]', false, 1,
                    is_deleted, COALESCE(create_time, NOW()), update_time
                FROM llm_function_definitions;
                """);

            // Full-Text Search for memories
            migrationBuilder.Sql("""
                CREATE INDEX IF NOT EXISTS ix_agent_memories_fts
                ON agent_memories USING gin (
                    (setweight(to_tsvector('german', coalesce(key, '')), 'A') ||
                     setweight(to_tsvector('german', coalesce(content, '')), 'B'))
                );
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "agent_links");
            migrationBuilder.DropTable(name: "agent_memory_tags");
            migrationBuilder.DropTable(name: "agent_session_messages");
            migrationBuilder.DropTable(name: "agent_skill_executions");
            migrationBuilder.DropTable(name: "agent_soul_histories");
            migrationBuilder.DropTable(name: "agent_memories");
            migrationBuilder.DropTable(name: "agent_sessions");
            migrationBuilder.DropTable(name: "agent_skills");
            migrationBuilder.DropTable(name: "agent_soul_sections");
            migrationBuilder.DropTable(name: "agents");
            migrationBuilder.DropTable(name: "agent_templates");
        }
    }
}
