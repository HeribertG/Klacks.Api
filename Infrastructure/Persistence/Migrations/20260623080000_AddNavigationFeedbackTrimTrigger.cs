using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddNavigationFeedbackTrimTrigger : Migration
    {
        private const string FunctionName = "trim_navigation_feedback";
        private const string TriggerName = "trg_trim_navigation_feedback";
        private const string TableName = "klacksy_navigation_feedback";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                CREATE OR REPLACE FUNCTION {FunctionName}()
                RETURNS trigger AS $$
                BEGIN
                    DELETE FROM {TableName}
                    WHERE id IN (
                        SELECT id
                        FROM {TableName}
                        ORDER BY timestamp DESC
                        OFFSET 1000
                    );
                    RETURN NULL;
                END;
                $$ LANGUAGE plpgsql;
                """);

            migrationBuilder.Sql($"""
                CREATE TRIGGER {TriggerName}
                AFTER INSERT ON {TableName}
                FOR EACH STATEMENT
                EXECUTE FUNCTION {FunctionName}();
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"DROP TRIGGER IF EXISTS {TriggerName} ON {TableName};");
            migrationBuilder.Sql($"DROP FUNCTION IF EXISTS {FunctionName}();");
        }
    }
}
