// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Klacks.Api.KnowledgeIndex.Domain;
using Klacks.Api.KnowledgeIndex.Presentation.Attributes;

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
public sealed class KnowledgeIndexSynchronizer : IKnowledgeIndexSynchronizer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ISkillRegistry _skillRegistry;
    private readonly IAgentRecipeRepository _recipeRepository;
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly IKnowledgeIndexRepository _repository;

    public KnowledgeIndexSynchronizer(
        ISkillRegistry skillRegistry,
        IAgentRecipeRepository recipeRepository,
        IEmbeddingProvider embeddingProvider,
        IKnowledgeIndexRepository repository)
    {
        _skillRegistry = skillRegistry;
        _recipeRepository = recipeRepository;
        _embeddingProvider = embeddingProvider;
        _repository = repository;
    }

    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var existingHashes = await _repository.GetAllHashesAsync(cancellationToken);
        var recipes = await _recipeRepository.GetAllEnabledAsync(cancellationToken);
        var current = BuildCurrentEntries(recipes);

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

    private List<(KnowledgeEntry Entry, string EmbeddingText)> BuildCurrentEntries(IReadOnlyList<AgentRecipe> recipes)
    {
        var result = new List<(KnowledgeEntry, string)>();

        foreach (var skill in _skillRegistry.GetAllSkills())
        {
            var exposedEndpointKey = GetExposedEndpointKey(skill);
            var embeddingText = BuildEmbeddingText(skill);
            var textHash = ComputeTextHash(embeddingText);
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
            var embeddingText = BuildRecipeEmbeddingText(recipe);
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

    private static string BuildRecipeEmbeddingText(AgentRecipe recipe)
    {
        var sb = new StringBuilder();
        sb.Append(recipe.Name);
        sb.Append(". ");
        sb.Append(recipe.Goal);

        var triggerWords = ExtractTriggerWords(recipe.TriggerJson);
        if (triggerWords.Count > 0)
        {
            sb.Append('\n');
            sb.Append("Keywords: ");
            sb.Append(string.Join(", ", triggerWords));
        }

        var synonyms = recipe.Synonyms?.Values
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

        var stepSkills = ExtractStepSkillNames(recipe.StepsJson);
        if (stepSkills.Count > 0)
        {
            sb.Append('\n');
            sb.Append("Steps: ");
            sb.Append(string.Join(", ", stepSkills));
        }

        return sb.ToString();
    }

    private static List<string> ExtractTriggerWords(string? triggerJson)
    {
        if (string.IsNullOrWhiteSpace(triggerJson))
        {
            return [];
        }

        try
        {
            var trigger = JsonSerializer.Deserialize<RecipeTrigger>(triggerJson, JsonOptions);
            if (trigger?.AllOf == null)
            {
                return [];
            }

            return trigger.AllOf
                .SelectMany(condition => (condition.AnyWordStart ?? [])
                    .Concat(condition.AnySubstring ?? [])
                    .Concat(condition.StartsWith ?? []))
                .Where(word => !string.IsNullOrWhiteSpace(word))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
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
