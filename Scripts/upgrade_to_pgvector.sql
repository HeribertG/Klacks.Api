-- ═══════════════════════════════════════════════════════════════
-- UPGRADE SCRIPT: Enable pgvector for hybrid vector search
-- ═══════════════════════════════════════════════════════════════
-- Prerequisites:
--   1. Install pgvector extension on PostgreSQL server
--      (run install_pgvector.bat as Administrator)
--   2. Then execute this script
-- ═══════════════════════════════════════════════════════════════

CREATE EXTENSION IF NOT EXISTS vector;

ALTER TABLE agent_memories
    ALTER COLUMN embedding TYPE vector(1536)
    USING embedding::vector(1536);

CREATE INDEX IF NOT EXISTS ix_agent_memories_embedding
    ON agent_memories USING hnsw (embedding vector_cosine_ops)
    WITH (m = 16, ef_construction = 100);

-- Verify
SELECT 'pgvector upgrade complete' AS status,
       COUNT(*) FILTER (WHERE embedding IS NOT NULL) AS memories_with_embeddings,
       COUNT(*) AS total_memories
FROM agent_memories WHERE is_deleted = false;
