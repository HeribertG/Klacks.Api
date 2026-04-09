using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");

            migrationBuilder.CreateTable(
                name: "knowledge_index",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    kind = table.Column<short>(type: "smallint", nullable: false),
                    source_id = table.Column<string>(type: "text", nullable: false),
                    text = table.Column<string>(type: "text", nullable: false),
                    text_hash = table.Column<byte[]>(type: "bytea", nullable: false),
                    required_permission = table.Column<string>(type: "text", nullable: true),
                    exposed_endpoint_key = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_knowledge_index", x => x.id);
                });

            migrationBuilder.Sql("ALTER TABLE knowledge_index ADD COLUMN embedding vector(384) NOT NULL DEFAULT array_fill(0, ARRAY[384])::vector;");

            migrationBuilder.CreateIndex(
                name: "knowledge_index_kind_source_unique",
                table: "knowledge_index",
                columns: new[] { "kind", "source_id" },
                unique: true);

            migrationBuilder.Sql("CREATE INDEX knowledge_index_permission_idx ON knowledge_index (required_permission);");

            migrationBuilder.Sql("CREATE INDEX knowledge_index_embedding_idx ON knowledge_index USING hnsw (embedding vector_cosine_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "knowledge_index");
        }
    }
}
