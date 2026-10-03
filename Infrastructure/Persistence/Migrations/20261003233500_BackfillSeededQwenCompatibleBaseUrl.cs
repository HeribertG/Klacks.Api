using Klacks.Api.Data.Seed;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Repairs the base URL of the seeded Qwen provider on existing databases: older builds seeded the native
    /// DashScope API (https://dashscope.aliyuncs.com/api/v1/), which has no chat/completions route, so the
    /// OpenAI-compatible provider that serves Qwen could never answer. The URL is replaced by the OpenAI-compatible
    /// endpoint only while the row still holds exactly the old seeded value, so an administrator's own URL survives.
    /// Data only - the model is unchanged. Down is empty on purpose: it would reintroduce the broken URL.
    /// </summary>
    public partial class BackfillSeededQwenCompatibleBaseUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(LLMProviderBaseUrlCorrectionSql.BuildQwenStatement());
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
