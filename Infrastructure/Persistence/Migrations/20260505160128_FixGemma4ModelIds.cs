using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class FixGemma4ModelIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE llm_models
                SET model_id     = 'gemma-4-26b-a4b',
                    model_name   = 'Gemma 4 26B (MoE)',
                    api_model_id = 'gemma-4-26b-a4b-it',
                    category     = 'balanced',
                    update_time  = NOW()
                WHERE model_id = 'gemma-4-27b';

                UPDATE llm_models
                SET model_id     = 'gemma-4-31b',
                    model_name   = 'Gemma 4 31B',
                    api_model_id = 'gemma-4-31b-it',
                    category     = 'powerful',
                    update_time  = NOW()
                WHERE model_id = 'gemma-4-9b';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                UPDATE llm_models SET model_id = 'gemma-4-27b', model_name = 'Gemma 4 27B', api_model_id = 'gemma-4-27b-it', category = 'balanced', update_time = NOW() WHERE model_id = 'gemma-4-26b-a4b';
                UPDATE llm_models SET model_id = 'gemma-4-9b',  model_name = 'Gemma 4 9B',  api_model_id = 'gemma-4-9b-it',  category = 'fast',     update_time = NOW() WHERE model_id = 'gemma-4-31b';
            ");
        }
    }
}
