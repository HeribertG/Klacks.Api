using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Klacks.Api.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Widens knowledge_index.embedding from vector(384) to vector(768) for the switch from
    /// multilingual-e5-small to -base. The stored vectors are deleted rather than converted: vectors
    /// from different embedding models are not comparable, so keeping them would silently mix
    /// incompatible geometries. The rows rebuild themselves on the next startup — the synchronizer
    /// folds the embedding space id into its stored text hash, so every entry already counts as
    /// changed. The HNSW index is bound to the column type and has to be dropped and recreated.
    /// </summary>
    public partial class EmbeddingDimension768 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS knowledge_index_embedding_idx;");
            migrationBuilder.Sql("DELETE FROM knowledge_index;");
            migrationBuilder.Sql("ALTER TABLE knowledge_index ALTER COLUMN embedding DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE knowledge_index ALTER COLUMN embedding TYPE vector(768);");
            migrationBuilder.Sql("ALTER TABLE knowledge_index ALTER COLUMN embedding SET DEFAULT array_fill(0, ARRAY[768])::vector;");
            migrationBuilder.Sql("CREATE INDEX knowledge_index_embedding_idx ON knowledge_index USING hnsw (embedding vector_cosine_ops);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS knowledge_index_embedding_idx;");
            migrationBuilder.Sql("DELETE FROM knowledge_index;");
            migrationBuilder.Sql("ALTER TABLE knowledge_index ALTER COLUMN embedding DROP DEFAULT;");
            migrationBuilder.Sql("ALTER TABLE knowledge_index ALTER COLUMN embedding TYPE vector(384);");
            migrationBuilder.Sql("ALTER TABLE knowledge_index ALTER COLUMN embedding SET DEFAULT array_fill(0, ARRAY[384])::vector;");
            migrationBuilder.Sql("CREATE INDEX knowledge_index_embedding_idx ON knowledge_index USING hnsw (embedding vector_cosine_ops);");
        }
    }
}
