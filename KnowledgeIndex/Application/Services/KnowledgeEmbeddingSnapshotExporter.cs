// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Domain;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Builds a portable snapshot of the current knowledge_index embeddings. Entries whose stored vector
/// does not match the active embedding provider's dimension, or whose vector is all zeros (never
/// successfully embedded), are excluded rather than shipped as unusable data. The export is refused
/// while learned phrases exist, because they change the text hashes and a snapshot taken from such a
/// database would never match a fresh installation.
/// </summary>
/// <param name="repository">Repository providing every knowledge index entry with its embedding.</param>
/// <param name="embeddingProvider">Active embedding provider; supplies the space id and dimension the snapshot is stamped with.</param>
/// <param name="phraseRepository">Repository used to detect learned phrases that make the database unsuitable as a snapshot source.</param>
public sealed class KnowledgeEmbeddingSnapshotExporter : IKnowledgeEmbeddingSnapshotExporter
{
    private readonly IKnowledgeIndexRepository _repository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly ISkillPhraseRepository _phraseRepository;

    public KnowledgeEmbeddingSnapshotExporter(
        IKnowledgeIndexRepository repository,
        IEmbeddingProvider embeddingProvider,
        ISkillPhraseRepository phraseRepository)
    {
        _repository = repository;
        _embeddingProvider = embeddingProvider;
        _phraseRepository = phraseRepository;
    }

    public async Task<KnowledgeEmbeddingSnapshotDocument> ExportAsync(CancellationToken ct)
    {
        var learnedPhrases = (await _phraseRepository.GetAllActiveAsync(ct))
            .Count(phrase => string.Equals(phrase.Source, SkillPhraseSources.Learned, StringComparison.Ordinal));
        if (learnedPhrases > 0)
        {
            throw new InvalidOperationException(
                $"Snapshot export refused: {learnedPhrases} learned phrases exist in this database. " +
                "Learned phrases change the text hashes, so the snapshot must be exported from a freshly seeded database.");
        }

        var entries = await _repository.GetAllWithEmbeddingsAsync(ct);
        var dimension = _embeddingProvider.Dimension;

        var snapshotEntries = entries
            .Where(entry => entry.Embedding.Length == dimension && !IsZeroVector(entry.Embedding))
            .OrderBy(entry => entry.Kind)
            .ThenBy(entry => entry.SourceId, StringComparer.Ordinal)
            .Select(entry => new KnowledgeEmbeddingSnapshotEntry
            {
                Kind = (short)entry.Kind,
                SourceId = entry.SourceId,
                TextHash = KnowledgeEmbeddingCodec.ToHex(entry.TextHash),
                Embedding = KnowledgeEmbeddingCodec.EncodeVector(entry.Embedding)
            })
            .ToList();

        return new KnowledgeEmbeddingSnapshotDocument
        {
            FormatVersion = KnowledgeIndexConstants.SnapshotFormatVersion,
            EmbeddingSpaceId = _embeddingProvider.EmbeddingSpaceId,
            Dimension = dimension,
            CreatedAt = DateTime.UtcNow,
            SourceVersion = $"{VersionConstant.CMajor}.{VersionConstant.CMinor}.{VersionConstant.CPatch}",
            Entries = snapshotEntries
        };
    }

    private static bool IsZeroVector(float[] vector)
    {
        foreach (var value in vector)
        {
            if (value != 0f)
            {
                return false;
            }
        }

        return true;
    }
}
