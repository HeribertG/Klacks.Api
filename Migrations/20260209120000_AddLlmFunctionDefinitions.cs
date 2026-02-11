using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLlmFunctionDefinitions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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

            migrationBuilder.CreateIndex(
                name: "ix_llm_function_definitions_is_deleted_is_enabled_sort_order",
                table: "llm_function_definitions",
                columns: new[] { "is_deleted", "is_enabled", "sort_order" });

            var now = new DateTime(2026, 2, 9, 12, 0, 0, DateTimeKind.Utc);

            // 1. get_system_info
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "get_system_info", "Returns general information about the Klacks HR system", "[]", null, "BuiltIn", "backend", true, 10, now, "Migration", false });

            // 2. navigate_to_page
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "navigate_to_page", "Navigates to different pages in the application",
                    "[{\"name\":\"page\",\"type\":\"string\",\"description\":\"Target page for navigation\",\"required\":true,\"enum\":[\"dashboard\",\"clients\",\"contracts\",\"settings\",\"calendar\",\"reports\"]}]",
                    null, "BuiltIn", "backend", true, 20, now, "Migration", false });

            // 3. search_clients
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "search_clients", "Searches for employees or clients based on various criteria",
                    "[{\"name\":\"searchTerm\",\"type\":\"string\",\"description\":\"Search term for name or company\",\"required\":false},{\"name\":\"canton\",\"type\":\"string\",\"description\":\"Filter by Swiss canton\",\"required\":false,\"enum\":[\"BE\",\"ZH\",\"SG\",\"VD\",\"AG\",\"LU\",\"BS\"]},{\"name\":\"membershipType\",\"type\":\"string\",\"description\":\"Type of membership\",\"required\":false,\"enum\":[\"Employee\",\"Customer\",\"ExternEmp\"]}]",
                    "CanViewClients", "BuiltIn", "backend", true, 30, now, "Migration", false });

            // 4. searchAndNavigate
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "searchAndNavigate", "Searches for an entity (employee, client, group, shift) by name and navigates directly to it. Use this function when the user wants to open/edit a person or entity (e.g. 'Open client Max Mueller', 'Show me Heribert Gasparoli'). If multiple matches are found, all are displayed.",
                    "[{\"name\":\"entityType\",\"type\":\"string\",\"description\":\"Type of entity to search: client (employees/clients), shift (shifts), group (groups)\",\"required\":true,\"enum\":[\"client\",\"shift\",\"group\"]},{\"name\":\"searchQuery\",\"type\":\"string\",\"description\":\"Name or search term to find the entity\",\"required\":true},{\"name\":\"action\",\"type\":\"string\",\"description\":\"Action after finding: view (display) or edit (modify). Default is edit.\",\"required\":false,\"enum\":[\"view\",\"edit\"]}]",
                    "CanViewClients", "Skill", "navigation", true, 40, now, "Migration", false });

            // 5. create_client
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "create_client", "Creates a new employee or client in the system with all data (name, address, birthdate, contract, group). Important: For Swiss addresses, automatically detect the canton from the postal code (e.g. 3097 Liebefeld = canton BE/Bern). Always set the country correctly.",
                    "[{\"name\":\"firstName\",\"type\":\"string\",\"description\":\"First name of the employee\",\"required\":true},{\"name\":\"lastName\",\"type\":\"string\",\"description\":\"Last name of the employee\",\"required\":true},{\"name\":\"gender\",\"type\":\"string\",\"description\":\"Gender - use LegalEntity for companies\",\"required\":true,\"enum\":[\"Male\",\"Female\",\"Intersexuality\",\"LegalEntity\"]},{\"name\":\"birthdate\",\"type\":\"string\",\"description\":\"Date of birth in format YYYY-MM-DD (e.g. 1959-10-25)\",\"required\":false},{\"name\":\"street\",\"type\":\"string\",\"description\":\"Street and house number\",\"required\":false},{\"name\":\"postalCode\",\"type\":\"string\",\"description\":\"Postal code (e.g. 3097)\",\"required\":false},{\"name\":\"city\",\"type\":\"string\",\"description\":\"City/town (e.g. Liebefeld)\",\"required\":false},{\"name\":\"canton\",\"type\":\"string\",\"description\":\"Swiss canton - detect from postal code: 1xxx=VD/GE, 2xxx=NE/JU, 3xxx=BE, 4xxx=BS/BL/SO, 5xxx=AG, 6xxx=LU/ZG/SZ/NW/OW/UR, 7xxx=GR, 8xxx=ZH/TG/SH, 9xxx=SG/AR/AI\",\"required\":false},{\"name\":\"country\",\"type\":\"string\",\"description\":\"Country - ALWAYS set (e.g. Schweiz, Deutschland, Oesterreich)\",\"required\":false},{\"name\":\"contractType\",\"type\":\"string\",\"description\":\"Contract type if desired (e.g. 'BE 180 Std', 'ZH 160 Std')\",\"required\":false},{\"name\":\"groupPath\",\"type\":\"string\",\"description\":\"Group path if desired (e.g. 'Deutschweiz Mitte -> BERN -> Bern')\",\"required\":false}]",
                    "CanCreateClients", "BuiltIn", "backend", true, 50, now, "Migration", false });

            // 6. create_contract
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "create_contract", "Creates a new employment contract for an employee",
                    "[{\"name\":\"clientId\",\"type\":\"string\",\"description\":\"ID of the employee\",\"required\":true},{\"name\":\"contractType\",\"type\":\"string\",\"description\":\"Type of contract\",\"required\":true,\"enum\":[\"Vollzeit 160\",\"Vollzeit 180\",\"Teilzeit 0 Std\"]},{\"name\":\"canton\",\"type\":\"string\",\"description\":\"Canton for the contract\",\"required\":true,\"enum\":[\"BE\",\"ZH\",\"SG\",\"VD\",\"AG\",\"LU\",\"BS\"]}]",
                    "CanCreateContracts", "BuiltIn", "backend", true, 60, now, "Migration", false });

            // 7. validate_address
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "validate_address", "Validates an address via Internet (Geocoding) and determines the canton from the postal code. Always use this function BEFORE setting an address.",
                    "[{\"name\":\"street\",\"type\":\"string\",\"description\":\"Street and house number\",\"required\":false},{\"name\":\"postalCode\",\"type\":\"string\",\"description\":\"Postal code\",\"required\":true},{\"name\":\"city\",\"type\":\"string\",\"description\":\"City/town\",\"required\":true},{\"name\":\"country\",\"type\":\"string\",\"description\":\"Country (default: Schweiz)\",\"required\":false}]",
                    null, "Skill", "backend", true, 70, now, "Migration", false });

            // 8. get_general_settings
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "get_general_settings", "Navigates to settings and reads the current app name.",
                    "[]",
                    "CanEditSettings", "FrontendOnly", "ui", true, 80, now, "Migration", false });

            // 9. update_general_settings
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "update_general_settings", "Navigates to settings and changes the app name directly in the form.",
                    "[{\"name\":\"appName\",\"type\":\"string\",\"description\":\"New app name\",\"required\":true}]",
                    "CanEditSettings", "FrontendOnly", "ui", true, 90, now, "Migration", false });

            // 10. get_owner_address
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "get_owner_address", "Reads the current company/owner address from settings. Returns all fields: Name, Supplement, Street, PostalCode, City, Canton/State, Country, Phone, Email.",
                    "[]",
                    "CanEditSettings", "FrontendOnly", "ui", true, 100, now, "Migration", false });

            // 11. update_owner_address
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "update_owner_address", "Updates the company/owner address in settings. State and Country are required fields. Use validate_address first to check the address and determine the canton.",
                    "[{\"name\":\"addressName\",\"type\":\"string\",\"description\":\"Company or owner name\",\"required\":false},{\"name\":\"supplementAddress\",\"type\":\"string\",\"description\":\"Address supplement\",\"required\":false},{\"name\":\"street\",\"type\":\"string\",\"description\":\"Street and house number\",\"required\":false},{\"name\":\"zip\",\"type\":\"string\",\"description\":\"Postal code\",\"required\":false},{\"name\":\"city\",\"type\":\"string\",\"description\":\"City/town\",\"required\":false},{\"name\":\"state\",\"type\":\"string\",\"description\":\"Canton/State abbreviation (e.g. BE, ZH)\",\"required\":true},{\"name\":\"country\",\"type\":\"string\",\"description\":\"Country abbreviation (e.g. CH, DE, AT)\",\"required\":true},{\"name\":\"phone\",\"type\":\"string\",\"description\":\"Phone number\",\"required\":false},{\"name\":\"email\",\"type\":\"string\",\"description\":\"Email address\",\"required\":false}]",
                    "CanEditSettings", "FrontendOnly", "ui", true, 110, now, "Migration", false });

            // 12. create_system_user
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "create_system_user", "Creates a new system user (login account) through the Settings UI. Opens Settings → User Administration, fills the form and saves. Returns username and password.",
                    "[{\"name\":\"firstName\",\"type\":\"string\",\"description\":\"First name of the user\",\"required\":true},{\"name\":\"lastName\",\"type\":\"string\",\"description\":\"Last name of the user\",\"required\":true},{\"name\":\"email\",\"type\":\"string\",\"description\":\"Email address of the user\",\"required\":true}]",
                    "CanEditSettings", "UiPassthrough", "ui", true, 120, now, "Migration", false });

            // 13. delete_system_user
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "delete_system_user", "Deletes a system user through the Settings UI. Opens Settings → User Administration, clicks the delete button and confirms.",
                    "[{\"name\":\"userId\",\"type\":\"string\",\"description\":\"ID of the user to delete\",\"required\":true}]",
                    "CanEditSettings", "UiPassthrough", "ui", true, 130, now, "Migration", false });

            // 14. list_system_users
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "list_system_users", "Lists all system users from the Settings UI. Opens Settings → User Administration and reads the user list.",
                    "[]",
                    "CanEditSettings", "UiPassthrough", "ui", true, 140, now, "Migration", false });

            // 15. create_branch
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "create_branch", "Creates a new branch through the Settings UI. Opens Settings → Branches, opens the modal, fills it and saves.",
                    "[{\"name\":\"name\",\"type\":\"string\",\"description\":\"Name of the branch\",\"required\":true},{\"name\":\"address\",\"type\":\"string\",\"description\":\"Address of the branch\",\"required\":true},{\"name\":\"phone\",\"type\":\"string\",\"description\":\"Phone number\",\"required\":false},{\"name\":\"email\",\"type\":\"string\",\"description\":\"Email address\",\"required\":false}]",
                    "CanEditSettings", "UiPassthrough", "ui", true, 150, now, "Migration", false });

            // 16. delete_branch
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "delete_branch", "Deletes a branch through the Settings UI. Clicks the delete button and confirms.",
                    "[{\"name\":\"branchId\",\"type\":\"string\",\"description\":\"ID of the branch to delete\",\"required\":true}]",
                    "CanEditSettings", "UiPassthrough", "ui", true, 160, now, "Migration", false });

            // 17. list_branches
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "list_branches", "Lists all branches from the Settings UI. Opens Settings and reads the branch list.",
                    "[]",
                    "CanEditSettings", "UiPassthrough", "ui", true, 170, now, "Migration", false });

            // 18. create_macro
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "create_macro",
                    "Creates a new macro with KlacksScript code through the Settings UI. Macros are VB.NET-like scripts that calculate shift surcharges, work rules, and break rules. Available variables (via import): hour (work hours), fromhour/untilhour (start/end as decimal hours), weekday (1=Mon..7=Sun), holiday/holidaynextday (boolean), nightrate, holidayrate, sarate (Samstag/Saturday surcharge), sorate (Sonntag/Sunday surcharge), guaranteedhours, fulltime. Available functions: TimeToHours('08:30')→8.5, TimeOverlap(s1,e1,s2,e2) for time range overlap, IIF(cond,true,false), Abs, Round, Len, Left, Right, Mid, InStr, Replace, Trim. Control: IF-THEN-ELSE-ENDIF, SELECT CASE, FOR-NEXT, DO-WHILE/UNTIL-LOOP, FUNCTION-ENDFUNCTION, SUB-ENDSUB. Output: OUTPUT type, value (type 1=DefaultResult, 5=Info, 8000=Filter). Debug: DEBUGPRINT value. IMPORTANT: DIM cannot initialize with a value (like old VB/VBA). Use: DIM varname on one line, then varname = value on the next. IMPORTANT: Ask the user what the macro should calculate before writing code. Example: dim rate / rate = 0 / if weekday >= 6 then rate = sorate / endif / OUTPUT 1, rate",
                    "[{\"name\":\"name\",\"type\":\"string\",\"description\":\"Name of the macro\",\"required\":true},{\"name\":\"type\",\"type\":\"string\",\"description\":\"Type: ShiftAndEmployments (shift surcharges) or WorkRules (work rule checks). Default: ShiftAndEmployments\",\"required\":false,\"enum\":[\"ShiftAndEmployments\",\"WorkRules\"]},{\"name\":\"content\",\"type\":\"string\",\"description\":\"KlacksScript code for the macro. Use \\\\n for newlines.\",\"required\":false}]",
                    "CanEditSettings", "UiPassthrough", "ui", true, 180, now, "Migration", false });

            // 19. delete_macro
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "delete_macro", "Deletes a macro through the Settings UI. Clicks the delete button and confirms.",
                    "[{\"name\":\"macroId\",\"type\":\"string\",\"description\":\"ID of the macro to delete\",\"required\":true}]",
                    "CanEditSettings", "UiPassthrough", "ui", true, 190, now, "Migration", false });

            // 20. list_macros
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "list_macros", "Lists all macros from the Settings UI. Opens Settings and reads the macro list.",
                    "[]",
                    "CanEditSettings", "UiPassthrough", "ui", true, 200, now, "Migration", false });

            // 21. set_user_group_scope
            migrationBuilder.InsertData(
                table: "llm_function_definitions",
                columns: new[] { "id", "name", "description", "parameters_json", "required_permission", "execution_type", "category", "is_enabled", "sort_order", "create_time", "current_user_created", "is_deleted" },
                values: new object[] { Guid.NewGuid(), "set_user_group_scope", "Sets the Group Scope (group area visibility) for a user through the Settings UI. Navigates to Settings → Group Scope, opens the modal for the user and activates the desired group checkboxes.",
                    "[{\"name\":\"userId\",\"type\":\"string\",\"description\":\"The ID of the user\",\"required\":true},{\"name\":\"groupNames\",\"type\":\"string\",\"description\":\"Comma-separated list of group names (e.g. 'Deutschweiz Mitte, Romandie')\",\"required\":true}]",
                    "CanEditSettings", "UiPassthrough", "ui", true, 210, now, "Migration", false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "llm_function_definitions");
        }
    }
}
