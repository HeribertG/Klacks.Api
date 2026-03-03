using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
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
                    color = table.Column<string>(type: "text", nullable: false),
                    default_length = table.Column<int>(type: "integer", nullable: false),
                    default_value = table.Column<double>(type: "double precision", nullable: false),
                    hide_in_gantt = table.Column<bool>(type: "boolean", nullable: false),
                    undeletable = table.Column<bool>(type: "boolean", nullable: false),
                    with_holiday = table.Column<bool>(type: "boolean", nullable: false),
                    with_saturday = table.Column<bool>(type: "boolean", nullable: false),
                    with_sunday = table.Column<bool>(type: "boolean", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    abbreviation = table.Column<string>(type: "jsonb", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_absence", x => x.id);
                });

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
                name: "ai_guidelines",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_ai_guidelines", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_memories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    key = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    importance = table.Column<int>(type: "integer", nullable: false),
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
                    table.PrimaryKey("pk_ai_memories", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "ai_souls",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_ai_souls", x => x.id);
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
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    rule = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false),
                    sub_rule = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "jsonb", nullable: false),
                    name = table.Column<string>(type: "jsonb", nullable: false)
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
                name: "countries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    abbreviation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.id);
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
                    bind_password = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                name: "llm_function_definitions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    parameters_json = table.Column<string>(type: "text", nullable: false),
                    required_permission = table.Column<string>(type: "text", nullable: true),
                    execution_type = table.Column<string>(type: "text", nullable: false),
                    category = table.Column<string>(type: "text", nullable: false),
                    is_enabled = table.Column<bool>(type: "boolean", nullable: false),
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
                    table.PrimaryKey("pk_llm_function_definitions", x => x.id);
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
                name: "macro",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_macro", x => x.id);
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
                    engaged = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
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
                name: "state",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    abbreviation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    country_prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    name = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_state", x => x.id);
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
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true),
                    detail_name = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_absence_detail", x => x.id);
                    table.ForeignKey(
                        name: "fk_absence_detail_absence_absence_id",
                        column: x => x.absence_id,
                        principalTable: "absence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "group",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_interval = table.Column<int>(type: "integer", nullable: false),
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
                    second_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    identity_provider_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ldap_external_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    sa_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    so_rate = table.Column<decimal>(type: "numeric", nullable: true),
                    payment_interval = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    calendar_selection_id = table.Column<Guid>(type: "uuid", nullable: true),
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
                name: "break",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    absence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    information = table.Column<string>(type: "text", nullable: true),
                    work_time = table.Column<decimal>(type: "numeric", nullable: false),
                    surcharges = table.Column<decimal>(type: "numeric", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    lock_level = table.Column<int>(type: "integer", nullable: false),
                    sealed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sealed_by = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_break", x => x.id);
                    table.ForeignKey(
                        name: "fk_break_absence_absence_id",
                        column: x => x.absence_id,
                        principalTable: "absence",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_break_client_client_id",
                        column: x => x.client_id,
                        principalTable: "client",
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
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_break_placeholder_client_client_id",
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
                        onDelete: ReferentialAction.SetNull);
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
                name: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    create_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    current_user_created = table.Column<string>(type: "text", nullable: true),
                    current_user_deleted = table.Column<string>(type: "text", nullable: true),
                    current_user_updated = table.Column<string>(type: "text", nullable: true),
                    deleted_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false),
                    update_time = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    current_date = table.Column<DateOnly>(type: "date", nullable: false),
                    information = table.Column<string>(type: "text", nullable: true),
                    work_time = table.Column<decimal>(type: "numeric", nullable: false),
                    surcharges = table.Column<decimal>(type: "numeric", nullable: false),
                    start_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    lock_level = table.Column<int>(type: "integer", nullable: false),
                    sealed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sealed_by = table.Column<string>(type: "text", nullable: true)
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
                });

            migrationBuilder.CreateTable(
                name: "container_template_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    container_template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    start_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    end_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    briefing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    debriefing_time = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    travel_time_after = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    travel_time_before = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    time_range_start_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
                    time_range_end_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: true),
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
                        onDelete: ReferentialAction.Cascade);
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
                name: "ix_agent_links_source_agent_id_target_agent_id_link_type",
                table: "agent_links",
                columns: new[] { "source_agent_id", "target_agent_id", "link_type" },
                unique: true,
                filter: "is_active = true AND is_deleted = false");

            migrationBuilder.CreateIndex(
                name: "ix_agent_links_target_agent_id",
                table: "agent_links",
                column: "target_agent_id");

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
                name: "ix_agents_template_id",
                table: "agents",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_ai_guidelines_is_deleted_is_active",
                table: "ai_guidelines",
                columns: new[] { "is_deleted", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_memories_is_deleted_category_importance",
                table: "ai_memories",
                columns: new[] { "is_deleted", "category", "importance" });

            migrationBuilder.CreateIndex(
                name: "ix_ai_souls_is_deleted_is_active",
                table: "ai_souls",
                columns: new[] { "is_deleted", "is_active" });

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
                name: "ix_break_client_id",
                table: "break",
                column: "client_id");

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
                name: "ix_client_period_hours_client_id_start_date_end_date",
                table: "client_period_hours",
                columns: new[] { "client_id", "start_date", "end_date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_client_period_hours_individual_period_id",
                table: "client_period_hours",
                column: "individual_period_id");

            migrationBuilder.CreateIndex(
                name: "ix_client_schedule_detail_client_id_current_year_current_month",
                table: "client_schedule_detail",
                columns: new[] { "client_id", "current_year", "current_month" });

            migrationBuilder.CreateIndex(
                name: "ix_communication_client_id",
                table: "communication",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_communication_value_is_deleted",
                table: "communication",
                columns: new[] { "value", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_container_template_container_id",
                table: "container_template",
                column: "container_id");

            migrationBuilder.CreateIndex(
                name: "ix_container_template_id_container_id_weekday_is_weekday_and_h",
                table: "container_template",
                columns: new[] { "id", "container_id", "weekday", "is_weekday_and_holiday", "is_holiday" });

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
                name: "ix_expenses_work_id",
                table: "expenses",
                column: "work_id");

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
                name: "ix_group_item_client_id_group_id_shift_id",
                table: "group_item",
                columns: new[] { "client_id", "group_id", "shift_id" });

            migrationBuilder.CreateIndex(
                name: "ix_group_item_group_id_client_id_is_deleted",
                table: "group_item",
                columns: new[] { "group_id", "client_id", "is_deleted" });

            migrationBuilder.CreateIndex(
                name: "ix_group_item_shift_id",
                table: "group_item",
                column: "shift_id");

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
                name: "ix_llm_conversations_user_id",
                table: "llm_conversations",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_llm_function_definitions_is_deleted_is_enabled_sort_order",
                table: "llm_function_definitions",
                columns: new[] { "is_deleted", "is_enabled", "sort_order" });

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
                name: "ix_period_individual_period_id",
                table: "period",
                column: "individual_period_id");

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
                name: "ix_scheduling_rules_is_deleted_name",
                table: "scheduling_rules",
                columns: new[] { "is_deleted", "name" });

            migrationBuilder.CreateIndex(
                name: "ix_selected_calendar_calendar_selection_id",
                table: "selected_calendar",
                column: "calendar_selection_id");

            migrationBuilder.CreateIndex(
                name: "ix_selected_calendar_state_country_calendar_selection_id",
                table: "selected_calendar",
                columns: new[] { "state", "country", "calendar_selection_id" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_client_id",
                table: "shift",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_shift_macro_id_client_id_status_from_date_until_date",
                table: "shift",
                columns: new[] { "macro_id", "client_id", "status", "from_date", "until_date" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_expenses_shift_id",
                table: "shift_expenses",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_client_id_shift_id",
                table: "work",
                columns: new[] { "client_id", "shift_id" });

            migrationBuilder.CreateIndex(
                name: "ix_work_shift_id",
                table: "work",
                column: "shift_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_change_replace_client_id",
                table: "work_change",
                column: "replace_client_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_change_work_id",
                table: "work_change",
                column: "work_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "absence_detail");

            migrationBuilder.DropTable(
                name: "address");

            migrationBuilder.DropTable(
                name: "agent_links");

            migrationBuilder.DropTable(
                name: "agent_memory_tags");

            migrationBuilder.DropTable(
                name: "agent_session_messages");

            migrationBuilder.DropTable(
                name: "agent_skill_executions");

            migrationBuilder.DropTable(
                name: "agent_soul_histories");

            migrationBuilder.DropTable(
                name: "ai_guidelines");

            migrationBuilder.DropTable(
                name: "ai_memories");

            migrationBuilder.DropTable(
                name: "ai_souls");

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
                name: "break");

            migrationBuilder.DropTable(
                name: "break_placeholder");

            migrationBuilder.DropTable(
                name: "calendar_rule");

            migrationBuilder.DropTable(
                name: "client_contract");

            migrationBuilder.DropTable(
                name: "client_image");

            migrationBuilder.DropTable(
                name: "client_period_hours");

            migrationBuilder.DropTable(
                name: "client_schedule_detail");

            migrationBuilder.DropTable(
                name: "communication");

            migrationBuilder.DropTable(
                name: "communication_type");

            migrationBuilder.DropTable(
                name: "container_template_item");

            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropTable(
                name: "expenses");

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
                name: "llm_function_definitions");

            migrationBuilder.DropTable(
                name: "llm_messages");

            migrationBuilder.DropTable(
                name: "llm_usages");

            migrationBuilder.DropTable(
                name: "macro");

            migrationBuilder.DropTable(
                name: "membership");

            migrationBuilder.DropTable(
                name: "period");

            migrationBuilder.DropTable(
                name: "postcode_ch");

            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropTable(
                name: "report_templates");

            migrationBuilder.DropTable(
                name: "schedule_change");

            migrationBuilder.DropTable(
                name: "selected_calendar");

            migrationBuilder.DropTable(
                name: "settings");

            migrationBuilder.DropTable(
                name: "shift_day_assignments");

            migrationBuilder.DropTable(
                name: "shift_expenses");

            migrationBuilder.DropTable(
                name: "skill_usage_records");

            migrationBuilder.DropTable(
                name: "state");

            migrationBuilder.DropTable(
                name: "work_change");

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
                name: "absence");

            migrationBuilder.DropTable(
                name: "contract");

            migrationBuilder.DropTable(
                name: "container_template");

            migrationBuilder.DropTable(
                name: "global_agent_rules");

            migrationBuilder.DropTable(
                name: "group");

            migrationBuilder.DropTable(
                name: "llm_conversations");

            migrationBuilder.DropTable(
                name: "llm_models");

            migrationBuilder.DropTable(
                name: "individual_period");

            migrationBuilder.DropTable(
                name: "work");

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
                name: "shift");

            migrationBuilder.DropTable(
                name: "agent_templates");

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
