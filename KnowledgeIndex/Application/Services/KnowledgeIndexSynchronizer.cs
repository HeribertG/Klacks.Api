// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Security.Cryptography;
using System.Text;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Domain;
using Klacks.Api.KnowledgeIndex.Presentation.Attributes;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Synchronizes the knowledge_index table with the current set of registered skills.
/// Computes SHA256 of embedding text, diffs against stored hashes, embeds only changed
/// entries, upserts them, and deletes orphans removed from the skill registry.
/// </summary>
/// <param name="skillRegistry">Registry providing all currently registered skill descriptors.</param>
/// <param name="embeddingProvider">Provider used to compute text embeddings for new or changed entries.</param>
/// <param name="repository">Repository for reading hashes and writing knowledge index entries.</param>
public sealed class KnowledgeIndexSynchronizer : IKnowledgeIndexSynchronizer
{
    private readonly ISkillRegistry _skillRegistry;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IKnowledgeIndexRepository _repository;

    public KnowledgeIndexSynchronizer(
        ISkillRegistry skillRegistry,
        IEmbeddingProvider embeddingProvider,
        IKnowledgeIndexRepository repository)
    {
        _skillRegistry = skillRegistry;
        _embeddingProvider = embeddingProvider;
        _repository = repository;
    }

    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var existingHashes = await _repository.GetAllHashesAsync(cancellationToken);
        var current = BuildCurrentEntries();

        var toEmbed = current
            .Where(x => !existingHashes.TryGetValue((x.Entry.Kind, x.Entry.SourceId), out var h)
                        || !h.SequenceEqual(x.Entry.TextHash))
            .ToList();

        if (toEmbed.Count > 0)
        {
            var texts = toEmbed.Select(x => x.EmbeddingText).ToList();
            var vectors = await _embeddingProvider.EmbedBatchAsync(texts, cancellationToken);
            for (var i = 0; i < toEmbed.Count; i++)
                toEmbed[i].Entry.Embedding = vectors[i];

            var entries = toEmbed.Select(x => x.Entry).ToList();
            await _repository.UpsertAsync(entries, cancellationToken);
        }

        var currentKeys = current.Select(x => (x.Entry.Kind, x.Entry.SourceId)).ToHashSet();
        var orphans = existingHashes.Keys.Where(k => !currentKeys.Contains(k)).ToList();
        if (orphans.Count > 0)
            await _repository.DeleteAsync(orphans, cancellationToken);
    }

    private List<(KnowledgeEntry Entry, string EmbeddingText)> BuildCurrentEntries()
    {
        var result = new List<(KnowledgeEntry, string)>();

        foreach (var skill in _skillRegistry.GetAllSkills())
        {
            var exposedEndpointKey = GetExposedEndpointKey(skill);
            var embeddingText = BuildEmbeddingText(skill);
            var textHash = SHA256.HashData(Encoding.UTF8.GetBytes(embeddingText));
            var requiredPermission = skill.RequiredPermissions.Count > 0
                ? skill.RequiredPermissions[0]
                : null;

            result.Add((new KnowledgeEntry
            {
                Id = Guid.NewGuid(),
                Kind = KnowledgeEntryKind.Skill,
                SourceId = skill.Name,
                Text = embeddingText,
                TextHash = textHash,
                RequiredPermission = requiredPermission,
                ExposedEndpointKey = exposedEndpointKey,
                UpdatedAt = DateTime.UtcNow
            }, embeddingText));
        }

        return result;
    }

    private static string? GetExposedEndpointKey(SkillDescriptor skill)
    {
        if (skill.ImplementationType is null) return null;

        return skill.ImplementationType
            .GetCustomAttributes(typeof(ExposesEndpointAttribute), false)
            .Cast<ExposesEndpointAttribute>()
            .FirstOrDefault()
            ?.EndpointKey;
    }

    private static string BuildEmbeddingText(SkillDescriptor skill)
    {
        var sb = new StringBuilder();
        sb.Append(skill.Name);
        sb.Append(". ");
        sb.Append(skill.Description);
        sb.Append('\n');
        sb.Append("Parameters: ");
        sb.Append(string.Join(", ", skill.Parameters.Select(p => $"{p.Name} ({p.Type})")));

        if (skill.TriggerKeywords.Count > 0)
        {
            sb.Append('\n');
            sb.Append("Keywords: ");
            sb.Append(string.Join(", ", skill.TriggerKeywords));
        }

        var synonyms = skill.Synonyms?.Values
            .SelectMany(values => values)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (synonyms is { Count: > 0 })
        {
            sb.Append('\n');
            sb.Append("Synonyms: ");
            sb.Append(string.Join(", ", synonyms));
        }

        return sb.ToString();
    }
}
