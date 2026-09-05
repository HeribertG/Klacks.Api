// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Text.Json;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Application.Services;
using Klacks.Api.KnowledgeIndex.Domain;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.KnowledgeIndex.Infrastructure.Persistence;

/// <summary>
/// Reads the shipped knowledge index snapshot from disk once per process, validates it against the
/// active embedding space, and caches the resulting text hash to vector lookup so repeated syncs do
/// not decode the file again. Every failure mode degrades to an empty result instead of throwing,
/// because a missing or stale snapshot only means the synchronizer has to embed the entries itself.
/// </summary>
/// <param name="filePath">Absolute path of the snapshot file.</param>
/// <param name="enabled">When false the file is never read and the lookup stays empty.</param>
/// <param name="logger">Logger for load diagnostics and validation mismatches.</param>
public sealed class FileKnowledgeEmbeddingSnapshotReader : IKnowledgeEmbeddingSnapshotReader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, float[]> Empty =
        new Dictionary<string, float[]>(StringComparer.Ordinal);

    private readonly string _filePath;
    private readonly bool _enabled;
    private readonly ILogger<FileKnowledgeEmbeddingSnapshotReader> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private (string SpaceId, int Dimension)? _cachedKey;
    private IReadOnlyDictionary<string, float[]> _cachedLookup = Empty;

    public FileKnowledgeEmbeddingSnapshotReader(
        string filePath,
        bool enabled,
        ILogger<FileKnowledgeEmbeddingSnapshotReader> logger)
    {
        _filePath = filePath;
        _enabled = enabled;
        _logger = logger;
    }

    public async Task<IReadOnlyDictionary<string, float[]>> LoadAsync(
        string embeddingSpaceId,
        int dimension,
        CancellationToken ct)
    {
        var key = (embeddingSpaceId, dimension);
        if (_cachedKey == key)
        {
            return _cachedLookup;
        }

        await _gate.WaitAsync(ct);
        try
        {
            if (_cachedKey == key)
            {
                return _cachedLookup;
            }

            var document = await ReadDocumentAsync();
            _cachedLookup = Validate(document, embeddingSpaceId, dimension)
                ? BuildLookup(document!, dimension)
                : Empty;
            _cachedKey = key;
            return _cachedLookup;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool Validate(KnowledgeEmbeddingSnapshotDocument? document, string embeddingSpaceId, int dimension)
    {
        if (document?.Entries is null)
        {
            return false;
        }

        if (document.FormatVersion != KnowledgeIndexConstants.SnapshotFormatVersion)
        {
            _logger.LogWarning(
                "Knowledge index snapshot ignored: format version {ActualVersion}, expected {ExpectedVersion} ({Path}).",
                document.FormatVersion,
                KnowledgeIndexConstants.SnapshotFormatVersion,
                _filePath);
            return false;
        }

        if (!string.Equals(document.EmbeddingSpaceId, embeddingSpaceId, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Knowledge index snapshot ignored: embedding space {ActualSpace}, expected {ExpectedSpace} ({Path}).",
                document.EmbeddingSpaceId,
                embeddingSpaceId,
                _filePath);
            return false;
        }

        if (document.Dimension != dimension)
        {
            _logger.LogWarning(
                "Knowledge index snapshot ignored: dimension {ActualDimension}, expected {ExpectedDimension} ({Path}).",
                document.Dimension,
                dimension,
                _filePath);
            return false;
        }

        return true;
    }

    private IReadOnlyDictionary<string, float[]> BuildLookup(
        KnowledgeEmbeddingSnapshotDocument document,
        int dimension)
    {
        var lookup = new Dictionary<string, float[]>(document.Entries.Count, StringComparer.Ordinal);
        var skipped = 0;

        foreach (var entry in document.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.TextHash) || string.IsNullOrWhiteSpace(entry.Embedding))
            {
                skipped++;
                continue;
            }

            float[] vector;
            try
            {
                vector = KnowledgeEmbeddingCodec.DecodeVector(entry.Embedding);
            }
            catch (FormatException)
            {
                skipped++;
                continue;
            }

            if (vector.Length != dimension)
            {
                skipped++;
                continue;
            }

            lookup.TryAdd(entry.TextHash.ToLowerInvariant(), vector);
        }

        if (skipped > 0)
        {
            _logger.LogWarning(
                "Knowledge index snapshot: {Skipped} of {Total} entries skipped because their vector was unusable ({Path}).",
                skipped,
                document.Entries.Count,
                _filePath);
        }

        _logger.LogInformation(
            "Knowledge index snapshot loaded: {Count} vectors from {Path}.",
            lookup.Count,
            _filePath);

        return lookup;
    }

    private async Task<KnowledgeEmbeddingSnapshotDocument?> ReadDocumentAsync()
    {
        if (!_enabled)
        {
            _logger.LogInformation("Knowledge index snapshot disabled by configuration.");
            return null;
        }

        if (!File.Exists(_filePath))
        {
            _logger.LogInformation("Knowledge index snapshot not found at {Path}.", _filePath);
            return null;
        }

        try
        {
            await using var stream = File.OpenRead(_filePath);
            return await JsonSerializer.DeserializeAsync<KnowledgeEmbeddingSnapshotDocument>(stream, JsonOptions);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Knowledge index snapshot at {Path} could not be parsed.", _filePath);
            return null;
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Knowledge index snapshot at {Path} could not be read.", _filePath);
            return null;
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Knowledge index snapshot at {Path} could not be read.", _filePath);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Knowledge index snapshot at {Path} could not be loaded.", _filePath);
            return null;
        }
    }
}
