using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddLlmProviderRequiresApiKey : Migration
    {
        private const string OllamaProviderId = "8f4c1d6a-9b2e-4e7f-a3c5-1d0b7e9f2a41";
        private const string LmStudioProviderId = "5b7e3f9c-2d4a-4c8b-9e6f-7a1c3d5b8e02";
        private const string CerebrasProviderId = "3d9a5c7e-4f1b-4a6d-8c2e-9b0f6a3d7c15";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "requires_api_key",
                table: "llm_providers",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.Sql($@"
                INSERT INTO llm_providers (id, provider_id, provider_name, is_enabled, priority, base_url, api_version, requires_api_key, settings, create_time, update_time, is_deleted)
                SELECT '{OllamaProviderId}', 'ollama', 'Ollama (local)', false, 16, 'http://localhost:11434/v1/', 'v1', false, NULL, NOW(), NOW(), false
                WHERE NOT EXISTS (SELECT 1 FROM llm_providers WHERE provider_id = 'ollama');
            ");

            migrationBuilder.Sql($@"
                INSERT INTO llm_providers (id, provider_id, provider_name, is_enabled, priority, base_url, api_version, requires_api_key, settings, create_time, update_time, is_deleted)
                SELECT '{LmStudioProviderId}', 'lm-studio', 'LM Studio (local)', false, 17, 'http://localhost:1234/v1/', 'v1', false, NULL, NOW(), NOW(), false
                WHERE NOT EXISTS (SELECT 1 FROM llm_providers WHERE provider_id = 'lm-studio');
            ");

            migrationBuilder.Sql($@"
                INSERT INTO llm_providers (id, provider_id, provider_name, is_enabled, priority, base_url, api_version, requires_api_key, settings, create_time, update_time, is_deleted)
                SELECT '{CerebrasProviderId}', 'cerebras', 'Cerebras', false, 18, 'https://api.cerebras.ai/v1/', 'v1', true, NULL, NOW(), NOW(), false
                WHERE NOT EXISTS (SELECT 1 FROM llm_providers WHERE provider_id = 'cerebras');
            ");

            migrationBuilder.Sql(@"
                UPDATE llm_providers SET requires_api_key = false WHERE provider_id IN ('ollama', 'lm-studio');
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                DELETE FROM llm_providers WHERE id IN ('{OllamaProviderId}', '{LmStudioProviderId}', '{CerebrasProviderId}');
            ");

            migrationBuilder.DropColumn(
                name: "requires_api_key",
                table: "llm_providers");
        }
    }
}
