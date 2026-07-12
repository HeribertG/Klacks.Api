using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateSequence<int>(
                name: "client_idnumber_seq",
                schema: "public");

            migrationBuilder.CreateTable(
                name: "absence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    abbreviation = table.Column<string>(type: "jsonb", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false),
                    default_length = table.Column<int>(type: "integer", nullable: false),
                    default_value = table.Column<double>(type: "double precision", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: false),
                    hide_in_gantt = table.Column<bool>(type: "boolean", nullable: false),
                    macro_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "jsonb", nullable: false),
                    undeletable = table.Column<bool>(type: "boolean", nullable: false),
                    with_holiday = table.Column<bool>(type: "boolean", nullable: false),
                    with_saturday = table.Column<bool>(type: "boolean", nullable: false),
                    with_sunday = table.Column<bool>(type: "boolean", nullable: false),
                    applies_to_container = table.Column<bool>(type: "boolean", nullable: false),
                    is_unpaid = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_absence", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agent_autonomy_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_agent_autonomy_preferences", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agent_plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    session_id = table.Column<Guid>(type: "uuid", nullable: true),
                    goal = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    steps_json = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    current_step_index = table.Column<int>(type: "integer", nullable: false),
                    last_error_message = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
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
                    table.PrimaryKey("pk_agent_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agent_recipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    goal = table.Column<string>(type: "text", nullable: false),
                    goal_translations = table.Column<string>(type: "jsonb", nullable: true),
                    trigger_json = table.Column<string>(type: "text", nullable: false),
                    steps_json = table.Column<string>(type: "text", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    synonyms = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("pk_agent_recipes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agent_trigger_dispatches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    trigger_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    dedup_key = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
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
                    table.PrimaryKey("pk_agent_trigger_dispatches", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "agent_trigger_preferences",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    trigger_kind = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    muted = table.Column<bool>(type: "boolean", nullable: false),
                    snoozed_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    minimum_severity = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
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
                    table.PrimaryKey("pk_agent_trigger_preferences", x => x.id);
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
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    id = table.Column<string>(type: "text", nullable: false),
                    discriminator = table.Column<string>(type: "character varying(13)", maxLength: 13, nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: true),
                    last_name = table.Column<string>(type: "text", nullable: true),
                    password_reset_token = table.Column<string>(type: "text", nullable: true),
                    password_reset_token_expires = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    normalized_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    email_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    security_stamp = table.Column<string>(type: "text", nullable: true),
                    concurrency_stamp = table.Column<string>(type: "text", nullable: true),
                    phone_number = table.Column<string>(type: "text", nullable: true),
                    phone_number_confirmed = table.Column<bool>(type: "boolean", nullable: false),
                    two_factor_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    lockout_end = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    lockout_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    access_failed_count = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_users", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "branch",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    address = table.Column<string>(type: "text", nullable: false),
                    phone = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_branch", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "calendar_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false),
                    rule = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    sub_rule = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_rule", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "calendar_selection",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    plugin_code = table.Column<string>(type: "text", nullable: true),
                    is_seeded = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_calendar_selection", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client_schedule_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_month = table.Column<int>(type: "integer", nullable: false),
                    current_year = table.Column<int>(type: "integer", nullable: false),
                    needed_rows = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_schedule_detail", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "client_sort_preference",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_client_sort_preference", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "communication_type",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    default_index = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_communication_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "container_lock",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    resource_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    resource_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    instance_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    acquired_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_heartbeat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_container_lock", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "countries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    abbreviation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false),
                    prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
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
                    table.PrimaryKey("pk_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "custom_stt_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    connection_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    api_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    api_key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    language_model = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_custom_stt_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "email_folders",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    imap_folder_name = table.Column<string>(type: "text", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_system = table.Column<bool>(type: "boolean", nullable: false),
                    special_use = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_email_folders", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "erp_drop_points",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    source_system_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    bucket_prefix = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    last_polled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
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
                    table.PrimaryKey("pk_erp_drop_points", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "erp_import_exception",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source_system_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    file_key = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    external_order_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    resolved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_erp_import_exception", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "eval_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    goldset = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    provider = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    model = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    composite_score = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: false),
                    dimensions_json = table.Column<string>(type: "jsonb", nullable: false),
                    regression_vs_baseline = table.Column<decimal>(type: "numeric(6,4)", precision: 6, scale: 4, nullable: true),
                    items_total = table.Column<int>(type: "integer", nullable: false),
                    items_passed = table.Column<int>(type: "integer", nullable: false),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_eval_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "export_format_override",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    format_key = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    patch_json = table.Column<string>(type: "text", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    note = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    created_under_version = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
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
                    table.PrimaryKey("pk_export_format_override", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "export_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    format = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    language = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    currency_code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    file_name = table.Column<string>(type: "character varying(260)", maxLength: 260, nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    record_count = table.Column<int>(type: "integer", nullable: false),
                    exported_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    exported_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    override_applied = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_export_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "global_agent_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_global_agent_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "heartbeat_configs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    interval_minutes = table.Column<int>(type: "integer", nullable: false),
                    active_hours_start = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    active_hours_end = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    checklist_json = table.Column<string>(type: "text", nullable: false),
                    last_executed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    onboarding_completed = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_heartbeat_configs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "identity_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    use_for_authentication = table.Column<bool>(type: "boolean", nullable: false),
                    use_for_client_import = table.Column<bool>(type: "boolean", nullable: false),
                    host = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    port = table.Column<int>(type: "integer", nullable: true),
                    use_ssl = table.Column<bool>(type: "boolean", nullable: false),
                    base_dn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bind_dn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    bind_password = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    user_filter = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    client_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    client_secret = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    authorization_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    token_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    user_info_url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    scopes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    tenant_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    last_sync_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_sync_count = table.Column<int>(type: "integer", nullable: true),
                    last_sync_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    attribute_mapping = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("pk_identity_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "individual_period",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_individual_period", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "klacks_bot_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    token_prefix = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_klacks_bot_token", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "klacksy_navigation_feedback",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    utterance = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    locale = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    matched_target_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    matched_score = table.Column<double>(type: "double precision", nullable: true),
                    user_action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    actual_route = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_klacksy_navigation_feedback", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "knowledge_index",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<short>(type: "smallint", nullable: false),
                    source_id = table.Column<string>(type: "text", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    text_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    required_permission = table.Column<string>(type: "text", nullable: true),
                    exposed_endpoint_key = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knowledge_index", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "llm_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    api_key = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    requires_api_key = table.Column<bool>(type: "boolean", nullable: false),
                    base_url = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    api_version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    priority = table.Column<int>(type: "integer", nullable: false),
                    settings = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("pk_llm_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "llm_sync_notifications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    new_models_count = table.Column<int>(type: "integer", nullable: false),
                    deactivated_models_count = table.Column<int>(type: "integer", nullable: false),
                    new_model_names = table.Column<string>(type: "jsonb", nullable: false),
                    deactivated_model_names = table.Column<string>(type: "jsonb", nullable: false),
                    failed_models_count = table.Column<int>(type: "integer", nullable: false),
                    model_test_results = table.Column<string>(type: "jsonb", nullable: false),
                    synced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_llm_sync_notifications", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "macro",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_macro", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "messaging_providers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    provider_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    config_json = table.Column<string>(type: "text", nullable: false),
                    webhook_secret = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messaging_providers", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "messenger_contact",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messenger_contact", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "navigation_target_synonyms",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_id = table.Column<string>(type: "text", nullable: false),
                    language = table.Column<string>(type: "text", nullable: false),
                    keyword = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_navigation_target_synonyms", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oauth_clients",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<string>(type: "text", nullable: false),
                    client_name = table.Column<string>(type: "text", nullable: false),
                    redirect_uris_json = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_oauth_clients", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "payroll_export_group_config",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_system = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    delimiter = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: false),
                    encoding = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    base_wage_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    surcharge_wage_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    absence_mapping_json = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_payroll_export_group_config", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "period_audit_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    action = table.Column<int>(type: "integer", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    affected_count = table.Column<int>(type: "integer", nullable: false),
                    performed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    performed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
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
                    table.PrimaryKey("pk_period_audit_log", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "plugin_docs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    plugin_code = table.Column<string>(type: "text", nullable: false),
                    manual_name = table.Column<string>(type: "text", nullable: false),
                    html_content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_plugin_docs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "postcode_ch",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    city = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    zip = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_postcode_ch", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "proposed_skill_changes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    field = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    value_before = table.Column<string>(type: "text", nullable: false),
                    value_after = table.Column<string>(type: "text", nullable: false),
                    justification = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    evidence_json = table.Column<string>(type: "jsonb", nullable: false),
                    reviewed_by = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    reviewed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_proposed_skill_changes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "qualification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    emoji = table.Column<string>(type: "text", nullable: true),
                    is_time_limited = table.Column<bool>(type: "boolean", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false, defaultValue: 2),
                    category = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("pk_qualification", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "received_emails",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    message_id = table.Column<string>(type: "text", nullable: false),
                    imap_uid = table.Column<long>(type: "bigint", nullable: false),
                    folder = table.Column<string>(type: "text", nullable: false),
                    source_imap_folder = table.Column<string>(type: "text", nullable: false),
                    from_address = table.Column<string>(type: "text", nullable: false),
                    from_name = table.Column<string>(type: "text", nullable: true),
                    to_address = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    body_html = table.Column<string>(type: "text", nullable: true),
                    body_text = table.Column<string>(type: "text", nullable: true),
                    received_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_read = table.Column<bool>(type: "boolean", nullable: false),
                    has_attachments = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_received_emails", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "refresh_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    asp_net_users_id = table.Column<string>(type: "text", nullable: false),
                    token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    expiry_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_refresh_token", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "report_templates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    source_id = table.Column<string>(type: "text", nullable: false),
                    data_set_ids = table.Column<string>(type: "jsonb", nullable: false),
                    page_setup = table.Column<string>(type: "jsonb", nullable: false),
                    sections = table.Column<string>(type: "jsonb", nullable: false),
                    merge_rows = table.Column<bool>(type: "boolean", nullable: false),
                    show_full_period = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_report_templates", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "schedule_change",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_date = table.Column<DateOnly>(type: "date", nullable: false),
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
                    table.PrimaryKey("pk_schedule_change", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scheduled_tasks",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    cron_expression = table.Column<string>(type: "text", nullable: false),
                    time_zone_id = table.Column<string>(type: "text", nullable: false),
                    action_type = table.Column<string>(type: "text", nullable: false),
                    message_text = table.Column<string>(type: "text", nullable: true),
                    skill_name = table.Column<string>(type: "text", nullable: true),
                    parameters_json = table.Column<string>(type: "text", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_name = table.Column<string>(type: "text", nullable: false),
                    owner_permissions_csv = table.Column<string>(type: "text", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    next_run_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_run_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_status = table.Column<string>(type: "text", nullable: true),
                    last_result = table.Column<string>(type: "text", nullable: true),
                    run_count = table.Column<int>(type: "integer", nullable: false),
                    max_runs = table.Column<int>(type: "integer", nullable: true),
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
                    table.PrimaryKey("pk_scheduled_tasks", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "scheduling_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    max_work_days = table.Column<int>(type: "integer", nullable: true),
                    min_rest_days = table.Column<int>(type: "integer", nullable: true),
                    min_pause_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    max_optimal_gap = table.Column<decimal>(type: "numeric", nullable: true),
                    max_daily_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    max_weekly_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    max_consecutive_days = table.Column<int>(type: "integer", nullable: true),
                    default_working_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    overtime_threshold = table.Column<decimal>(type: "numeric", nullable: true),
                    guaranteed_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    maximum_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    minimum_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    full_time_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    vacation_days_per_year = table.Column<int>(type: "integer", nullable: true),
                    night_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    holiday_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    we1rate = table.Column<decimal>(type: "numeric", nullable: true),
                    we2rate = table.Column<decimal>(type: "numeric", nullable: true),
                    we3rate = table.Column<decimal>(type: "numeric", nullable: true),
                    work_on_monday = table.Column<bool>(type: "boolean", nullable: true),
                    work_on_tuesday = table.Column<bool>(type: "boolean", nullable: true),
                    work_on_wednesday = table.Column<bool>(type: "boolean", nullable: true),
                    work_on_thursday = table.Column<bool>(type: "boolean", nullable: true),
                    work_on_friday = table.Column<bool>(type: "boolean", nullable: true),
                    work_on_saturday = table.Column<bool>(type: "boolean", nullable: true),
                    work_on_sunday = table.Column<bool>(type: "boolean", nullable: true),
                    performs_shift_work = table.Column<bool>(type: "boolean", nullable: true),
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
                    table.PrimaryKey("pk_scheduling_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sealed_day",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    level = table.Column<int>(type: "integer", nullable: false),
                    reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    sealed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    sealed_by = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
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
                    table.PrimaryKey("pk_sealed_day", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "sentiment_keyword_sets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    language = table.Column<string>(type: "text", nullable: false),
                    keywords = table.Column<string>(type: "jsonb", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_sentiment_keyword_sets", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "settings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settings", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "shift_day_assignments",
                columns: table => new
                {
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    day_of_week = table.Column<int>(type: "integer", nullable: false),
                    shift_name = table.Column<string>(type: "text", nullable: false),
                    abbreviation = table.Column<string>(type: "text", nullable: false),
                    start_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    work_time = table.Column<decimal>(type: "numeric", nullable: false),
                    is_sporadic = table.Column<bool>(type: "boolean", nullable: false),
                    is_time_range = table.Column<bool>(type: "boolean", nullable: false),
                    shift_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    is_in_template_container = table.Column<bool>(type: "boolean", nullable: false),
                    sum_employees = table.Column<int>(type: "integer", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    sporadic_scope = table.Column<int>(type: "integer", nullable: false),
                    engaged = table.Column<int>(type: "integer", nullable: false),
                    sporadic_status = table.Column<short>(type: "smallint", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "skill_gap_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_message = table.Column<string>(type: "text", nullable: false),
                    detected_intent = table.Column<string>(type: "text", nullable: false),
                    occurrence_count = table.Column<int>(type: "integer", nullable: false),
                    suggested_skill_name = table.Column<string>(type: "text", nullable: true),
                    suggested_description = table.Column<string>(type: "text", nullable: true),
                    status = table.Column<string>(type: "text", nullable: false),
                    first_detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_detected_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    embedding = table.Column<float[]>(type: "real[]", nullable: true),
                    normalized_message_hash = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_skill_gap_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "skill_usage_records",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_name = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<int>(type: "integer", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tenant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<int>(type: "integer", nullable: true),
                    model_id = table.Column<string>(type: "text", nullable: true),
                    session_id = table.Column<string>(type: "text", nullable: true),
                    parameters_json = table.Column<string>(type: "text", nullable: true),
                    success = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    duration_ms = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_skill_usage_records", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "spam_rules",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    rule_type = table.Column<int>(type: "integer", nullable: false),
                    pattern = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_spam_rules", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "state",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    abbreviation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    country_prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false),
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
                    table.PrimaryKey("pk_state", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "telegram_onboarding_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    redeemed_chat_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_telegram_onboarding_token", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "transcription_dictionary_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    correct_term = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    phonetic_variants = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'[]'::jsonb"),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    language = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
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
                    table.PrimaryKey("pk_transcription_dictionary_entries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ui_controls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_key = table.Column<string>(type: "text", nullable: false),
                    control_key = table.Column<string>(type: "text", nullable: false),
                    selector = table.Column<string>(type: "text", nullable: false),
                    selector_type = table.Column<string>(type: "text", nullable: false),
                    control_type = table.Column<string>(type: "text", nullable: false),
                    label = table.Column<string>(type: "text", nullable: true),
                    route = table.Column<string>(type: "text", nullable: true),
                    parent_control_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false),
                    is_dynamic = table.Column<bool>(type: "boolean", nullable: false),
                    selector_pattern = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_ui_controls", x => x.id);
                    table.ForeignKey(
                        name: "fk_ui_controls_ui_controls_parent_control_id",
                        column: x => x.parent_control_id,
                        principalTable: "ui_controls",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "update_history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    operation_type = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    channel = table.Column<int>(type: "integer", nullable: false),
                    from_version = table.Column<string>(type: "text", nullable: false),
                    target_version = table.Column<string>(type: "text", nullable: false),
                    artifact_ref = table.Column<string>(type: "text", nullable: true),
                    artifact_sha256 = table.Column<string>(type: "text", nullable: true),
                    artifact_signature = table.Column<string>(type: "text", nullable: true),
                    contains_migrations = table.Column<bool>(type: "boolean", nullable: false),
                    backup_ref = table.Column<string>(type: "text", nullable: true),
                    related_operation_id = table.Column<Guid>(type: "uuid", nullable: true),
                    requested_by = table.Column<string>(type: "text", nullable: false),
                    requested_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_heartbeat_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    message = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_update_history", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "wizard_training_runs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    source = table.Column<string>(type: "text", nullable: false),
                    config_json = table.Column<string>(type: "text", nullable: false),
                    duration_ms = table.Column<long>(type: "bigint", nullable: false),
                    stage0violations = table.Column<int>(type: "integer", nullable: false),
                    stage1completion = table.Column<double>(type: "double precision", nullable: false),
                    stage2score = table.Column<double>(type: "double precision", nullable: false),
                    token_count = table.Column<int>(type: "integer", nullable: false),
                    available_shift_slots = table.Column<int>(type: "integer", nullable: false),
                    coverage_ratio = table.Column<double>(type: "double precision", nullable: false),
                    client_day_duplicates = table.Column<int>(type: "integer", nullable: false),
                    agents_count = table.Column<int>(type: "integer", nullable: false),
                    shifts_count = table.Column<int>(type: "integer", nullable: false),
                    period_days = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_wizard_training_runs", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "work_softening",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    kind = table.Column<byte>(type: "smallint", nullable: false),
                    rule_name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    hint = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_work_softening", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "absence_detail",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    absence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    mode = table.Column<int>(type: "integer", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    duration = table.Column<decimal>(type: "numeric", nullable: false),
                    detail_name = table.Column<string>(type: "jsonb", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: true),
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
                    table.PrimaryKey("pk_absence_detail", x => x.id);
                    table.ForeignKey(
                        name: "fk_absence_detail_absence_absence_id",
                        column: x => x.absence_id,
                        principalTable: "absence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "skill_selection_trajectories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    turn_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: true),
                    locale = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    user_message_hash = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    intent_excerpt = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    knowledge_index_candidates_json = table.Column<string>(type: "jsonb", nullable: false),
                    llm_chosen_skill = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    was_executed = table.Column<bool>(type: "boolean", nullable: false),
                    had_mutation_intent = table.Column<bool>(type: "boolean", nullable: false),
                    was_corrected = table.Column<bool>(type: "boolean", nullable: false),
                    correction_type = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    latency_ms_total = table.Column<int>(type: "integer", nullable: false),
                    latency_ms_knowledge = table.Column<int>(type: "integer", nullable: false),
                    latency_ms_llm = table.Column<int>(type: "integer", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_skill_selection_trajectories", x => x.id);
                    table.ForeignKey(
                        name: "fk_skill_selection_trajectories_agent_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "agent_plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "agent_memories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    synonyms = table.Column<string>(type: "jsonb", nullable: true),
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
                name: "pending_user_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    content = table.Column<string>(type: "text", nullable: false),
                    topic = table.Column<string>(type: "text", nullable: true),
                    first_delivered_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_pending_user_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_pending_user_notes_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "skill_relations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    agent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    skill_a_name = table.Column<string>(type: "text", nullable: false),
                    skill_b_name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    confidence = table.Column<double>(type: "double precision", nullable: false),
                    support_count = table.Column<int>(type: "integer", nullable: false),
                    contradiction_count = table.Column<int>(type: "integer", nullable: false),
                    provenance = table.Column<string>(type: "text", nullable: false),
                    source = table.Column<int>(type: "integer", nullable: false),
                    last_reinforced_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_skill_relations", x => x.id);
                    table.ForeignKey(
                        name: "fk_skill_relations_agents_agent_id",
                        column: x => x.agent_id,
                        principalTable: "agents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    role_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_role_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_role_claims_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    claim_type = table.Column<string>(type: "text", nullable: true),
                    claim_value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_claims", x => x.id);
                    table.ForeignKey(
                        name: "fk_asp_net_user_claims_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    provider_key = table.Column<string>(type: "text", nullable: false),
                    provider_display_name = table.Column<string>(type: "text", nullable: true),
                    user_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_logins", x => new { x.login_provider, x.provider_key });
                    table.ForeignKey(
                        name: "fk_asp_net_user_logins_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    role_id = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_roles", x => new { x.user_id, x.role_id });
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "AspNetRoles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_asp_net_user_roles_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    user_id = table.Column<string>(type: "text", nullable: false),
                    login_provider = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_asp_net_user_tokens", x => new { x.user_id, x.login_provider, x.name });
                    table.ForeignKey(
                        name: "fk_asp_net_user_tokens_asp_net_users_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "llm_conversations",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    summary = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_message_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    message_count = table.Column<int>(type: "integer", nullable: false),
                    total_tokens = table.Column<int>(type: "integer", nullable: false),
                    total_cost = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    last_model_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
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
                    table.PrimaryKey("pk_llm_conversations", x => x.id);
                    table.ForeignKey(
                        name: "fk_llm_conversations_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "personal_access_tokens",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    token_prefix = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_personal_access_tokens", x => x.id);
                    table.ForeignKey(
                        name: "fk_personal_access_tokens_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_interval = table.Column<int>(type: "integer", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
                    calendar_selection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent = table.Column<Guid>(type: "uuid", nullable: true),
                    root = table.Column<Guid>(type: "uuid", nullable: true),
                    lft = table.Column<int>(type: "integer", nullable: false),
                    rgt = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_group", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_calendar_selection_calendar_selection_id",
                        column: x => x.calendar_selection_id,
                        principalTable: "calendar_selection",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "selected_calendar",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    calendar_selection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_selected_calendar", x => x.id);
                    table.ForeignKey(
                        name: "fk_selected_calendar_calendar_selection_calendar_selection_id",
                        column: x => x.calendar_selection_id,
                        principalTable: "calendar_selection",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "erp_import_token",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    drop_point_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    token_hash = table.Column<string>(type: "text", nullable: false),
                    token_prefix = table.Column<string>(type: "text", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_erp_import_token", x => x.id);
                    table.ForeignKey(
                        name: "fk_erp_import_token_erp_drop_points_drop_point_id",
                        column: x => x.drop_point_id,
                        principalTable: "erp_drop_points",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "global_agent_rule_histories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    global_agent_rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_global_agent_rule_histories", x => x.id);
                    table.ForeignKey(
                        name: "fk_global_agent_rule_histories_global_agent_rules_global_agent",
                        column: x => x.global_agent_rule_id,
                        principalTable: "global_agent_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    birthdate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    company = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    first_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    gender = table.Column<int>(type: "integer", nullable: false),
                    id_number = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('public.client_idnumber_seq')"),
                    legal_entity = table.Column<bool>(type: "boolean", nullable: false),
                    maiden_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    passwort_reset_token = table.Column<string>(type: "text", nullable: true),
                    phonetic_tokens = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: true),
                    second_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    identity_provider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ldap_external_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    source_system_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    external_customer_reference = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("pk_client", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_identity_providers_identity_provider_id",
                        column: x => x.identity_provider_id,
                        principalTable: "identity_providers",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "period",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    individual_period_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    until_date = table.Column<DateOnly>(type: "date", nullable: true),
                    full_hours = table.Column<decimal>(type: "numeric", nullable: false),
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
                    table.PrimaryKey("pk_period", x => x.id);
                    table.ForeignKey(
                        name: "fk_period_individual_period_individual_period_id",
                        column: x => x.individual_period_id,
                        principalTable: "individual_period",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "llm_models",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    model_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    model_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    api_model_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    provider_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    cost_per_input_token = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    cost_per_output_token = table.Column<decimal>(type: "numeric(10,6)", nullable: false),
                    max_tokens = table.Column<int>(type: "integer", nullable: false),
                    context_window = table.Column<int>(type: "integer", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    released_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    deprecated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    llm_provider_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_llm_models", x => x.id);
                    table.ForeignKey(
                        name: "fk_llm_models_llm_providers_llm_provider_id",
                        column: x => x.llm_provider_id,
                        principalTable: "llm_providers",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    broadcast_id = table.Column<Guid>(type: "uuid", nullable: true),
                    external_message_id = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    sender = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    sender_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    recipient = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    recipient_display_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    content_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    direction = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    media_url = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_messages_messaging_provider_provider_id",
                        column: x => x.provider_id,
                        principalTable: "messaging_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "qualification_country",
                columns: table => new
                {
                    qualification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country_code = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qualification_country", x => new { x.qualification_id, x.country_code });
                    table.ForeignKey(
                        name: "fk_qualification_country_qualification_qualification_id",
                        column: x => x.qualification_id,
                        principalTable: "qualification",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "email_analyses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    received_email_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    client_type = table.Column<int>(type: "integer", nullable: true),
                    intent = table.Column<int>(type: "integer", nullable: false),
                    summary = table.Column<string>(type: "text", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: true),
                    until_date = table.Column<DateOnly>(type: "date", nullable: true),
                    start_hour = table.Column<int>(type: "integer", nullable: true),
                    end_hour = table.Column<int>(type: "integer", nullable: true),
                    weekdays = table.Column<string>(type: "text", nullable: true),
                    schedule_commands = table.Column<string>(type: "text", nullable: true),
                    analyzed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    failure_reason = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_email_analyses", x => x.id);
                    table.ForeignKey(
                        name: "fk_email_analyses_received_emails_received_email_id",
                        column: x => x.received_email_id,
                        principalTable: "received_emails",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "contract",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    guaranteed_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    maximum_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    minimum_hours = table.Column<decimal>(type: "numeric", nullable: true),
                    full_time = table.Column<decimal>(type: "numeric", nullable: true),
                    night_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    holiday_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    we1rate = table.Column<decimal>(type: "numeric", nullable: true),
                    we2rate = table.Column<decimal>(type: "numeric", nullable: true),
                    we3rate = table.Column<decimal>(type: "numeric", nullable: true),
                    payment_interval = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    calendar_selection_id = table.Column<Guid>(type: "uuid", nullable: true),
                    work_on_monday = table.Column<bool>(type: "boolean", nullable: false),
                    work_on_tuesday = table.Column<bool>(type: "boolean", nullable: false),
                    work_on_wednesday = table.Column<bool>(type: "boolean", nullable: false),
                    work_on_thursday = table.Column<bool>(type: "boolean", nullable: false),
                    work_on_friday = table.Column<bool>(type: "boolean", nullable: false),
                    work_on_saturday = table.Column<bool>(type: "boolean", nullable: false),
                    work_on_sunday = table.Column<bool>(type: "boolean", nullable: false),
                    performs_shift_work = table.Column<bool>(type: "boolean", nullable: false),
                    scheduling_rule_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_contract", x => x.id);
                    table.ForeignKey(
                        name: "fk_contract_calendar_selection_calendar_selection_id",
                        column: x => x.calendar_selection_id,
                        principalTable: "calendar_selection",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_contract_scheduling_rules_scheduling_rule_id",
                        column: x => x.scheduling_rule_id,
                        principalTable: "scheduling_rules",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
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

            migrationBuilder.CreateTable(
                name: "llm_messages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    token_count = table.Column<int>(type: "integer", nullable: true),
                    model_id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    function_calls = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_llm_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_llm_messages_llm_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "llm_conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "analyse_scenarios",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    until_date = table.Column<DateOnly>(type: "date", nullable: false),
                    token = table.Column<Guid>(type: "uuid", nullable: false),
                    run_group_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_by_user = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    sub_score_json = table.Column<string>(type: "text", nullable: true),
                    churn_ratio = table.Column<double>(type: "double precision", nullable: true),
                    stage0violations = table.Column<int>(type: "integer", nullable: true),
                    reject_reason = table.Column<int>(type: "integer", nullable: true),
                    reject_reason_text = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
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
                    table.PrimaryKey("pk_analyse_scenarios", x => x.id);
                    table.ForeignKey(
                        name: "fk_analyse_scenarios_group_group_id",
                        column: x => x.group_id,
                        principalTable: "group",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "group_visibility",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    app_user_id = table.Column<string>(type: "text", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_group_visibility", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_visibility_app_user_app_user_id",
                        column: x => x.app_user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_visibility_group_group_id",
                        column: x => x.group_id,
                        principalTable: "group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "address",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    address_line1 = table.Column<string>(type: "text", nullable: false),
                    address_line2 = table.Column<string>(type: "text", nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    street2 = table.Column<string>(type: "text", nullable: false),
                    street3 = table.Column<string>(type: "text", nullable: false),
                    zip = table.Column<string>(type: "text", nullable: false),
                    city = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    latitude = table.Column<double>(type: "double precision", nullable: true),
                    longitude = table.Column<double>(type: "double precision", nullable: true),
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
                    table.PrimaryKey("pk_address", x => x.id);
                    table.ForeignKey(
                        name: "fk_address_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "annotation",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    note = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_annotation", x => x.id);
                    table.ForeignKey(
                        name: "fk_annotation_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "assigned_group",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
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
                    table.PrimaryKey("pk_assigned_group", x => x.id);
                    table.ForeignKey(
                        name: "fk_assigned_group_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_assigned_group_group_group_id",
                        column: x => x.group_id,
                        principalTable: "group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "break_placeholder",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    absence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    information = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("pk_break_placeholder", x => x.id);
                    table.ForeignKey(
                        name: "fk_break_placeholder_absence_absence_id",
                        column: x => x.absence_id,
                        principalTable: "absence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_break_placeholder_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_availability",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    hour = table.Column<int>(type: "integer", nullable: false),
                    is_available = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_client_availability", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_availability_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_image",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    image_data = table.Column<byte[]>(type: "bytea", nullable: false),
                    content_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    file_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client_image", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_image_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_period_hours",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_date = table.Column<DateOnly>(type: "date", nullable: false),
                    end_date = table.Column<DateOnly>(type: "date", nullable: false),
                    hours = table.Column<decimal>(type: "numeric", nullable: false),
                    surcharges = table.Column<decimal>(type: "numeric", nullable: false),
                    payment_interval = table.Column<int>(type: "integer", nullable: false),
                    individual_period_id = table.Column<Guid>(type: "uuid", nullable: true),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true),
                    calculated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
                    table.PrimaryKey("pk_client_period_hours", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_period_hours_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_period_hours_individual_period_individual_period_id",
                        column: x => x.individual_period_id,
                        principalTable: "individual_period",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "client_qualification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    qualification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    level = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateOnly>(type: "date", nullable: true),
                    valid_until = table.Column<DateOnly>(type: "date", nullable: true),
                    note = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_client_qualification", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_qualification_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_qualification_qualification_qualification_id",
                        column: x => x.qualification_id,
                        principalTable: "qualification",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "communication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prefix = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_communication", x => x.id);
                    table.ForeignKey(
                        name: "fk_communication_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "history",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    data = table.Column<string>(type: "text", nullable: false),
                    old_data = table.Column<string>(type: "text", nullable: false),
                    new_data = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("pk_history", x => x.id);
                    table.ForeignKey(
                        name: "fk_history_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "identity_provider_sync_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    identity_provider_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    external_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    external_dn = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    last_sync_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active_in_source = table.Column<bool>(type: "boolean", nullable: false),
                    sync_error = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("pk_identity_provider_sync_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_identity_provider_sync_logs_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_identity_provider_sync_logs_identity_providers_identity_pro",
                        column: x => x.identity_provider_id,
                        principalTable: "identity_providers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "membership",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("pk_membership", x => x.id);
                    table.ForeignKey(
                        name: "fk_membership_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_commands",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workday = table.Column<DateOnly>(type: "date", nullable: false),
                    command_keyword = table.Column<string>(type: "text", nullable: false),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_schedule_commands", x => x.id);
                    table.ForeignKey(
                        name: "fk_schedule_commands_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "schedule_notes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workday = table.Column<DateOnly>(type: "date", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_schedule_notes", x => x.id);
                    table.ForeignKey(
                        name: "fk_schedule_notes_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shift",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cutting_after_midnight = table.Column<bool>(type: "boolean", nullable: false),
                    abbreviation = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    macro_id = table.Column<Guid>(type: "uuid", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    after_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    before_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    until_date = table.Column<DateOnly>(type: "date", nullable: true),
                    briefing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    debriefing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    travel_time_after = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    travel_time_before = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    is_friday = table.Column<bool>(type: "boolean", nullable: false),
                    is_holiday = table.Column<bool>(type: "boolean", nullable: false),
                    is_monday = table.Column<bool>(type: "boolean", nullable: false),
                    is_saturday = table.Column<bool>(type: "boolean", nullable: false),
                    is_sunday = table.Column<bool>(type: "boolean", nullable: false),
                    is_thursday = table.Column<bool>(type: "boolean", nullable: false),
                    is_tuesday = table.Column<bool>(type: "boolean", nullable: false),
                    is_wednesday = table.Column<bool>(type: "boolean", nullable: false),
                    is_weekday_and_holiday = table.Column<bool>(type: "boolean", nullable: false),
                    is_sporadic = table.Column<bool>(type: "boolean", nullable: false),
                    sporadic_scope = table.Column<int>(type: "integer", nullable: false),
                    is_time_range = table.Column<bool>(type: "boolean", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    sum_employees = table.Column<int>(type: "integer", nullable: false),
                    work_time = table.Column<decimal>(type: "numeric", nullable: false),
                    shift_type = table.Column<int>(type: "integer", nullable: false),
                    original_id = table.Column<Guid>(type: "uuid", nullable: true),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    root_id = table.Column<Guid>(type: "uuid", nullable: true),
                    lft = table.Column<int>(type: "integer", nullable: true),
                    rgt = table.Column<int>(type: "integer", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true),
                    scenario_source_shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    source_child_count_snapshot = table.Column<int>(type: "integer", nullable: true),
                    source_system_id = table.Column<string>(type: "text", nullable: true),
                    external_order_reference = table.Column<string>(type: "text", nullable: true),
                    supersedes_order_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_shift", x => x.id);
                    table.ForeignKey(
                        name: "fk_shift_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_shift_shift_scenario_source_shift_id",
                        column: x => x.scenario_source_shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "llm_usages",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<string>(type: "text", nullable: false),
                    model_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    input_tokens = table.Column<int>(type: "integer", nullable: false),
                    output_tokens = table.Column<int>(type: "integer", nullable: false),
                    cost = table.Column<decimal>(type: "numeric(10,4)", nullable: false),
                    user_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    assistant_message = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    response_time_ms = table.Column<int>(type: "integer", nullable: false),
                    has_error = table.Column<bool>(type: "boolean", nullable: false),
                    error_message = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    functions_called = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
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
                    table.PrimaryKey("pk_llm_usages", x => x.id);
                    table.ForeignKey(
                        name: "fk_llm_usages_app_user_user_id",
                        column: x => x.user_id,
                        principalTable: "AspNetUsers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_llm_usages_llm_models_model_id",
                        column: x => x.model_id,
                        principalTable: "llm_models",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "client_contract",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contract_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    until_date = table.Column<DateOnly>(type: "date", nullable: true),
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
                    table.PrimaryKey("pk_client_contract", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_contract_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_contract_contract_contract_id",
                        column: x => x.contract_id,
                        principalTable: "contract",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "client_shift_preference",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    preference_type = table.Column<int>(type: "integer", nullable: false),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_client_shift_preference", x => x.id);
                    table.ForeignKey(
                        name: "fk_client_shift_preference_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_client_shift_preference_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "container_shift_overrides",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_id = table.Column<Guid>(type: "uuid", nullable: false),
                    date = table.Column<DateOnly>(type: "date", nullable: false),
                    from_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    until_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    start_base = table.Column<string>(type: "text", nullable: true),
                    end_base = table.Column<string>(type: "text", nullable: true),
                    route_info = table.Column<string>(type: "jsonb", nullable: true),
                    transport_mode = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_container_shift_overrides", x => x.id);
                    table.ForeignKey(
                        name: "fk_container_shift_overrides_shift_container_id",
                        column: x => x.container_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "container_template",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    until_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    weekday = table.Column<int>(type: "integer", nullable: false),
                    is_weekday_and_holiday = table.Column<bool>(type: "boolean", nullable: false),
                    is_holiday = table.Column<bool>(type: "boolean", nullable: false),
                    start_base = table.Column<string>(type: "text", nullable: true),
                    end_base = table.Column<string>(type: "text", nullable: true),
                    route_info = table.Column<string>(type: "jsonb", nullable: true),
                    transport_mode = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_container_template", x => x.id);
                    table.ForeignKey(
                        name: "fk_container_template_shift_container_id",
                        column: x => x.container_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "group_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true),
                    scenario_source_group_item_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_group_item", x => x.id);
                    table.ForeignKey(
                        name: "fk_group_item_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_item_group_group_id",
                        column: x => x.group_id,
                        principalTable: "group",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_group_item_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shift_expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    taxable = table.Column<bool>(type: "boolean", nullable: false),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_shift_expenses", x => x.id);
                    table.ForeignKey(
                        name: "fk_shift_expenses_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shift_required_qualification",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    qualification_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    min_level = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_shift_required_qualification", x => x.id);
                    table.ForeignKey(
                        name: "fk_shift_required_qualification_qualification_qualification_id",
                        column: x => x.qualification_id,
                        principalTable: "qualification",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_shift_required_qualification_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_work_id = table.Column<Guid>(type: "uuid", nullable: true),
                    transport_mode = table.Column<int>(type: "integer", nullable: true),
                    start_base = table.Column<string>(type: "text", nullable: true),
                    end_base = table.Column<string>(type: "text", nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workday = table.Column<DateOnly>(type: "date", nullable: false),
                    information = table.Column<string>(type: "text", nullable: true),
                    work_time = table.Column<decimal>(type: "numeric", nullable: false),
                    surcharges = table.Column<decimal>(type: "numeric", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    lock_level = table.Column<int>(type: "integer", nullable: false),
                    sealed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sealed_by = table.Column<string>(type: "text", nullable: true),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_work", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_work_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_work_work_parent_work_id",
                        column: x => x.parent_work_id,
                        principalTable: "work",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "container_shift_override_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_shift_override_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    absence_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_item = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_item = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    briefing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    debriefing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    travel_time_after = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    travel_time_before = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_range_start_item = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    time_range_end_item = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    transport_mode = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_container_shift_override_items", x => x.id);
                    table.CheckConstraint("CK_ContainerShiftOverrideItem_ShiftXorAbsence", "(shift_id IS NOT NULL AND absence_id IS NULL) OR (shift_id IS NULL AND absence_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_container_shift_override_items_absence_absence_id",
                        column: x => x.absence_id,
                        principalTable: "absence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_container_shift_override_items_container_shift_overrides_co",
                        column: x => x.container_shift_override_id,
                        principalTable: "container_shift_overrides",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_container_shift_override_items_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "container_template_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: true),
                    absence_id = table.Column<Guid>(type: "uuid", nullable: true),
                    start_item = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_item = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    briefing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    debriefing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    travel_time_after = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    travel_time_before = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_range_start_item = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    time_range_end_item = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    transport_mode = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_container_template_item", x => x.id);
                    table.CheckConstraint("CK_ContainerTemplateItem_ShiftXorAbsence", "(shift_id IS NOT NULL AND absence_id IS NULL) OR (shift_id IS NULL AND absence_id IS NOT NULL)");
                    table.ForeignKey(
                        name: "fk_container_template_item_absence_absence_id",
                        column: x => x.absence_id,
                        principalTable: "absence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_container_template_item_container_template_container_templa",
                        column: x => x.container_template_id,
                        principalTable: "container_template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_container_template_item_shift_shift_id",
                        column: x => x.shift_id,
                        principalTable: "shift",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "break",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    absence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    parent_work_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    workday = table.Column<DateOnly>(type: "date", nullable: false),
                    information = table.Column<string>(type: "text", nullable: true),
                    work_time = table.Column<decimal>(type: "numeric", nullable: false),
                    surcharges = table.Column<decimal>(type: "numeric", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    lock_level = table.Column<int>(type: "integer", nullable: false),
                    sealed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sealed_by = table.Column<string>(type: "text", nullable: true),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_break", x => x.id);
                    table.ForeignKey(
                        name: "fk_break_absence_absence_id",
                        column: x => x.absence_id,
                        principalTable: "absence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_break_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_break_work_parent_work_id",
                        column: x => x.parent_work_id,
                        principalTable: "work",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "expenses",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    taxable = table.Column<bool>(type: "boolean", nullable: false),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_expenses", x => x.id);
                    table.ForeignKey(
                        name: "fk_expenses_work_work_id",
                        column: x => x.work_id,
                        principalTable: "work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "work_change",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    work_id = table.Column<Guid>(type: "uuid", nullable: false),
                    change_time = table.Column<decimal>(type: "numeric", nullable: false),
                    surcharges = table.Column<decimal>(type: "numeric", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    replace_client_id = table.Column<Guid>(type: "uuid", nullable: true),
                    description = table.Column<string>(type: "text", nullable: false),
                    to_invoice = table.Column<bool>(type: "boolean", nullable: false),
                    analyse_token = table.Column<Guid>(type: "uuid", nullable: true),
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
                    table.PrimaryKey("pk_work_change", x => x.id);
                    table.ForeignKey(
                        name: "fk_work_change_client_replace_client_id",
                        column: x => x.replace_client_id,
                        principalTable: "client",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "fk_work_change_work_work_id",
                        column: x => x.work_id,
                        principalTable: "work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "surcharge_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    amount = table.Column<decimal>(type: "numeric", nullable: false),
                    work_id = table.Column<Guid>(type: "uuid", nullable: true),
                    break_id = table.Column<Guid>(type: "uuid", nullable: true),
                    work_change_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_surcharge_item", x => x.id);
                    table.CheckConstraint("CK_SurchargeItem_ExactlyOneParent", "((CASE WHEN work_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN break_id IS NOT NULL THEN 1 ELSE 0 END) + (CASE WHEN work_change_id IS NOT NULL THEN 1 ELSE 0 END)) = 1");
                    table.ForeignKey(
                        name: "fk_surcharge_item_break_break_id",
                        column: x => x.break_id,
                        principalTable: "break",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_surcharge_item_work_change_work_change_id",
                        column: x => x.work_change_id,
                        principalTable: "work_change",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_surcharge_item_work_work_id",
                        column: x => x.work_id,
                        principalTable: "work",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_absence_is_deleted",
                table: "absence",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_absence_detail_absence_id",
                table: "absence_detail",
                column: "absence_id");

            migrationBuilder.CreateIndex(
                name: "ix_absence_detail_is_deleted_absence_id",
                table: "absence_detail",
                columns: new[] { "is_deleted", "absence_id" });

            migrationBuilder.CreateIndex(
                name: "ix_address_client_id_street_street2_street3_city_is_deleted",
                table: "address",
                columns: new[] { "client_id", "street", "street2", "street3", "city", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_autonomy_preferences_user_id",
                table: "agent_autonomy_preferences",
                column: "user_id",
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_agent_memories_agent_id",
                table: "agent_memories",
                column: "agent_id",
                filter: "is_pinned = true AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_agent_memories_agent_id_category",
                table: "agent_memories",
                columns: new[] { "agent_id", "category" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_memories_agent_id_importance",
                table: "agent_memories",
                columns: new[] { "agent_id", "importance" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_memories_agent_id_source",
                table: "agent_memories",
                columns: new[] { "agent_id", "source" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_memories_agent_id_user_id",
                table: "agent_memories",
                columns: new[] { "agent_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_memories_expires_at",
                table: "agent_memories",
                column: "expires_at",
                filter: "expires_at IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_agent_memories_supersedes_id",
                table: "agent_memories",
                column: "supersedes_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_memory_tags_tag",
                table: "agent_memory_tags",
                column: "tag");

            migrationBuilder.CreateIndex(
                name: "ix_agent_plans_agent_id_status",
                table: "agent_plans",
                columns: new[] { "agent_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_plans_session_id",
                table: "agent_plans",
                column: "session_id",
                filter: "session_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_agent_plans_status",
                table: "agent_plans",
                column: "status",
                filter: "status IN ('drafting','executing','paused_for_approval')");

            migrationBuilder.CreateIndex(
                name: "ix_agent_plans_user_id",
                table: "agent_plans",
                column: "user_id",
                filter: "user_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_agent_recipes_is_enabled_sort_order",
                table: "agent_recipes",
                columns: new[] { "is_enabled", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_recipes_name",
                table: "agent_recipes",
                column: "name",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_agent_session_messages_compacted_into_id",
                table: "agent_session_messages",
                column: "compacted_into_id");

            migrationBuilder.CreateIndex(
                name: "ix_agent_session_messages_session_id_create_time",
                table: "agent_session_messages",
                columns: new[] { "session_id", "create_time" },
                filter: "is_compacted = false AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_agent_sessions_agent_id_session_id",
                table: "agent_sessions",
                columns: new[] { "agent_id", "session_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_agent_sessions_agent_id_status",
                table: "agent_sessions",
                columns: new[] { "agent_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_sessions_user_id_update_time",
                table: "agent_sessions",
                columns: new[] { "user_id", "update_time" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_skill_executions_session_id_create_time",
                table: "agent_skill_executions",
                columns: new[] { "session_id", "create_time" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_skill_executions_skill_id_create_time",
                table: "agent_skill_executions",
                columns: new[] { "skill_id", "create_time" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_skills_agent_id_is_enabled_sort_order",
                table: "agent_skills",
                columns: new[] { "agent_id", "is_enabled", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_skills_agent_id_name",
                table: "agent_skills",
                columns: new[] { "agent_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_agent_soul_histories_agent_id_create_time",
                table: "agent_soul_histories",
                columns: new[] { "agent_id", "create_time" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_soul_histories_soul_section_id_create_time",
                table: "agent_soul_histories",
                columns: new[] { "soul_section_id", "create_time" });

            migrationBuilder.CreateIndex(
                name: "ix_agent_soul_sections_agent_id_section_type",
                table: "agent_soul_sections",
                columns: new[] { "agent_id", "section_type" },
                unique: true,
                filter: "is_active = true AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_agent_soul_sections_agent_id_sort_order",
                table: "agent_soul_sections",
                columns: new[] { "agent_id", "sort_order" },
                filter: "is_active = true AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_agent_trigger_dispatches_user_id_trigger_kind_dedup_key",
                table: "agent_trigger_dispatches",
                columns: new[] { "user_id", "trigger_kind", "dedup_key" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_agent_trigger_preferences_user_id_trigger_kind",
                table: "agent_trigger_preferences",
                columns: new[] { "user_id", "trigger_kind" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_agents_is_default",
                table: "agents",
                column: "is_default",
                unique: true,
                filter: "is_default = true AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_agents_is_deleted_is_active",
                table: "agents",
                columns: new[] { "is_deleted", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_analyse_scenarios_group_id_status",
                table: "analyse_scenarios",
                columns: new[] { "group_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_analyse_scenarios_token",
                table: "analyse_scenarios",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_annotation_client_id",
                table: "annotation",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_annotation_note_is_deleted",
                table: "annotation",
                columns: new[] { "note", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_role_claims_role_id",
                table: "AspNetRoleClaims",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "normalized_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_claims_user_id",
                table: "AspNetUserClaims",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_logins_user_id",
                table: "AspNetUserLogins",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_asp_net_user_roles_role_id",
                table: "AspNetUserRoles",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "normalized_email");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "normalized_user_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_assigned_group_client_id_group_id",
                table: "assigned_group",
                columns: new[] { "client_id", "group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_assigned_group_group_id",
                table: "assigned_group",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_absence_id",
                table: "break",
                column: "absence_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_analyse_token",
                table: "break",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_break_client_id",
                table: "break",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_current_date_client_id",
                table: "break",
                columns: new[] { "workday", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_break_parent_work_id",
                table: "break",
                column: "parent_work_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_placeholder_absence_id",
                table: "break_placeholder",
                column: "absence_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_placeholder_client_id",
                table: "break_placeholder",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_placeholder_is_deleted_absence_id_client_id",
                table: "break_placeholder",
                columns: new[] { "is_deleted", "absence_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_break_placeholder_is_deleted_client_id_from_until",
                table: "break_placeholder",
                columns: new[] { "is_deleted", "client_id", "from", "until" });

            migrationBuilder.CreateIndex(
                name: "ix_calendar_rule_state_country",
                table: "calendar_rule",
                columns: new[] { "state", "country" });

            migrationBuilder.CreateIndex(
                name: "ix_client_first_name_second_name_name_maiden_name_company_gend",
                table: "client",
                columns: new[] { "first_name", "second_name", "name", "maiden_name", "company", "gender", "type", "legal_entity", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_client_identity_provider_id",
                table: "client",
                column: "identity_provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_is_deleted_company_name",
                table: "client",
                columns: new[] { "is_deleted", "company", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_client_is_deleted_first_name_name",
                table: "client",
                columns: new[] { "is_deleted", "first_name", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_client_is_deleted_id_number",
                table: "client",
                columns: new[] { "is_deleted", "id_number" });

            migrationBuilder.CreateIndex(
                name: "ix_client_is_deleted_name_first_name",
                table: "client",
                columns: new[] { "is_deleted", "name", "first_name" });

            migrationBuilder.CreateIndex(
                name: "ix_client_source_system_id_external_customer_reference",
                table: "client",
                columns: new[] { "source_system_id", "external_customer_reference" },
                unique: true,
                filter: "external_customer_reference IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_client_availability_client_id_date_hour",
                table: "client_availability",
                columns: new[] { "client_id", "date", "hour" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_client_availability_is_deleted_client_id_date",
                table: "client_availability",
                columns: new[] { "is_deleted", "client_id", "date" });

            migrationBuilder.CreateIndex(
                name: "ix_client_contract_client_id_contract_id_from_date_until_date",
                table: "client_contract",
                columns: new[] { "client_id", "contract_id", "from_date", "until_date" });

            migrationBuilder.CreateIndex(
                name: "ix_client_contract_contract_id",
                table: "client_contract",
                column: "contract_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_image_client_id",
                table: "client_image",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_client_period_hours_client_id_start_date_end_date_analyse_t",
                table: "client_period_hours",
                columns: new[] { "client_id", "start_date", "end_date", "analyse_token" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_client_period_hours_individual_period_id",
                table: "client_period_hours",
                column: "individual_period_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_qualification_client_id_qualification_id",
                table: "client_qualification",
                columns: new[] { "client_id", "qualification_id" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_client_qualification_client_id_valid_from_valid_until_is_de",
                table: "client_qualification",
                columns: new[] { "client_id", "valid_from", "valid_until", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_client_qualification_qualification_id",
                table: "client_qualification",
                column: "qualification_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_schedule_detail_client_id_current_year_current_month",
                table: "client_schedule_detail",
                columns: new[] { "client_id", "current_year", "current_month" });

            migrationBuilder.CreateIndex(
                name: "ix_client_shift_preference_analyse_token",
                table: "client_shift_preference",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_client_shift_preference_client_id_is_deleted",
                table: "client_shift_preference",
                columns: new[] { "client_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_client_shift_preference_client_id_shift_id_preference_type",
                table: "client_shift_preference",
                columns: new[] { "client_id", "shift_id", "preference_type" });

            migrationBuilder.CreateIndex(
                name: "ix_client_shift_preference_shift_id",
                table: "client_shift_preference",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_sort_preference_user_id_group_id_client_id",
                table: "client_sort_preference",
                columns: new[] { "user_id", "group_id", "client_id" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_client_sort_preference_user_id_group_id_sort_order",
                table: "client_sort_preference",
                columns: new[] { "user_id", "group_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_communication_client_id",
                table: "communication",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_communication_value_is_deleted",
                table: "communication",
                columns: new[] { "value", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_container_lock_resource_type_resource_id",
                table: "container_lock",
                columns: new[] { "resource_type", "resource_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_container_shift_override_items_absence_id",
                table: "container_shift_override_items",
                column: "absence_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_shift_override_items_container_shift_override_id",
                table: "container_shift_override_items",
                column: "container_shift_override_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_shift_override_items_shift_id",
                table: "container_shift_override_items",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_shift_overrides_container_id_date",
                table: "container_shift_overrides",
                columns: new[] { "container_id", "date" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_container_template_container_id",
                table: "container_template",
                column: "container_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_template_id_container_id_weekday_is_weekday_and_h",
                table: "container_template",
                columns: new[] { "id", "container_id", "weekday", "is_weekday_and_holiday", "is_holiday" });

            migrationBuilder.CreateIndex(
                name: "ix_container_template_item_absence_id",
                table: "container_template_item",
                column: "absence_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_template_item_container_template_id",
                table: "container_template_item",
                column: "container_template_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_template_item_shift_id",
                table: "container_template_item",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_calendar_selection_id",
                table: "contract",
                column: "calendar_selection_id");

            migrationBuilder.CreateIndex(
                name: "ix_contract_name_valid_from_valid_until",
                table: "contract",
                columns: new[] { "name", "valid_from", "valid_until" });

            migrationBuilder.CreateIndex(
                name: "ix_contract_scheduling_rule_id",
                table: "contract",
                column: "scheduling_rule_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_analyses_is_deleted_client_id",
                table: "email_analyses",
                columns: new[] { "is_deleted", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_email_analyses_is_deleted_intent",
                table: "email_analyses",
                columns: new[] { "is_deleted", "intent" });

            migrationBuilder.CreateIndex(
                name: "ix_email_analyses_received_email_id",
                table: "email_analyses",
                column: "received_email_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_email_folders_imap_folder_name",
                table: "email_folders",
                column: "imap_folder_name",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_email_folders_is_deleted_sort_order",
                table: "email_folders",
                columns: new[] { "is_deleted", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_erp_drop_points_source_system_id",
                table: "erp_drop_points",
                column: "source_system_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_erp_import_exception_resolved_at",
                table: "erp_import_exception",
                column: "resolved_at");

            migrationBuilder.CreateIndex(
                name: "ix_erp_import_token_drop_point_id",
                table: "erp_import_token",
                column: "drop_point_id");

            migrationBuilder.CreateIndex(
                name: "ix_erp_import_token_token_hash",
                table: "erp_import_token",
                column: "token_hash",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_eval_runs_goldset_create_time",
                table: "eval_runs",
                columns: new[] { "goldset", "create_time" });

            migrationBuilder.CreateIndex(
                name: "ix_expenses_analyse_token",
                table: "expenses",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_expenses_work_id",
                table: "expenses",
                column: "work_id");

            migrationBuilder.CreateIndex(
                name: "ix_export_format_override_format_key",
                table: "export_format_override",
                column: "format_key",
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_export_log_start_date_end_date_group_id",
                table: "export_log",
                columns: new[] { "start_date", "end_date", "group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_global_agent_rule_histories_global_agent_rule_id_create_time",
                table: "global_agent_rule_histories",
                columns: new[] { "global_agent_rule_id", "create_time" });

            migrationBuilder.CreateIndex(
                name: "ix_global_agent_rules_name",
                table: "global_agent_rules",
                column: "name",
                unique: true,
                filter: "is_active = true AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_global_agent_rules_sort_order",
                table: "global_agent_rules",
                column: "sort_order",
                filter: "is_active = true AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_group_calendar_selection_id",
                table: "group",
                column: "calendar_selection_id");

            migrationBuilder.CreateIndex(
                name: "ix_group_name",
                table: "group",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_client_id_group_id",
                table: "group_item",
                columns: new[] { "client_id", "group_id" },
                unique: true,
                filter: "\"client_id\" IS NOT NULL AND \"is_deleted\" = false AND \"analyse_token\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_client_id_group_id_analyse_token",
                table: "group_item",
                columns: new[] { "client_id", "group_id", "analyse_token" },
                unique: true,
                filter: "\"client_id\" IS NOT NULL AND \"is_deleted\" = false AND \"analyse_token\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_client_id_group_id_shift_id",
                table: "group_item",
                columns: new[] { "client_id", "group_id", "shift_id" });

            migrationBuilder.CreateIndex(
                name: "ix_group_item_group_id_client_id_is_deleted",
                table: "group_item",
                columns: new[] { "group_id", "client_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_group_item_shift_id_group_id",
                table: "group_item",
                columns: new[] { "shift_id", "group_id" },
                unique: true,
                filter: "\"shift_id\" IS NOT NULL AND \"is_deleted\" = false AND \"analyse_token\" IS NULL");

            migrationBuilder.CreateIndex(
                name: "ix_group_visibility_app_user_id_group_id",
                table: "group_visibility",
                columns: new[] { "app_user_id", "group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_group_visibility_group_id",
                table: "group_visibility",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_heartbeat_configs_is_deleted_is_enabled",
                table: "heartbeat_configs",
                columns: new[] { "is_deleted", "is_enabled" });

            migrationBuilder.CreateIndex(
                name: "ix_heartbeat_configs_is_deleted_user_id",
                table: "heartbeat_configs",
                columns: new[] { "is_deleted", "user_id" });

            migrationBuilder.CreateIndex(
                name: "ix_history_client_id",
                table: "history",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_history_is_deleted",
                table: "history",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_identity_provider_sync_logs_client_id_identity_provider_id",
                table: "identity_provider_sync_logs",
                columns: new[] { "client_id", "identity_provider_id" });

            migrationBuilder.CreateIndex(
                name: "ix_identity_provider_sync_logs_identity_provider_id_external_id",
                table: "identity_provider_sync_logs",
                columns: new[] { "identity_provider_id", "external_id" });

            migrationBuilder.CreateIndex(
                name: "ix_identity_providers_is_deleted_is_enabled_sort_order",
                table: "identity_providers",
                columns: new[] { "is_deleted", "is_enabled", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_klacks_bot_token_token_hash",
                table: "klacks_bot_token",
                column: "token_hash",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_klacksy_navigation_feedback_matched_target_id",
                table: "klacksy_navigation_feedback",
                column: "matched_target_id");

            migrationBuilder.CreateIndex(
                name: "ix_klacksy_navigation_feedback_timestamp",
                table: "klacksy_navigation_feedback",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "knowledge_index_kind_source_unique",
                table: "knowledge_index",
                columns: new[] { "kind", "source_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_llm_conversations_user_id",
                table: "llm_conversations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_llm_messages_conversation_id",
                table: "llm_messages",
                column: "conversation_id");

            migrationBuilder.CreateIndex(
                name: "ix_llm_models_llm_provider_id",
                table: "llm_models",
                column: "llm_provider_id");

            migrationBuilder.CreateIndex(
                name: "ix_llm_usages_model_id",
                table: "llm_usages",
                column: "model_id");

            migrationBuilder.CreateIndex(
                name: "ix_llm_usages_user_id",
                table: "llm_usages",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_macro_is_deleted_name",
                table: "macro",
                columns: new[] { "is_deleted", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_membership_client_id",
                table: "membership",
                column: "client_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_membership_client_id_valid_from_valid_until_is_deleted",
                table: "membership",
                columns: new[] { "client_id", "valid_from", "valid_until", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_messages_broadcast_id",
                table: "messages",
                column: "broadcast_id");

            migrationBuilder.CreateIndex(
                name: "ix_messages_client_id",
                table: "messages",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_messages_direction",
                table: "messages",
                column: "direction");

            migrationBuilder.CreateIndex(
                name: "ix_messages_provider_id_timestamp",
                table: "messages",
                columns: new[] { "provider_id", "timestamp" },
                descending: new[] { false, true });

            migrationBuilder.CreateIndex(
                name: "ix_messaging_providers_name",
                table: "messaging_providers",
                column: "name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_messenger_contact_client_id",
                table: "messenger_contact",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_messenger_contact_client_id_type",
                table: "messenger_contact",
                columns: new[] { "client_id", "type" });

            migrationBuilder.CreateIndex(
                name: "ix_navigation_target_synonyms_target_id_language",
                table: "navigation_target_synonyms",
                columns: new[] { "target_id", "language" });

            migrationBuilder.CreateIndex(
                name: "ix_navigation_target_synonyms_target_id_language_keyword",
                table: "navigation_target_synonyms",
                columns: new[] { "target_id", "language", "keyword" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_oauth_clients_client_id",
                table: "oauth_clients",
                column: "client_id",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_payroll_export_group_config_group_id",
                table: "payroll_export_group_config",
                column: "group_id",
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_pending_user_notes_agent_id_user_id_is_deleted",
                table: "pending_user_notes",
                columns: new[] { "agent_id", "user_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_period_individual_period_id",
                table: "period",
                column: "individual_period_id");

            migrationBuilder.CreateIndex(
                name: "ix_period_audit_log_performed_at",
                table: "period_audit_log",
                column: "performed_at");

            migrationBuilder.CreateIndex(
                name: "ix_period_audit_log_start_date_end_date_group_id",
                table: "period_audit_log",
                columns: new[] { "start_date", "end_date", "group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_personal_access_tokens_token_hash",
                table: "personal_access_tokens",
                column: "token_hash",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_personal_access_tokens_user_id",
                table: "personal_access_tokens",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_plugin_docs_plugin_code_manual_name",
                table: "plugin_docs",
                columns: new[] { "plugin_code", "manual_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_proposed_skill_changes_skill_id_field_status",
                table: "proposed_skill_changes",
                columns: new[] { "skill_id", "field", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_proposed_skill_changes_status",
                table: "proposed_skill_changes",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_received_emails_folder_imap_uid",
                table: "received_emails",
                columns: new[] { "folder", "imap_uid" });

            migrationBuilder.CreateIndex(
                name: "ix_received_emails_is_deleted_is_read",
                table: "received_emails",
                columns: new[] { "is_deleted", "is_read" });

            migrationBuilder.CreateIndex(
                name: "ix_received_emails_is_deleted_received_date",
                table: "received_emails",
                columns: new[] { "is_deleted", "received_date" });

            migrationBuilder.CreateIndex(
                name: "ix_received_emails_message_id",
                table: "received_emails",
                column: "message_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_received_emails_source_imap_folder_imap_uid",
                table: "received_emails",
                columns: new[] { "source_imap_folder", "imap_uid" });

            migrationBuilder.CreateIndex(
                name: "ix_report_templates_is_deleted_type_name",
                table: "report_templates",
                columns: new[] { "is_deleted", "type", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_schedule_change_client_id_change_date",
                table: "schedule_change",
                columns: new[] { "client_id", "change_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_schedule_commands_analyse_token",
                table: "schedule_commands",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_commands_client_id",
                table: "schedule_commands",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_notes_analyse_token",
                table: "schedule_notes",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_schedule_notes_client_id",
                table: "schedule_notes",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_tasks_is_enabled_next_run_utc",
                table: "scheduled_tasks",
                columns: new[] { "is_enabled", "next_run_utc" });

            migrationBuilder.CreateIndex(
                name: "ix_scheduled_tasks_owner_user_id_name",
                table: "scheduled_tasks",
                columns: new[] { "owner_user_id", "name" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_scheduling_rules_is_deleted_name",
                table: "scheduling_rules",
                columns: new[] { "is_deleted", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_sealed_day_date_global",
                table: "sealed_day",
                column: "date",
                unique: true,
                filter: "\"group_id\" IS NULL AND \"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_sealed_day_date_group",
                table: "sealed_day",
                columns: new[] { "date", "group_id" },
                unique: true,
                filter: "\"group_id\" IS NOT NULL AND \"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_selected_calendar_calendar_selection_id",
                table: "selected_calendar",
                column: "calendar_selection_id");

            migrationBuilder.CreateIndex(
                name: "ix_selected_calendar_state_country_calendar_selection_id",
                table: "selected_calendar",
                columns: new[] { "state", "country", "calendar_selection_id" });

            migrationBuilder.CreateIndex(
                name: "ix_sentiment_keyword_sets_language",
                table: "sentiment_keyword_sets",
                column: "language",
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_shift_analyse_token",
                table: "shift",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_shift_client_id",
                table: "shift",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_macro_id_client_id_status_from_date_until_date",
                table: "shift",
                columns: new[] { "macro_id", "client_id", "status", "from_date", "until_date" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_scenario_source_shift_id",
                table: "shift",
                column: "scenario_source_shift_id",
                filter: "scenario_source_shift_id IS NOT NULL AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_shift_source_system_id_external_order_reference",
                table: "shift",
                columns: new[] { "source_system_id", "external_order_reference" },
                filter: "external_order_reference IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_shift_expenses_analyse_token",
                table: "shift_expenses",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_shift_expenses_shift_id",
                table: "shift_expenses",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_required_qualification_qualification_id",
                table: "shift_required_qualification",
                column: "qualification_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_required_qualification_shift_id_is_deleted",
                table: "shift_required_qualification",
                columns: new[] { "shift_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_required_qualification_shift_id_qualification_id",
                table: "shift_required_qualification",
                columns: new[] { "shift_id", "qualification_id" },
                unique: true,
                filter: "\"is_deleted\" = false");

            migrationBuilder.CreateIndex(
                name: "ix_skill_gap_records_agent_id_occurrence_count",
                table: "skill_gap_records",
                columns: new[] { "agent_id", "occurrence_count" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_gap_records_agent_id_status",
                table: "skill_gap_records",
                columns: new[] { "agent_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_relations_agent_id_skill_a_name_skill_b_name_type",
                table: "skill_relations",
                columns: new[] { "agent_id", "skill_a_name", "skill_b_name", "type" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_skill_relations_agent_id_status",
                table: "skill_relations",
                columns: new[] { "agent_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_selection_trajectories_agent_id_create_time",
                table: "skill_selection_trajectories",
                columns: new[] { "agent_id", "create_time" });

            migrationBuilder.CreateIndex(
                name: "ix_skill_selection_trajectories_plan_id",
                table: "skill_selection_trajectories",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "ix_skill_selection_trajectories_was_corrected",
                table: "skill_selection_trajectories",
                column: "was_corrected");

            migrationBuilder.CreateIndex(
                name: "ix_spam_rules_is_deleted_is_active_sort_order",
                table: "spam_rules",
                columns: new[] { "is_deleted", "is_active", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_surcharge_item_break_id",
                table: "surcharge_item",
                column: "break_id");

            migrationBuilder.CreateIndex(
                name: "ix_surcharge_item_work_change_id",
                table: "surcharge_item",
                column: "work_change_id");

            migrationBuilder.CreateIndex(
                name: "ix_surcharge_item_work_id",
                table: "surcharge_item",
                column: "work_id");

            migrationBuilder.CreateIndex(
                name: "ix_telegram_onboarding_token_client_id_used_at_is_deleted",
                table: "telegram_onboarding_token",
                columns: new[] { "client_id", "used_at", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_telegram_onboarding_token_token",
                table: "telegram_onboarding_token",
                column: "token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_transcription_dictionary_entries_language",
                table: "transcription_dictionary_entries",
                column: "language",
                filter: "\"language\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_ui_controls_page_key_control_key",
                table: "ui_controls",
                columns: new[] { "page_key", "control_key" },
                unique: true,
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_ui_controls_page_key_sort_order",
                table: "ui_controls",
                columns: new[] { "page_key", "sort_order" },
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_ui_controls_parent_control_id",
                table: "ui_controls",
                column: "parent_control_id");

            migrationBuilder.CreateIndex(
                name: "ix_update_history_requested_at",
                table: "update_history",
                column: "requested_at");

            migrationBuilder.CreateIndex(
                name: "ix_update_history_status",
                table: "update_history",
                column: "status",
                filter: "is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_wizard_training_runs_create_time",
                table: "wizard_training_runs",
                column: "create_time");

            migrationBuilder.CreateIndex(
                name: "ix_wizard_training_runs_source_create_time",
                table: "wizard_training_runs",
                columns: new[] { "source", "create_time" });

            migrationBuilder.CreateIndex(
                name: "ix_wizard_training_runs_stage2score",
                table: "wizard_training_runs",
                column: "stage2score");

            migrationBuilder.CreateIndex(
                name: "ix_work_analyse_token",
                table: "work",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_work_client_id_shift_id",
                table: "work",
                columns: new[] { "client_id", "shift_id" });

            migrationBuilder.CreateIndex(
                name: "ix_work_current_date_client_id_is_deleted",
                table: "work",
                columns: new[] { "workday", "client_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_work_parent_work_id",
                table: "work",
                column: "parent_work_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_shift_id",
                table: "work",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_change_analyse_token",
                table: "work_change",
                column: "analyse_token",
                filter: "analyse_token IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_work_change_replace_client_id",
                table: "work_change",
                column: "replace_client_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_change_work_id_is_deleted",
                table: "work_change",
                columns: new[] { "work_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_work_softening_is_deleted_analyse_token",
                table: "work_softening",
                columns: new[] { "is_deleted", "analyse_token" });

            migrationBuilder.CreateIndex(
                name: "ix_work_softening_is_deleted_client_id_current_date_analyse_to",
                table: "work_softening",
                columns: new[] { "is_deleted", "client_id", "current_date", "analyse_token" });

            // KnowledgeEntry.Embedding is [NotMapped] - pgvector column/index are managed via raw SQL
            // outside the EF model (see KnowledgeIndexRepository), matching the original AddKnowledgeIndex migration.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
            migrationBuilder.Sql("ALTER TABLE knowledge_index ADD COLUMN embedding vector(384) NOT NULL DEFAULT array_fill(0, ARRAY[384])::vector;");
            migrationBuilder.Sql("CREATE INDEX knowledge_index_permission_idx ON knowledge_index (required_permission);");
            migrationBuilder.Sql("CREATE INDEX knowledge_index_embedding_idx ON knowledge_index USING hnsw (embedding vector_cosine_ops);");

            // Fuzzy client name search (pg_trgm) - GIN indexes on constant expressions EF cannot model.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_client_search_text_trgm ON client USING gin " +
                "((lower(coalesce(name, '') || ' ' || coalesce(first_name, '') || ' ' || " +
                "coalesce(maiden_name, '') || ' ' || coalesce(company, ''))) gin_trgm_ops);");
            migrationBuilder.Sql(
                "CREATE INDEX IF NOT EXISTS ix_client_phonetic_tokens ON client USING gin " +
                "((string_to_array(phonetic_tokens, ' ')));");

            // At most one active (Pending=0 / Running=1) update operation at a time - indexes a constant
            // expression that EF cannot model, so it is created via raw SQL.
            migrationBuilder.Sql(
                "CREATE UNIQUE INDEX ix_update_history_single_active ON update_history ((true)) " +
                "WHERE status IN (0, 1) AND is_deleted = false;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "absence_detail");

            migrationBuilder.DropTable(
                name: "address");

            migrationBuilder.DropTable(
                name: "agent_autonomy_preferences");

            migrationBuilder.DropTable(
                name: "agent_memory_tags");

            migrationBuilder.DropTable(
                name: "agent_recipes");

            migrationBuilder.DropTable(
                name: "agent_session_messages");

            migrationBuilder.DropTable(
                name: "agent_skill_executions");

            migrationBuilder.DropTable(
                name: "agent_soul_histories");

            migrationBuilder.DropTable(
                name: "agent_trigger_dispatches");

            migrationBuilder.DropTable(
                name: "agent_trigger_preferences");

            migrationBuilder.DropTable(
                name: "analyse_scenarios");

            migrationBuilder.DropTable(
                name: "annotation");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "assigned_group");

            migrationBuilder.DropTable(
                name: "branch");

            migrationBuilder.DropTable(
                name: "break_placeholder");

            migrationBuilder.DropTable(
                name: "calendar_rule");

            migrationBuilder.DropTable(
                name: "client_availability");

            migrationBuilder.DropTable(
                name: "client_contract");

            migrationBuilder.DropTable(
                name: "client_image");

            migrationBuilder.DropTable(
                name: "client_period_hours");

            migrationBuilder.DropTable(
                name: "client_qualification");

            migrationBuilder.DropTable(
                name: "client_schedule_detail");

            migrationBuilder.DropTable(
                name: "client_shift_preference");

            migrationBuilder.DropTable(
                name: "client_sort_preference");

            migrationBuilder.DropTable(
                name: "communication");

            migrationBuilder.DropTable(
                name: "communication_type");

            migrationBuilder.DropTable(
                name: "container_lock");

            migrationBuilder.DropTable(
                name: "container_shift_override_items");

            migrationBuilder.DropTable(
                name: "container_template_item");

            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropTable(
                name: "custom_stt_providers");

            migrationBuilder.DropTable(
                name: "email_analyses");

            migrationBuilder.DropTable(
                name: "email_folders");

            migrationBuilder.DropTable(
                name: "erp_import_exception");

            migrationBuilder.DropTable(
                name: "erp_import_token");

            migrationBuilder.DropTable(
                name: "eval_runs");

            migrationBuilder.DropTable(
                name: "expenses");

            migrationBuilder.DropTable(
                name: "export_format_override");

            migrationBuilder.DropTable(
                name: "export_log");

            migrationBuilder.DropTable(
                name: "global_agent_rule_histories");

            migrationBuilder.DropTable(
                name: "group_item");

            migrationBuilder.DropTable(
                name: "group_visibility");

            migrationBuilder.DropTable(
                name: "heartbeat_configs");

            migrationBuilder.DropTable(
                name: "history");

            migrationBuilder.DropTable(
                name: "identity_provider_sync_logs");

            migrationBuilder.DropTable(
                name: "klacks_bot_token");

            migrationBuilder.DropTable(
                name: "klacksy_navigation_feedback");

            migrationBuilder.DropTable(
                name: "knowledge_index");

            migrationBuilder.DropTable(
                name: "llm_messages");

            migrationBuilder.DropTable(
                name: "llm_sync_notifications");

            migrationBuilder.DropTable(
                name: "llm_usages");

            migrationBuilder.DropTable(
                name: "macro");

            migrationBuilder.DropTable(
                name: "membership");

            migrationBuilder.DropTable(
                name: "messages");

            migrationBuilder.DropTable(
                name: "messenger_contact");

            migrationBuilder.DropTable(
                name: "navigation_target_synonyms");

            migrationBuilder.DropTable(
                name: "oauth_clients");

            migrationBuilder.DropTable(
                name: "payroll_export_group_config");

            migrationBuilder.DropTable(
                name: "pending_user_notes");

            migrationBuilder.DropTable(
                name: "period");

            migrationBuilder.DropTable(
                name: "period_audit_log");

            migrationBuilder.DropTable(
                name: "personal_access_tokens");

            migrationBuilder.DropTable(
                name: "plugin_docs");

            migrationBuilder.DropTable(
                name: "postcode_ch");

            migrationBuilder.DropTable(
                name: "proposed_skill_changes");

            migrationBuilder.DropTable(
                name: "qualification_country");

            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropTable(
                name: "report_templates");

            migrationBuilder.DropTable(
                name: "schedule_change");

            migrationBuilder.DropTable(
                name: "schedule_commands");

            migrationBuilder.DropTable(
                name: "schedule_notes");

            migrationBuilder.DropTable(
                name: "scheduled_tasks");

            migrationBuilder.DropTable(
                name: "sealed_day");

            migrationBuilder.DropTable(
                name: "selected_calendar");

            migrationBuilder.DropTable(
                name: "sentiment_keyword_sets");

            migrationBuilder.DropTable(
                name: "settings");

            migrationBuilder.DropTable(
                name: "shift_day_assignments");

            migrationBuilder.DropTable(
                name: "shift_expenses");

            migrationBuilder.DropTable(
                name: "shift_required_qualification");

            migrationBuilder.DropTable(
                name: "skill_gap_records");

            migrationBuilder.DropTable(
                name: "skill_relations");

            migrationBuilder.DropTable(
                name: "skill_selection_trajectories");

            migrationBuilder.DropTable(
                name: "skill_usage_records");

            migrationBuilder.DropTable(
                name: "spam_rules");

            migrationBuilder.DropTable(
                name: "state");

            migrationBuilder.DropTable(
                name: "surcharge_item");

            migrationBuilder.DropTable(
                name: "telegram_onboarding_token");

            migrationBuilder.DropTable(
                name: "transcription_dictionary_entries");

            migrationBuilder.DropTable(
                name: "ui_controls");

            migrationBuilder.DropTable(
                name: "update_history");

            migrationBuilder.DropTable(
                name: "wizard_training_runs");

            migrationBuilder.DropTable(
                name: "work_softening");

            migrationBuilder.DropTable(
                name: "agent_memories");

            migrationBuilder.DropTable(
                name: "agent_sessions");

            migrationBuilder.DropTable(
                name: "agent_skills");

            migrationBuilder.DropTable(
                name: "agent_soul_sections");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "contract");

            migrationBuilder.DropTable(
                name: "container_shift_overrides");

            migrationBuilder.DropTable(
                name: "container_template");

            migrationBuilder.DropTable(
                name: "received_emails");

            migrationBuilder.DropTable(
                name: "erp_drop_points");

            migrationBuilder.DropTable(
                name: "global_agent_rules");

            migrationBuilder.DropTable(
                name: "group");

            migrationBuilder.DropTable(
                name: "llm_conversations");

            migrationBuilder.DropTable(
                name: "llm_models");

            migrationBuilder.DropTable(
                name: "messaging_providers");

            migrationBuilder.DropTable(
                name: "individual_period");

            migrationBuilder.DropTable(
                name: "qualification");

            migrationBuilder.DropTable(
                name: "agent_plans");

            migrationBuilder.DropTable(
                name: "break");

            migrationBuilder.DropTable(
                name: "work_change");

            migrationBuilder.DropTable(
                name: "agents");

            migrationBuilder.DropTable(
                name: "scheduling_rules");

            migrationBuilder.DropTable(
                name: "calendar_selection");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "llm_providers");

            migrationBuilder.DropTable(
                name: "absence");

            migrationBuilder.DropTable(
                name: "work");

            migrationBuilder.DropTable(
                name: "shift");

            migrationBuilder.DropTable(
                name: "client");

            migrationBuilder.DropTable(
                name: "identity_providers");

            migrationBuilder.DropSequence(
                name: "client_idnumber_seq",
                schema: "public");
        }
    }
}
