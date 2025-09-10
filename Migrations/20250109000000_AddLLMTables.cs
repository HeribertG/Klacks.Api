using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Migrations;

/// <inheritdoc />
public partial class AddLLMTables : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create LLMProviders table
        migrationBuilder.CreateTable(
            name: "llm_providers",
            columns: table => new
            {
                id = table.Column<Guid>(nullable: false),
                provider_id = table.Column<string>(maxLength: 50, nullable: false),
                provider_name = table.Column<string>(maxLength: 100, nullable: false),
                api_key = table.Column<string>(maxLength: 500, nullable: true),
                is_enabled = table.Column<bool>(nullable: false),
                base_url = table.Column<string>(maxLength: 200, nullable: true),
                api_version = table.Column<string>(maxLength: 50, nullable: true),
                priority = table.Column<int>(nullable: false),
                create_time = table.Column<DateTime>(nullable: true),
                current_user_created = table.Column<string>(nullable: true),
                current_user_deleted = table.Column<string>(nullable: true),
                current_user_updated = table.Column<string>(nullable: true),
                deleted_time = table.Column<DateTime>(nullable: true),
                is_deleted = table.Column<bool>(nullable: false),
                update_time = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_llm_providers", x => x.id);
                table.UniqueConstraint("ak_llm_providers_provider_id", x => x.provider_id);
            });

        // Create LLMModels table
        migrationBuilder.CreateTable(
            name: "llm_models",
            columns: table => new
            {
                id = table.Column<Guid>(nullable: false),
                model_id = table.Column<string>(maxLength: 50, nullable: false),
                model_name = table.Column<string>(maxLength: 100, nullable: false),
                api_model_id = table.Column<string>(maxLength: 50, nullable: false),
                provider_id = table.Column<Guid>(nullable: false),
                is_enabled = table.Column<bool>(nullable: false),
                is_default = table.Column<bool>(nullable: false),
                cost_per_input_token = table.Column<decimal>(type: "decimal(10, 6)", nullable: false),
                cost_per_output_token = table.Column<decimal>(type: "decimal(10, 6)", nullable: false),
                max_tokens = table.Column<int>(nullable: false),
                context_window = table.Column<int>(nullable: false),
                description = table.Column<string>(maxLength: 500, nullable: true),
                category = table.Column<string>(maxLength: 50, nullable: true),
                released_at = table.Column<DateTime>(nullable: true),
                deprecated_at = table.Column<DateTime>(nullable: true),
                create_time = table.Column<DateTime>(nullable: true),
                current_user_created = table.Column<string>(nullable: true),
                current_user_deleted = table.Column<string>(nullable: true),
                current_user_updated = table.Column<string>(nullable: true),
                deleted_time = table.Column<DateTime>(nullable: true),
                is_deleted = table.Column<bool>(nullable: false),
                update_time = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_llm_models", x => x.id);
                table.UniqueConstraint("ak_llm_models_model_id", x => x.model_id);
                table.ForeignKey(
                    name: "fk_llm_models_llm_providers_provider_id",
                    column: x => x.provider_id,
                    principalTable: "llm_providers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create LLMConversations table
        migrationBuilder.CreateTable(
            name: "llm_conversations",
            columns: table => new
            {
                id = table.Column<Guid>(nullable: false),
                conversation_id = table.Column<string>(maxLength: 100, nullable: false),
                user_id = table.Column<string>(nullable: false),
                title = table.Column<string>(maxLength: 200, nullable: true),
                summary = table.Column<string>(maxLength: 500, nullable: true),
                last_message_at = table.Column<DateTime>(nullable: false),
                message_count = table.Column<int>(nullable: false),
                total_tokens = table.Column<int>(nullable: false),
                total_cost = table.Column<decimal>(type: "decimal(10, 4)", nullable: false),
                last_model_id = table.Column<string>(maxLength: 50, nullable: true),
                is_archived = table.Column<bool>(nullable: false),
                create_time = table.Column<DateTime>(nullable: true),
                current_user_created = table.Column<string>(nullable: true),
                current_user_deleted = table.Column<string>(nullable: true),
                current_user_updated = table.Column<string>(nullable: true),
                deleted_time = table.Column<DateTime>(nullable: true),
                is_deleted = table.Column<bool>(nullable: false),
                update_time = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_llm_conversations", x => x.id);
                table.UniqueConstraint("ak_llm_conversations_conversation_id", x => x.conversation_id);
                table.ForeignKey(
                    name: "fk_llm_conversations_asp_net_users_user_id",
                    column: x => x.user_id,
                    principalTable: "AspNetUsers",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Create LLMMessages table
        migrationBuilder.CreateTable(
            name: "llm_messages",
            columns: table => new
            {
                id = table.Column<Guid>(nullable: false),
                conversation_id = table.Column<Guid>(nullable: false),
                role = table.Column<string>(maxLength: 20, nullable: false),
                content = table.Column<string>(nullable: false),
                token_count = table.Column<int>(nullable: true),
                model_id = table.Column<string>(maxLength: 50, nullable: true),
                function_calls = table.Column<string>(maxLength: 500, nullable: true),
                create_time = table.Column<DateTime>(nullable: true),
                current_user_created = table.Column<string>(nullable: true),
                current_user_deleted = table.Column<string>(nullable: true),
                current_user_updated = table.Column<string>(nullable: true),
                deleted_time = table.Column<DateTime>(nullable: true),
                is_deleted = table.Column<bool>(nullable: false),
                update_time = table.Column<DateTime>(nullable: true)
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

        // Create LLMUsages table
        migrationBuilder.CreateTable(
            name: "llm_usages",
            columns: table => new
            {
                id = table.Column<Guid>(nullable: false),
                user_id = table.Column<string>(nullable: false),
                model_id = table.Column<Guid>(nullable: false),
                conversation_id = table.Column<string>(maxLength: 100, nullable: false),
                input_tokens = table.Column<int>(nullable: false),
                output_tokens = table.Column<int>(nullable: false),
                cost = table.Column<decimal>(type: "decimal(10, 4)", nullable: false),
                user_message = table.Column<string>(maxLength: 4000, nullable: true),
                assistant_message = table.Column<string>(maxLength: 4000, nullable: true),
                response_time_ms = table.Column<int>(nullable: false),
                has_error = table.Column<bool>(nullable: false),
                error_message = table.Column<string>(maxLength: 500, nullable: true),
                functions_called = table.Column<string>(maxLength: 200, nullable: true),
                create_time = table.Column<DateTime>(nullable: true),
                current_user_created = table.Column<string>(nullable: true),
                current_user_deleted = table.Column<string>(nullable: true),
                current_user_updated = table.Column<string>(nullable: true),
                deleted_time = table.Column<DateTime>(nullable: true),
                is_deleted = table.Column<bool>(nullable: false),
                update_time = table.Column<DateTime>(nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("pk_llm_usages", x => x.id);
                table.ForeignKey(
                    name: "fk_llm_usages_asp_net_users_user_id",
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

        // Create indexes
        migrationBuilder.CreateIndex(
            name: "ix_llm_models_provider_id",
            table: "llm_models",
            column: "provider_id");

        migrationBuilder.CreateIndex(
            name: "ix_llm_models_is_enabled",
            table: "llm_models",
            column: "is_enabled");

        migrationBuilder.CreateIndex(
            name: "ix_llm_conversations_user_id",
            table: "llm_conversations",
            column: "user_id");

        migrationBuilder.CreateIndex(
            name: "ix_llm_conversations_last_message_at",
            table: "llm_conversations",
            column: "last_message_at");

        migrationBuilder.CreateIndex(
            name: "ix_llm_messages_conversation_id",
            table: "llm_messages",
            column: "conversation_id");

        migrationBuilder.CreateIndex(
            name: "ix_llm_usages_user_id_create_time",
            table: "llm_usages",
            columns: new[] { "user_id", "create_time" });

        migrationBuilder.CreateIndex(
            name: "ix_llm_usages_model_id",
            table: "llm_usages",
            column: "model_id");

        // Insert default data after tables are created
        InsertDefaultData(migrationBuilder);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "llm_messages");
        migrationBuilder.DropTable(name: "llm_usages");
        migrationBuilder.DropTable(name: "llm_conversations");
        migrationBuilder.DropTable(name: "llm_models");
        migrationBuilder.DropTable(name: "llm_providers");
    }

    private void InsertDefaultData(MigrationBuilder migrationBuilder)
    {
        var now = DateTime.UtcNow;
        
        // Insert providers using SQL
        var openAiId = Guid.NewGuid();
        var anthropicId = Guid.NewGuid();
        var googleId = Guid.NewGuid();

        migrationBuilder.Sql($@"
            INSERT INTO llm_providers (id, provider_id, provider_name, is_enabled, priority, create_time, update_time, is_deleted) VALUES 
            ('{openAiId}', 'openai', 'OpenAI', false, 1, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            ('{anthropicId}', 'anthropic', 'Anthropic', false, 2, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            ('{googleId}', 'google', 'Google Gemini', false, 3, '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false);
        ");

        migrationBuilder.Sql($@"
            INSERT INTO llm_models (id, model_id, model_name, api_model_id, provider_id, is_enabled, is_default, cost_per_input_token, cost_per_output_token, max_tokens, context_window, category, create_time, update_time, is_deleted) VALUES 
            ('{Guid.NewGuid()}', 'gpt-35-turbo', 'GPT-3.5 Turbo', 'gpt-3.5-turbo', '{openAiId}', false, false, 0.0005, 0.0015, 4096, 16385, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            ('{Guid.NewGuid()}', 'gpt-4', 'GPT-4', 'gpt-4', '{openAiId}', false, false, 0.03, 0.06, 8192, 8192, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            ('{Guid.NewGuid()}', 'gpt-5', 'GPT-5', 'gpt-5', '{openAiId}', false, true, 0.05, 0.10, 16384, 256000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            ('{Guid.NewGuid()}', 'gpt-5-turbo', 'GPT-5 Turbo', 'gpt-5-turbo', '{openAiId}', false, false, 0.02, 0.04, 16384, 256000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            ('{Guid.NewGuid()}', 'claude-37-haiku', 'Claude 3.7 Haiku', 'claude-3.7-haiku-20250115', '{anthropicId}', false, false, 0.0002, 0.001, 4096, 200000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            ('{Guid.NewGuid()}', 'claude-37-sonnet', 'Claude 3.7 Sonnet', 'claude-3.7-sonnet-20250115', '{anthropicId}', false, false, 0.0025, 0.012, 4096, 200000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            ('{Guid.NewGuid()}', 'claude-37-opus', 'Claude 3.7 Opus', 'claude-3.7-opus-20250115', '{anthropicId}', false, false, 0.012, 0.06, 4096, 200000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            ('{Guid.NewGuid()}', 'gemini-25-flash', 'Gemini 2.5 Flash', 'gemini-2.5-flash', '{googleId}', false, false, 0.00025, 0.0005, 8192, 1000000, 'fast', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            ('{Guid.NewGuid()}', 'gemini-25-pro', 'Gemini 2.5 Pro', 'gemini-2.5-pro', '{googleId}', false, false, 0.0025, 0.0075, 8192, 1000000, 'balanced', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false),
            ('{Guid.NewGuid()}', 'gemini-25-ultra', 'Gemini 2.5 Ultra', 'gemini-2.5-ultra', '{googleId}', false, false, 0.005, 0.015, 8192, 1000000, 'powerful', '{now:yyyy-MM-dd HH:mm:ss}', '{now:yyyy-MM-dd HH:mm:ss}', false);
        ");
    }
}