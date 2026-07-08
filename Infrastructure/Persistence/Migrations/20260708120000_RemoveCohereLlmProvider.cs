using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(DataBaseContext))]
    [Migration("20260708120000_RemoveCohereLlmProvider")]
    public partial class RemoveCohereLlmProvider : Migration
    {
        private const string CohereProviderId = "cohere";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"""
                DELETE FROM llm_usages
                WHERE model_id IN (SELECT id FROM llm_models WHERE provider_id = '{CohereProviderId}');
                """);

            migrationBuilder.Sql($"""
                DELETE FROM llm_models WHERE provider_id = '{CohereProviderId}';
                """);

            migrationBuilder.Sql($"""
                DELETE FROM llm_providers WHERE provider_id = '{CohereProviderId}';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
