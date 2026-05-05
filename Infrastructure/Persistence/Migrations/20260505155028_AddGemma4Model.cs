using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddGemma4Model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                INSERT INTO llm_models (id, model_id, model_name, api_model_id, provider_id, is_enabled, is_default, cost_per_input_token, cost_per_output_token, max_tokens, context_window, category, create_time, update_time, is_deleted)
                SELECT gen_random_uuid(), 'gemma-4-27b', 'Gemma 4 27B', 'gemma-4-27b-it', 'google', true, false, 0.0, 0.0, 8192, 128000, 'balanced', NOW(), NOW(), false
                WHERE NOT EXISTS (SELECT 1 FROM llm_models WHERE model_id = 'gemma-4-27b');

                INSERT INTO llm_models (id, model_id, model_name, api_model_id, provider_id, is_enabled, is_default, cost_per_input_token, cost_per_output_token, max_tokens, context_window, category, create_time, update_time, is_deleted)
                SELECT gen_random_uuid(), 'gemma-4-9b', 'Gemma 4 9B', 'gemma-4-9b-it', 'google', true, false, 0.0, 0.0, 8192, 128000, 'fast', NOW(), NOW(), false
                WHERE NOT EXISTS (SELECT 1 FROM llm_models WHERE model_id = 'gemma-4-9b');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM llm_models WHERE model_id IN ('gemma-4-27b', 'gemma-4-9b');
            ");
        }
    }
}
