// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Domain;
using Klacks.Api.KnowledgeIndex.Presentation.Attributes;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Synchronizes the knowledge_index table with the current set of registered skills and enabled
/// recipes. Computes SHA256 of embedding text, diffs against stored hashes, embeds only changed
/// entries, upserts them, and deletes orphans removed from the skill registry or recipe table.
/// </summary>
/// <param name="skillRegistry">Registry providing all currently registered skill descriptors.</param>
/// <param name="recipeRepository">Repository providing all currently enabled recipes.</param>
/// <param name="embeddingProvider">Provider used to compute text embeddings for new or changed entries.</param>
/// <param name="repository">Repository for reading hashes and writing knowledge index entries.</param>
/// <param name="phraseRepository">Repository providing the trigger keywords and synonyms of every skill and recipe.</param>
/// <param name="snapshotReader">Reader for the shipped embedding snapshot used to avoid re-embedding known texts.</param>
/// <param name="logger">Logger for the per-run synchronization summary.</param>
public sealed class KnowledgeIndexSynchronizer : IKnowledgeIndexSynchronizer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ISkillRegistry _skillRegistry;
    private readonly IAgentRecipeRepository _recipeRepository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IKnowledgeIndexRepository _repository;
    private readonly ISkillPhraseRepository _phraseRepository;
    private readonly IKnowledgeEmbeddingSnapshotReader _snapshotReader;
    private readonly ILogger<KnowledgeIndexSynchronizer> _logger;

    public KnowledgeIndexSynchronizer(
        ISkillRegistry skillRegistry,
        IAgentRecipeRepository recipeRepository,
        IEmbeddingProvider embeddingProvider,
        IKnowledgeIndexRepository repository,
        ISkillPhraseRepository phraseRepository,
        IKnowledgeEmbeddingSnapshotReader snapshotReader,
        ILogger<KnowledgeIndexSynchronizer> logger)
    {
        _skillRegistry = skillRegistry;
        _recipeRepository = recipeRepository;
        _embeddingProvider = embeddingProvider;
        _repository = repository;
        _phraseRepository = phraseRepository;
        _snapshotReader = snapshotReader;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        var existingHashes = await _repository.GetAllHashesAsync(cancellationToken);
        var recipes = await _recipeRepository.GetAllEnabledAsync(cancellationToken);

        // One query for all owners: this loop covers every registered skill and every enabled recipe,
        // so a per-owner lookup would be several hundred round trips per startup.
        var phraseSets = SkillPhraseGrouper.Group(await _phraseRepository.GetAllActiveAsync(cancellationToken));

        var current = BuildCurrentEntries(recipes, phraseSets);

        var toEmbed = current
            .Where(x => !existingHashes.TryGetValue((x.Entry.Kind, x.Entry.SourceId), out var h)
                        || !h.SequenceEqual(x.Entry.TextHash))
            .ToList();

        var restoredFromSnapshot = 0;

        if (toEmbed.Count > 0)
        {
            // Shipped vectors are resolved first: on a fresh database every entry is dirty, and
            // embedding several hundred texts locally is the single most expensive part of startup.
            var snapshot = await _snapshotReader.LoadAsync(
                _embeddingProvider.EmbeddingSpaceId,
                _embeddingProvider.Dimension,
                cancellationToken);

            var misses = new List<(KnowledgeEntry Entry, string EmbeddingText)>();
            foreach (var candidate in toEmbed)
            {
                if (snapshot.TryGetValue(KnowledgeEmbeddingCodec.ToHex(candidate.Entry.TextHash), out var stored))
                {
                    candidate.Entry.Embedding = stored;
                    restoredFromSnapshot++;
                }
                else
                {
                    misses.Add(candidate);
                }
            }

            if (misses.Count > 0)
            {
                var texts = misses.Select(x => x.EmbeddingText).ToList();
                var vectors = await _embeddingProvider.EmbedBatchAsync(texts, cancellationToken);
                for (var i = 0; i < misses.Count; i++)
                    misses[i].Entry.Embedding = vectors[i];
            }

            var entries = toEmbed.Select(x => x.Entry).ToList();
            await _repository.UpsertAsync(entries, cancellationToken);
        }

        var currentKeys = current.Select(x => (x.Entry.Kind, x.Entry.SourceId)).ToHashSet();
        var orphans = existingHashes.Keys.Where(k => !currentKeys.Contains(k)).ToList();
        if (orphans.Count > 0)
            await _repository.DeleteAsync(orphans, cancellationToken);

        stopwatch.Stop();
        _logger.LogInformation(
            "Knowledge index sync: {Total} entries, {Unchanged} unchanged, {FromSnapshot} restored from snapshot, {Embedded} embedded, {Orphans} orphans removed, {ElapsedMs} ms",
            current.Count,
            current.Count - toEmbed.Count,
            restoredFromSnapshot,
            toEmbed.Count - restoredFromSnapshot,
            orphans.Count,
            stopwatch.ElapsedMilliseconds);
    }

    private List<(KnowledgeEntry Entry, string EmbeddingText)> BuildCurrentEntries(
        IReadOnlyList<AgentRecipe> recipes,
        IReadOnlyDictionary<(string OwnerKind, string OwnerName), IndexPhraseSet> phraseSets)
    {
        var result = new List<(KnowledgeEntry, string)>();

        foreach (var skill in _skillRegistry.GetAllSkills())
        {
            var exposedEndpointKey = GetExposedEndpointKey(skill);
            var embeddingText = BuildEmbeddingText(
                skill,
                GetPhrases(phraseSets, SkillPhraseOwnerKinds.Skill, skill.Name));
            var textHash = ComputeTextHash(embeddingText);

            // One column, so only the first of several permissions reaches the KNN predicate, while
            // SkillToolsetAssembler checks all of them (Permissions.HasAllRequiredPermissions splits
            // the comma-separated list). The asymmetry cannot leak a skill: the assembler is
            // authoritative and drops what the user may not use. It costs candidate slots - 12 of 457
            // skills carry two permissions, and for a user holding only the first one of those, one of
            // the 25 KNN slots is spent on a candidate that will be discarded. Left as is on
            // 2026-08-06: closing it means a schema change, and 12 skills do not pay for one.
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

        foreach (var recipe in recipes)
        {
            var embeddingText = BuildRecipeEmbeddingText(
                recipe,
                GetPhrases(phraseSets, SkillPhraseOwnerKinds.Recipe, recipe.Name));
            var textHash = ComputeTextHash(embeddingText);

            result.Add((new KnowledgeEntry
            {
                Id = Guid.NewGuid(),
                Kind = KnowledgeEntryKind.Recipe,
                SourceId = recipe.Name,
                Text = embeddingText,
                TextHash = textHash,
                RequiredPermission = null,
                ExposedEndpointKey = null,
                UpdatedAt = DateTime.UtcNow
            }, embeddingText));
        }

        return result;
    }

    // The embedding space id is part of the hash: vectors from different embedding models are not
    // comparable, so switching providers (ONNX on x64 vs Gemini fallback on ARM64) must invalidate
    // every stored entry and force a full re-embed in the active provider's space.
    private byte[] ComputeTextHash(string embeddingText) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(_embeddingProvider.EmbeddingSpaceId + "\n" + embeddingText));

    private static string? GetExposedEndpointKey(SkillDescriptor skill)
    {
        if (skill.ImplementationType is null) return null;

        return skill.ImplementationType
            .GetCustomAttributes(typeof(ExposesEndpointAttribute), false)
            .Cast<ExposesEndpointAttribute>()
            .FirstOrDefault()
            ?.EndpointKey;
    }

    private static IndexPhraseSet GetPhrases(
        IReadOnlyDictionary<(string OwnerKind, string OwnerName), IndexPhraseSet> phraseSets,
        string ownerKind,
        string ownerName) =>
        phraseSets.TryGetValue((ownerKind, ownerName), out var phrases) ? phrases : IndexPhraseSet.Empty;

    // Keywords and synonyms come from skill_phrase, not from the descriptor. The section order,
    // the "Keywords: " and "Synonyms: " labels and the ", " separator are part of the hashed text:
    // changing any of them re-embeds every entry in the index.
    private static string BuildEmbeddingText(SkillDescriptor skill, IndexPhraseSet phrases)
    {
        var sb = new StringBuilder();
        sb.Append(skill.Name);
        sb.Append(". ");
        sb.Append(skill.Description);
        sb.Append('\n');
        sb.Append("Parameters: ");
        sb.Append(string.Join(", ", skill.Parameters.Select(p => $"{p.Name} ({p.Type})")));

        AppendPhraseSections(sb, phrases);

        return sb.ToString();
    }

    private static string BuildRecipeEmbeddingText(AgentRecipe recipe, IndexPhraseSet phrases)
    {
        var sb = new StringBuilder();
        sb.Append(recipe.Name);
        sb.Append(". ");
        sb.Append(recipe.Goal);

        AppendPhraseSections(sb, phrases);

        var stepSkills = ExtractStepSkillNames(recipe.StepsJson);
        if (stepSkills.Count > 0)
        {
            sb.Append('\n');
            sb.Append("Steps: ");
            sb.Append(string.Join(", ", stepSkills));
        }

        return sb.ToString();
    }

    // Keywords precede synonyms and an empty list emits no section at all - both facts are baked into
    // every stored hash.
    //
    // A phrase that is both a keyword and a synonym of the same owner is emitted ONCE, in the keyword
    // section. Measured on the seed catalogue, 811 of 3112 keywords were also synonyms of their own
    // skill and reached the embedding text twice, which inflates the text for no retrieval benefit and
    // pushes the trailing synonym section closer to the tokenizer's 512-token cap. The two source lists
    // stay untouched: only this text projection deduplicates, so both matching paths keep their data.
    private static void AppendPhraseSections(StringBuilder sb, IndexPhraseSet phrases)
    {
        if (phrases.Keywords.Count > 0)
        {
            sb.Append('\n');
            sb.Append("Keywords: ");
            sb.Append(string.Join(", ", phrases.Keywords));
        }

        var keywords = phrases.Keywords.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var synonyms = phrases.Synonyms.Where(s => !keywords.Contains(s)).ToList();

        if (synonyms.Count > 0)
        {
            sb.Append('\n');
            sb.Append("Synonyms: ");
            sb.Append(string.Join(", ", synonyms));
        }
    }

    private static List<string> ExtractStepSkillNames(string? stepsJson)
    {
        if (string.IsNullOrWhiteSpace(stepsJson))
        {
            return [];
        }

        try
        {
            var steps = JsonSerializer.Deserialize<List<RecipeStep>>(stepsJson, JsonOptions);
            if (steps == null)
            {
                return [];
            }

            return steps
                .Where(s => !string.IsNullOrWhiteSpace(s.Skill))
                .Select(s => s.Skill!)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
