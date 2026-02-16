using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class CleanupBuiltInFunctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE llm_function_definitions SET name='create_employee', execution_type='UiPassthrough'
                  WHERE name='create_client' AND is_deleted=false;

                UPDATE llm_function_definitions SET name='search_employees', execution_type='Skill'
                  WHERE name='search_clients' AND is_deleted=false;

                UPDATE llm_function_definitions SET execution_type='Skill'
                  WHERE name='get_system_info' AND is_deleted=false;

                UPDATE llm_function_definitions SET execution_type='Skill'
                  WHERE name='create_contract' AND is_deleted=false;

                UPDATE llm_function_definitions SET name='navigate_to', execution_type='FrontendOnly'
                  WHERE name='navigate_to_page' AND is_deleted=false;
            ");

            var now = new DateTime(2026, 2, 15, 18, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] {
                    Guid.NewGuid(),
                    "validate_calendar_rule",
                    "Validates a calendar/holiday rule and calculates the resulting date. Supports fixed dates (MM/DD), Easter-relative (EASTER+XX) and SubRules (SA+2;SU+1)",
                    "[{\"name\":\"rule\",\"type\":\"string\",\"description\":\"The rule (e.g. '01/01', 'EASTER+39', '11/22+00+TH')\",\"required\":true},{\"name\":\"subRule\",\"type\":\"string\",\"description\":\"Optional SubRule for weekend adjustment (e.g. 'SA+2;SU+1')\",\"required\":false},{\"name\":\"year\",\"type\":\"integer\",\"description\":\"Year for calculation (default: current year)\",\"required\":false}]",
                    null,
                    "Skill",
                    "backend",
                    true,
                    75,
                    now,
                    "Migration",
                    false
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE llm_function_definitions SET name='create_client', execution_type='BuiltIn'
                  WHERE name='create_employee' AND is_deleted=false;

                UPDATE llm_function_definitions SET name='search_clients', execution_type='BuiltIn'
                  WHERE name='search_employees' AND is_deleted=false;

                UPDATE llm_function_definitions SET execution_type='BuiltIn'
                  WHERE name='get_system_info' AND is_deleted=false;

                UPDATE llm_function_definitions SET execution_type='BuiltIn'
                  WHERE name='create_contract' AND is_deleted=false;

                UPDATE llm_function_definitions SET name='navigate_to_page', execution_type='BuiltIn'
                  WHERE name='navigate_to' AND is_deleted=false;

                DELETE FROM llm_function_definitions WHERE name='validate_calendar_rule';
            ");
        }
    }
}
