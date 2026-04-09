// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Globalization;
using System.Text;
using Klacks.Api.Infrastructure.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.Infrastructure.KnowledgeIndex.Domain;
using Npgsql;

namespace Klacks.Api.Infrastructure.KnowledgeIndex.Infrastructure.Persistence;

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
        CancellationToken ct)
    {
        var vectorLiteral = ToVectorLiteral(queryEmbedding);
        var permArray = userPermissions.ToArray();

        const string sql = """
            SELECT id, kind, source_id, text, text_hash, embedding::text,
                   required_permission, exposed_endpoint_key, updated_at
              FROM knowledge_index
             WHERE @adminBypass
                OR required_permission IS NULL
                OR required_permission = ANY(@userPermissions)
             ORDER BY embedding <=> @queryVec::vector
             LIMIT @topN;
            """;

        await using var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.Parameters.AddWithValue("adminBypass", adminBypass);
        cmd.Parameters.AddWithValue("userPermissions", permArray);
        cmd.Parameters.AddWithValue("queryVec", vectorLiteral);
        cmd.Parameters.AddWithValue("topN", topN);

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
