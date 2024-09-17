using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Klacks_api.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "absence",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false),
                    default_length = table.Column<int>(type: "integer", nullable: false),
                    default_value = table.Column<double>(type: "double precision", nullable: false),
                    description_de = table.Column<string>(type: "text", nullable: true),
                    description_en = table.Column<string>(type: "text", nullable: true),
                    description_fr = table.Column<string>(type: "text", nullable: true),
                    description_it = table.Column<string>(type: "text", nullable: true),
                    hide_in_gantt = table.Column<bool>(type: "boolean", nullable: false),
                    name_de = table.Column<string>(type: "text", nullable: true),
                    name_en = table.Column<string>(type: "text", nullable: true),
                    name_fr = table.Column<string>(type: "text", nullable: true),
                    name_it = table.Column<string>(type: "text", nullable: true),
                    undeletable = table.Column<bool>(type: "boolean", nullable: false),
                    with_holiday = table.Column<bool>(type: "boolean", nullable: false),
                    with_saturday = table.Column<bool>(type: "boolean", nullable: false),
                    with_sunday = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_absence", x => x.id);
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
                    discriminator = table.Column<string>(type: "text", nullable: false),
                    first_name = table.Column<string>(type: "text", nullable: true),
                    last_name = table.Column<string>(type: "text", nullable: true),
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
                name: "bank_details",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    account_description = table.Column<string>(type: "text", nullable: false),
                    account_number = table.Column<string>(type: "text", nullable: false),
                    subscriber_number = table.Column<string>(type: "text", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    company = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    title = table.Column<string>(type: "character varying(15)", maxLength: 15, nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    addition_line = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    street = table.Column<string>(type: "text", nullable: false),
                    street2 = table.Column<string>(type: "text", nullable: false),
                    street3 = table.Column<string>(type: "text", nullable: false),
                    zip = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    city = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    state = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    payment_slip_address = table.Column<string>(type: "text", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_bank_details", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "break_reason",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    color = table.Column<string>(type: "text", nullable: false),
                    default_length = table.Column<int>(type: "integer", nullable: false),
                    default_value = table.Column<double>(type: "double precision", nullable: false),
                    hide_in_gantt = table.Column<bool>(type: "boolean", nullable: false),
                    undeletable = table.Column<bool>(type: "boolean", nullable: false),
                    macro = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_break_reason", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "calendar_rule",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    description_de = table.Column<string>(type: "text", nullable: true),
                    description_en = table.Column<string>(type: "text", nullable: true),
                    description_fr = table.Column<string>(type: "text", nullable: true),
                    description_it = table.Column<string>(type: "text", nullable: true),
                    is_mandatory = table.Column<bool>(type: "boolean", nullable: false),
                    is_paid = table.Column<bool>(type: "boolean", nullable: false),
                    name_de = table.Column<string>(type: "text", nullable: true),
                    name_en = table.Column<string>(type: "text", nullable: true),
                    name_fr = table.Column<string>(type: "text", nullable: true),
                    name_it = table.Column<string>(type: "text", nullable: true),
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
                    name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_calendar_selection", x => x.id);
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
                    id_number = table.Column<int>(type: "integer", nullable: false),
                    legal_entity = table.Column<bool>(type: "boolean", nullable: false),
                    maiden_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    membership_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    passwort_reset_token = table.Column<string>(type: "text", nullable: true),
                    second_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    title = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    type = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_client", x => x.id);
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
                    name_de = table.Column<string>(type: "text", nullable: true),
                    name_en = table.Column<string>(type: "text", nullable: true),
                    name_fr = table.Column<string>(type: "text", nullable: true),
                    name_it = table.Column<string>(type: "text", nullable: true),
                    prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_countries", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "group",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_group", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "macro",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false),
                    description_de = table.Column<string>(type: "text", nullable: true),
                    description_en = table.Column<string>(type: "text", nullable: true),
                    description_fr = table.Column<string>(type: "text", nullable: true),
                    description_it = table.Column<string>(type: "text", nullable: true),
                    name = table.Column<string>(type: "text", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_macro", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "macro_type",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_macro_type", x => x.id);
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
                name: "shift",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cutting_after_midnight = table.Column<bool>(type: "boolean", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    macro_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    parent_id = table.Column<Guid>(type: "uuid", nullable: true),
                    root_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<int>(type: "integer", nullable: false),
                    after_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    before_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    end_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    from_date = table.Column<DateOnly>(type: "date", nullable: false),
                    start_shift = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    until_date = table.Column<DateOnly>(type: "date", nullable: true),
                    is_friday = table.Column<bool>(type: "boolean", nullable: false),
                    is_holiday = table.Column<bool>(type: "boolean", nullable: false),
                    is_monday = table.Column<bool>(type: "boolean", nullable: false),
                    is_saturday = table.Column<bool>(type: "boolean", nullable: false),
                    is_sunday = table.Column<bool>(type: "boolean", nullable: false),
                    is_thursday = table.Column<bool>(type: "boolean", nullable: false),
                    is_tuesday = table.Column<bool>(type: "boolean", nullable: false),
                    is_wednesday = table.Column<bool>(type: "boolean", nullable: false),
                    is_weekday_or_holiday = table.Column<bool>(type: "boolean", nullable: false),
                    is_sporadic = table.Column<bool>(type: "boolean", nullable: false),
                    is_time_range = table.Column<bool>(type: "boolean", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    travel_time_after = table.Column<decimal>(type: "numeric", nullable: false),
                    travel_time_before = table.Column<decimal>(type: "numeric", nullable: false),
                    work_time = table.Column<decimal>(type: "numeric", nullable: false),
                    shift_type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shift", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "state",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    abbreviation = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    country_prefix = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    name_de = table.Column<string>(type: "text", nullable: true),
                    name_en = table.Column<string>(type: "text", nullable: true),
                    name_fr = table.Column<string>(type: "text", nullable: true),
                    name_it = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_state", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "vat",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    vat_rate = table.Column<decimal>(type: "numeric", nullable: false),
                    is_default = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_vat", x => x.id);
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
                name: "selected_calendar",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    calendar_selection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    country = table.Column<string>(type: "text", nullable: false),
                    state = table.Column<string>(type: "text", nullable: false)
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
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
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
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "break",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    absence_id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    information = table.Column<string>(type: "text", nullable: true),
                    until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    break_reason_id = table.Column<Guid>(type: "uuid", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
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
                        name: "fk_break_break_reason_break_reason_id",
                        column: x => x.break_reason_id,
                        principalTable: "break_reason",
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
                name: "communication",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    value = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    prefix = table.Column<string>(type: "text", nullable: false),
                    description = table.Column<string>(type: "text", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
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
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
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
                name: "membership",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    valid_from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    valid_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                name: "group_item",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    group_id = table.Column<Guid>(type: "uuid", nullable: false)
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
                });

            migrationBuilder.CreateTable(
                name: "work",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    client_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    information = table.Column<string>(type: "text", nullable: true),
                    is_sealed = table.Column<bool>(type: "boolean", nullable: false),
                    shift_id = table.Column<Guid>(type: "uuid", nullable: false),
                    until = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "ix_absence_is_deleted",
                table: "absence",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_address_client_id_street_street2_street3_city_is_deleted",
                table: "address",
                columns: new[] { "client_id", "street", "street2", "street3", "city", "is_deleted" });

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
                name: "ix_bank_details_is_deleted",
                table: "bank_details",
                column: "is_deleted");

            migrationBuilder.CreateIndex(
                name: "ix_break_absence_id",
                table: "break",
                column: "absence_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_break_reason_id",
                table: "break",
                column: "break_reason_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_client_id",
                table: "break",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_break_is_deleted_absence_id_client_id",
                table: "break",
                columns: new[] { "is_deleted", "absence_id", "client_id" });

            migrationBuilder.CreateIndex(
                name: "ix_calendar_rule_state_country",
                table: "calendar_rule",
                columns: new[] { "state", "country" });

            migrationBuilder.CreateIndex(
                name: "ix_client_first_name_second_name_name_maiden_name_company_gend",
                table: "client",
                columns: new[] { "first_name", "second_name", "name", "maiden_name", "company", "gender", "type", "legal_entity", "is_deleted" });

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
                name: "ix_group_name",
                table: "group",
                column: "name");

            migrationBuilder.CreateIndex(
                name: "ix_group_item_client_id_group_id",
                table: "group_item",
                columns: new[] { "client_id", "group_id" });

            migrationBuilder.CreateIndex(
                name: "ix_group_item_group_id",
                table: "group_item",
                column: "group_id");

            migrationBuilder.CreateIndex(
                name: "ix_history_client_id",
                table: "history",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "ix_history_is_deleted",
                table: "history",
                column: "is_deleted");

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
                name: "ix_selected_calendar_calendar_selection_id",
                table: "selected_calendar",
                column: "calendar_selection_id");

            migrationBuilder.CreateIndex(
                name: "ix_selected_calendar_state_country",
                table: "selected_calendar",
                columns: new[] { "state", "country" });

            migrationBuilder.CreateIndex(
                name: "ix_shift_macro_id",
                table: "shift",
                column: "macro_id");

            migrationBuilder.CreateIndex(
                name: "ix_work_client_id_shift_id",
                table: "work",
                columns: new[] { "client_id", "shift_id" });

            migrationBuilder.CreateIndex(
                name: "ix_work_shift_id",
                table: "work",
                column: "shift_id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "address");

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
                name: "bank_details");

            migrationBuilder.DropTable(
                name: "break");

            migrationBuilder.DropTable(
                name: "calendar_rule");

            migrationBuilder.DropTable(
                name: "client_schedule_detail");

            migrationBuilder.DropTable(
                name: "communication");

            migrationBuilder.DropTable(
                name: "communication_type");

            migrationBuilder.DropTable(
                name: "countries");

            migrationBuilder.DropTable(
                name: "group_item");

            migrationBuilder.DropTable(
                name: "history");

            migrationBuilder.DropTable(
                name: "macro");

            migrationBuilder.DropTable(
                name: "macro_type");

            migrationBuilder.DropTable(
                name: "membership");

            migrationBuilder.DropTable(
                name: "postcode_ch");

            migrationBuilder.DropTable(
                name: "refresh_token");

            migrationBuilder.DropTable(
                name: "selected_calendar");

            migrationBuilder.DropTable(
                name: "settings");

            migrationBuilder.DropTable(
                name: "state");

            migrationBuilder.DropTable(
                name: "vat");

            migrationBuilder.DropTable(
                name: "work");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "absence");

            migrationBuilder.DropTable(
                name: "break_reason");

            migrationBuilder.DropTable(
                name: "group");

            migrationBuilder.DropTable(
                name: "calendar_selection");

            migrationBuilder.DropTable(
                name: "client");

            migrationBuilder.DropTable(
                name: "shift");
        }
    }
}
