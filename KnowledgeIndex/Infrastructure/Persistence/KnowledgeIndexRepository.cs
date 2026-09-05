// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Globalization;
using System.Text;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Domain;
using Npgsql;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Persistence;

/// <summary>
/// PostgreSQL repository for knowledge index entries using pgvector.
/// Uses raw Npgsql SQL because EF Core cannot handle the vector type natively.
/// </summary>
/// <param name="connection">Open Npgsql connection to the PostgreSQL database.</param>
public sealed class KnowledgeIndexRepository : IKnowledgeIndexRepository
{
    private readonly NpgsqlConnection _connection;

    public KnowledgeIndexRepository(NpgsqlConnection connection) => _connection = connection;

    public async Task<IReadOnlyDictionary<(KnowledgeEntryKind Kind, string SourceId), byte[]>> GetAllHashesAsync(CancellationToken ct)
    {
        const string sql = "SELECT kind, source_id, text_hash FROM knowledge_index;";
        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;

        var result = new Dictionary<(KnowledgeEntryKind, string), byte[]>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            var kind = (KnowledgeEntryKind)(short)reader["kind"];
            var sourceId = (string)reader["source_id"];
            var hash = (byte[])reader["text_hash"];
            result[(kind, sourceId)] = hash;
        }

        return result;
    }

    public async Task UpsertAsync(IReadOnlyList<KnowledgeEntry> entries, CancellationToken ct)
    {
        foreach (var entry in entries)
        {
            var vectorLiteral = ToVectorLiteral(entry.Embedding);
            const string sql = """
                INSERT INTO knowledge_index
                    (id, kind, source_id, text, text_hash, embedding, required_permission, exposed_endpoint_key, updated_at)
                VALUES
                    (@id, @kind, @sourceId, @text, @textHash, @embedding::vector, @requiredPermission, @exposedEndpointKey, @updatedAt)
                ON CONFLICT (kind, source_id)
                DO UPDATE SET
                    text = EXCLUDED.text,
                    text_hash = EXCLUDED.text_hash,
                    embedding = EXCLUDED.embedding,
                    required_permission = EXCLUDED.required_permission,
                    exposed_endpoint_key = EXCLUDED.exposed_endpoint_key,
                    updated_at = EXCLUDED.updated_at;
                """;

            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            cmd.Parameters.AddWithValue("id", entry.Id);
            cmd.Parameters.AddWithValue("kind", (short)entry.Kind);
            cmd.Parameters.AddWithValue("sourceId", entry.SourceId);
            cmd.Parameters.AddWithValue("text", entry.Text);
            cmd.Parameters.AddWithValue("textHash", entry.TextHash);
            cmd.Parameters.AddWithValue("embedding", vectorLiteral);
            cmd.Parameters.AddWithValue("requiredPermission", (object?)entry.RequiredPermission ?? DBNull.Value);
            cmd.Parameters.AddWithValue("exposedEndpointKey", (object?)entry.ExposedEndpointKey ?? DBNull.Value);
            cmd.Parameters.AddWithValue("updatedAt", entry.UpdatedAt);

            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task DeleteAsync(IReadOnlyList<(KnowledgeEntryKind Kind, string SourceId)> keys, CancellationToken ct)
    {
        foreach (var (kind, sourceId) in keys)
        {
            await using var cmd = _connection.CreateCommand();
            cmd.CommandText = "DELETE FROM knowledge_index WHERE kind = @kind AND source_id = @sourceId;";
            cmd.Parameters.AddWithValue("kind", (short)kind);
            cmd.Parameters.AddWithValue("sourceId", sourceId);
            await cmd.ExecuteNonQueryAsync(ct);
        }
    }

    public async Task<IReadOnlyList<KnowledgeEntry>> FindNearestAsync(
        float[] queryEmbedding,
        IReadOnlyCollection<string> userPermissions,
        bool adminBypass,
        int topN,
        CancellationToken ct,
        KnowledgeEntryKind? kindFilter = null)
    {
        var vectorLiteral = ToVectorLiteral(queryEmbedding);
        var permArray = userPermissions.ToArray();

        const string sql = """
            SELECT id, kind, source_id, text, text_hash, embedding::text,
                   required_permission, exposed_endpoint_key, updated_at
              FROM knowledge_index
             WHERE (@adminBypass
                OR required_permission IS NULL
                OR required_permission = ANY(@userPermissions))
               AND (@kind IS NULL OR kind = @kind)
             ORDER BY embedding <=> @queryVec::vector
             LIMIT @topN;
            """;

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("adminBypass", adminBypass);
        cmd.Parameters.AddWithValue("userPermissions", permArray);
        cmd.Parameters.AddWithValue("queryVec", vectorLiteral);
        cmd.Parameters.AddWithValue("topN", topN);
        cmd.Parameters.Add("kind", NpgsqlTypes.NpgsqlDbType.Smallint).Value =
            kindFilter is null ? DBNull.Value : (short)kindFilter.Value;

        var results = new List<KnowledgeEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(MapRow(reader));
        }

        return results;
    }

    public async Task<IReadOnlyList<KnowledgeEntry>> FindLexicalAsync(
        string query,
        IReadOnlyCollection<string> userPermissions,
        bool adminBypass,
        int topN,
        CancellationToken ct)
    {
        var permArray = userPermissions.ToArray();

        const string sql = """
            SELECT id, kind, source_id, text, text_hash, embedding::text,
                   required_permission, exposed_endpoint_key, updated_at
              FROM knowledge_index
             WHERE @adminBypass
                OR required_permission IS NULL
                OR required_permission = ANY(@userPermissions)
             ORDER BY word_similarity(@query, text) DESC, similarity(@query, text) DESC
             LIMIT @topN;
            """;

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("adminBypass", adminBypass);
        cmd.Parameters.AddWithValue("userPermissions", permArray);
        cmd.Parameters.AddWithValue("query", query);
        cmd.Parameters.AddWithValue("topN", topN);

        var results = new List<KnowledgeEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(MapRow(reader));
        }

        return results;
    }

    public async Task<IReadOnlyList<KnowledgeEntry>> GetByKeysAsync(
        IReadOnlyList<(KnowledgeEntryKind Kind, string SourceId)> keys,
        CancellationToken ct)
    {
        if (keys.Count == 0)
        {
            return Array.Empty<KnowledgeEntry>();
        }

        var kinds = new short[keys.Count];
        var sourceIds = new string[keys.Count];
        for (var i = 0; i < keys.Count; i++)
        {
            kinds[i] = (short)keys[i].Kind;
            sourceIds[i] = keys[i].SourceId;
        }

        // unnest pairs the two arrays element-wise into (kind, source_id) tuples, so the join matches the
        // EXACT keys requested (not the cross product of kinds and ids). Casing is significant here, as it
        // is for the stored source_id.
        const string sql = """
            SELECT ki.id, ki.kind, ki.source_id, ki.text, ki.text_hash, ki.embedding::text,
                   ki.required_permission, ki.exposed_endpoint_key, ki.updated_at
              FROM knowledge_index ki
              JOIN unnest(@kinds::smallint[], @sourceIds::text[]) AS k(kind, source_id)
                ON ki.kind = k.kind AND ki.source_id = k.source_id;
            """;

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("kinds", kinds);
        cmd.Parameters.AddWithValue("sourceIds", sourceIds);

        var results = new List<KnowledgeEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(MapRow(reader));
        }

        return results;
    }

    public async Task<IReadOnlyList<KnowledgeEntry>> GetAllWithEmbeddingsAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT id, kind, source_id, text, text_hash, embedding::text,
                   required_permission, exposed_endpoint_key, updated_at
              FROM knowledge_index
             ORDER BY kind, source_id;
            """;

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;

        var results = new List<KnowledgeEntry>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            results.Add(MapRow(reader));
        }

        return results;
    }

    private static KnowledgeEntry MapRow(NpgsqlDataReader reader)
    {
        var embeddingText = (string)reader["embedding"];
        return new KnowledgeEntry
        {
            Id = (Guid)reader["id"],
            Kind = (KnowledgeEntryKind)(short)reader["kind"],
            SourceId = (string)reader["source_id"],
            Text = (string)reader["text"],
            TextHash = (byte[])reader["text_hash"],
            Embedding = ParseVectorLiteral(embeddingText),
            RequiredPermission = reader["required_permission"] as string,
            ExposedEndpointKey = reader["exposed_endpoint_key"] as string,
            UpdatedAt = (DateTime)reader["updated_at"]
        };
    }

    private static string ToVectorLiteral(float[] values)
    {
        var sb = new StringBuilder("[");
        for (var i = 0; i < values.Length; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append(values[i].ToString("G9", CultureInfo.InvariantCulture));
        }
        sb.Append(']');
        return sb.ToString();
    }

    private static float[] ParseVectorLiteral(string literal)
    {
        var inner = literal.TrimStart('[').TrimEnd(']');
        var parts = inner.Split(',');
        var result = new float[parts.Length];
        for (var i = 0; i < parts.Length; i++)
            result[i] = float.Parse(parts[i], CultureInfo.InvariantCulture);
        return result;
    }
}
