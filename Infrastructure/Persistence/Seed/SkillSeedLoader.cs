// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Loads skill definitions from skill-seeds.json and syncs them with the AgentSkill database table.
/// Performs INSERT for new skills, UPDATE when seed version exceeds DB version or DB entry is unmodified,
/// and SKIP when the DB entry has a higher or equal version and was user-modified.
/// </summary>
using System.Text.Json;
using System.Text.Json.Nodes;
using Klacks.Api.Application.Constants;
using Klacks.Api.Application.Interfaces.Plugins;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Infrastructure.Persistence.Seed.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Persistence.Seed;

public class SkillSeedLoader
{
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly ISkillPhraseRepository _skillPhraseRepository;
    private readonly IFeaturePluginService _featurePluginService;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<SkillSeedLoader> _logger;

    private static readonly JsonSerializerOptions JsonReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions JsonWriteOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private const string SeedFilePath = "Application/Skills/Definitions/skill-seeds.json";
    private const string DefaultAgentName = "klacks-default";

    public SkillSeedLoader(
        IAgentSkillRepository agentSkillRepository,
        IAgentRepository agentRepository,
        ISkillPhraseRepository skillPhraseRepository,
        IFeaturePluginService featurePluginService,
        IWebHostEnvironment environment,
        ILogger<SkillSeedLoader> logger)
    {
        _agentSkillRepository = agentSkillRepository;
        _agentRepository = agentRepository;
        _skillPhraseRepository = skillPhraseRepository;
        _featurePluginService = featurePluginService;
        _environment = environment;
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(_environment.ContentRootPath, SeedFilePath);

        if (!File.Exists(filePath))
        {
            _logger.LogInformation("Skill seed file not found at {FilePath}. Skipping.", filePath);
            return;
        }

        SkillSeedFile seedFile;
        try
        {
            await using var stream = File.OpenRead(filePath);
            seedFile = await JsonSerializer.DeserializeAsync<SkillSeedFile>(stream, JsonReadOptions, cancellationToken)
                       ?? throw new InvalidDataException("Skill seed file deserialized to null.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read or deserialize skill seed file at {FilePath}.", filePath);
            return;
        }

        if (seedFile.Skills.Count == 0)
        {
            _logger.LogInformation("Skill seed file contains no skill definitions. Skipping.");
            return;
        }

        var agent = await EnsureDefaultAgentAsync(cancellationToken);
        var existingSkills = await _agentSkillRepository.GetAllByAgentIdAsync(agent.Id, cancellationToken);
        var existingByName = existingSkills.ToDictionary(s => s.Name, StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        var updated = 0;
        var skipped = 0;

        foreach (var seedDefinition in seedFile.Skills)
        {
            if (string.IsNullOrWhiteSpace(seedDefinition.Name))
            {
                _logger.LogWarning("Skill seed definition has an empty name. Skipping entry.");
                skipped++;
                continue;
            }

            if (existingByName.TryGetValue(seedDefinition.Name, out var existing))
            {
                if (seedDefinition.Version > existing.Version)
                {
                    ApplyDefinitionToSkill(existing, seedDefinition);
                    await _agentSkillRepository.UpdateAsync(existing, cancellationToken);
                    await WritePhrasesAsync(seedDefinition, cancellationToken);
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
            else
            {
                var newSkill = CreateSkillFromDefinition(agent.Id, seedDefinition);
                await _agentSkillRepository.AddAsync(newSkill, cancellationToken);
                await WritePhrasesAsync(seedDefinition, cancellationToken);
                existingByName[seedDefinition.Name] = newSkill;
                inserted++;
            }
        }

        _logger.LogInformation(
            "Skill seed completed: {Total} definitions processed (inserted: {Inserted}, updated: {Updated}, skipped: {Skipped})",
            seedFile.Skills.Count, inserted, updated, skipped);

        await LoadPluginSkillSeedsAsync(agent, existingByName, cancellationToken);
    }

    private async Task LoadPluginSkillSeedsAsync(Agent agent, Dictionary<string, AgentSkill> existingByName, CancellationToken cancellationToken)
    {
        var plugins = await _featurePluginService.GetAllPluginsAsync();

        if (plugins.Count == 0)
        {
            LogEmptyPluginList();
            return;
        }

        foreach (var plugin in plugins)
        {
            if (!plugin.IsInstalled || !plugin.IsEnabled)
                continue;

            var pluginSeedPath = Path.Combine(
                _environment.ContentRootPath,
                FeaturePluginConstants.PluginDirectory,
                plugin.Name,
                FeaturePluginConstants.SkillSeedsFileName);

            if (!File.Exists(pluginSeedPath))
                continue;

            try
            {
                await using var stream = File.OpenRead(pluginSeedPath);
                var skills = await JsonSerializer.DeserializeAsync<List<SkillSeedDefinition>>(stream, JsonReadOptions, cancellationToken);

                if (skills == null || skills.Count == 0)
                    continue;

                var inserted = 0;
                var updated = 0;

                foreach (var definition in skills)
                {
                    if (string.IsNullOrWhiteSpace(definition.Name))
                        continue;

                    if (existingByName.TryGetValue(definition.Name, out var existing))
                    {
                        if (definition.Version > existing.Version)
                        {
                            ApplyDefinitionToSkill(existing, definition);
                            await _agentSkillRepository.UpdateAsync(existing, cancellationToken);
                            await WritePhrasesAsync(definition, cancellationToken);
                            updated++;
                        }
                    }
                    else
                    {
                        var newSkill = CreateSkillFromDefinition(agent.Id, definition);
                        await _agentSkillRepository.AddAsync(newSkill, cancellationToken);
                        await WritePhrasesAsync(definition, cancellationToken);
                        existingByName[definition.Name] = newSkill;
                        inserted++;
                    }
                }

                _logger.LogInformation(
                    "Plugin '{Plugin}' skill seed: {Inserted} inserted, {Updated} updated",
                    plugin.Name, inserted, updated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load skill seeds for plugin '{Plugin}'", plugin.Name);
            }
        }
    }

    private void LogEmptyPluginList()
    {
        var pluginRoot = Path.Combine(_environment.ContentRootPath, FeaturePluginConstants.PluginDirectory);

        var seedFileCount = Directory.Exists(pluginRoot)
            ? Directory.EnumerateFiles(pluginRoot, FeaturePluginConstants.SkillSeedsFileName, SearchOption.AllDirectories).Count()
            : 0;

        if (seedFileCount == 0)
        {
            _logger.LogInformation("No feature plugins registered and no plugin skill seed files found. Skipping plugin skill seeds.");
            return;
        }

        _logger.LogWarning(
            "Feature plugin list is empty although {SeedFileCount} plugin skill seed file(s) exist under {PluginRoot}. " +
            "Plugin skill seeds were skipped — feature plugin initialization may not have run before skill seeding.",
            seedFileCount,
            pluginRoot);
    }

    private async Task<Agent> EnsureDefaultAgentAsync(CancellationToken cancellationToken)
    {
        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent != null)
            return agent;

        agent = new Agent
        {
            Name = DefaultAgentName,
            DisplayName = "Klacksy",
            Description = "Klacksy — the built-in assistant of the Klacks workforce-planning platform",
            IsActive = true,
            IsDefault = true
        };

        await _agentRepository.AddAsync(agent, cancellationToken);
        _logger.LogInformation("Created default agent: {AgentName} ({AgentId})", agent.Name, agent.Id);
        return agent;
    }

    /// <summary>
    /// Mirrors the phrases of a seeded skill into skill_phrase alongside the legacy jsonb columns.
    /// The seed owns its own origin only: replacing the Seed rows leaves the rows a language pack or
    /// an administrator contributed for the same skill untouched, exactly as MergeSynonyms leaves
    /// their language keys inside the jsonb value.
    /// </summary>
    /// <param name="definition">The seed definition whose columns were just persisted</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task WritePhrasesAsync(SkillSeedDefinition definition, CancellationToken cancellationToken)
    {
        await _skillPhraseRepository.ReplaceAllLanguagesAsync(
            SkillPhraseOwnerKinds.Skill,
            definition.Name,
            SkillPhraseKinds.Keyword,
            SkillPhraseSources.Seed,
            ToStoredLanguages(definition.TriggerKeywords),
            cancellationToken: cancellationToken);

        await _skillPhraseRepository.ReplaceAllLanguagesAsync(
            SkillPhraseOwnerKinds.Skill,
            definition.Name,
            SkillPhraseKinds.Synonym,
            SkillPhraseSources.Seed,
            definition.Synonyms,
            cancellationToken: cancellationToken);
    }

    private AgentSkill CreateSkillFromDefinition(Guid agentId, SkillSeedDefinition definition)
    {
        return new AgentSkill
        {
            AgentId = agentId,
            Name = definition.Name,
            Description = definition.Description,
            Category = definition.Category.ToLowerInvariant(),
            ExecutionType = definition.ExecutionType,
            RequiredPermission = definition.RequiredPermissions.Count > 0
                ? string.Join(",", definition.RequiredPermissions)
                : null,
            ParametersJson = SerializeParameters(definition.Parameters),
            HandlerConfig = SerializeHandlerConfig(definition.HandlerConfig),
            HandlerType = definition.HandlerType ?? AgentSkillDefaults.HandlerType,
            TriggerKeywords = SerializeTriggerKeywords(definition.TriggerKeywords),
            Synonyms = definition.Synonyms,
            IsEnabled = definition.IsEnabled,
            AlwaysOn = definition.AlwaysOn,
            PairedApplySkill = NormalizeSkillName(definition.PairedApplySkill),
            Version = definition.Version
        };
    }

    private void ApplyDefinitionToSkill(AgentSkill skill, SkillSeedDefinition definition)
    {
        skill.Description = definition.Description;
        skill.Category = definition.Category.ToLowerInvariant();
        skill.ExecutionType = definition.ExecutionType;
        skill.RequiredPermission = definition.RequiredPermissions.Count > 0
            ? string.Join(",", definition.RequiredPermissions)
            : null;
        skill.ParametersJson = SerializeParameters(definition.Parameters);
        skill.HandlerConfig = SerializeHandlerConfig(definition.HandlerConfig);
        skill.HandlerType = definition.HandlerType ?? AgentSkillDefaults.HandlerType;
        skill.TriggerKeywords = SerializeTriggerKeywords(definition.TriggerKeywords);
        skill.Synonyms = MergeSynonyms(skill.Synonyms, definition.Synonyms);
        skill.IsEnabled = definition.IsEnabled;
        skill.AlwaysOn = definition.AlwaysOn;
        skill.PairedApplySkill = NormalizeSkillName(definition.PairedApplySkill);
        skill.Version = definition.Version;
    }

    /// <summary>
    /// Merges seed synonyms into the stored ones instead of replacing them. The seed file only carries the
    /// core languages, while every other language key is written by a language plugin at install time and
    /// never re-created on startup — a full replacement on a version bump would silently delete them.
    /// Core languages dropped from the definition are removed so they cannot linger forever; non-core keys
    /// are never removed, not even when the definition carries no synonyms at all.
    /// </summary>
    private static Dictionary<string, List<string>> MergeSynonyms(
        Dictionary<string, List<string>>? existing,
        Dictionary<string, List<string>>? definition)
    {
        var merged = existing == null
            ? new Dictionary<string, List<string>>()
            : new Dictionary<string, List<string>>(existing);

        foreach (var coreLanguage in LanguagePluginConstants.CoreLanguages)
        {
            if (definition == null || !definition.ContainsKey(coreLanguage))
            {
                merged.Remove(coreLanguage);
            }
        }

        if (definition != null)
        {
            foreach (var entry in definition)
            {
                merged[entry.Key] = [.. entry.Value];
            }
        }

        return merged;
    }

    private static string? NormalizeSkillName(string? skillName)
    {
        return string.IsNullOrWhiteSpace(skillName) ? null : skillName.Trim();
    }

    private static string SerializeParameters(List<SkillSeedParameter>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return "[]";

        return JsonSerializer.Serialize(parameters, JsonWriteOptions);
    }

    private static string SerializeHandlerConfig(object? handlerConfig)
    {
        if (handlerConfig == null)
            return "{}";

        if (handlerConfig is JsonElement jsonElement)
            return jsonElement.GetRawText();

        if (handlerConfig is JsonNode jsonNode)
            return jsonNode.ToJsonString(JsonWriteOptions);

        return JsonSerializer.Serialize(handlerConfig, JsonWriteOptions);
    }

    private static string SerializeTriggerKeywords(Dictionary<string, List<string>>? keywords) =>
        TriggerKeywordFormat.Write(keywords, JsonWriteOptions);

    /// <summary>
    /// Maps the seed file's language keys onto what skill_phrase stores. The reserved key "mul"
    /// becomes a null language, which is the column's documented meaning of "applies to every
    /// language" - keeping one representation instead of two. It also sorts first when the index text
    /// is built, so internationally used terms survive the tokenizer's 512-token cap on long entries.
    /// "und" is stored verbatim, because "not assigned yet" must stay distinguishable from
    /// "deliberately language-neutral" for the later editorial pass.
    /// </summary>
    /// <param name="groups">Phrases keyed by language tag, or by the reserved mul/und keys</param>
    private static Dictionary<string, List<string>>? ToStoredLanguages(
        Dictionary<string, List<string>>? groups)
    {
        if (groups == null || groups.Count == 0)
        {
            return groups;
        }

        var stored = new Dictionary<string, List<string>>();
        foreach (var (language, phrases) in groups)
        {
            var key = string.Equals(language, SkillPhraseLanguages.Multiple, StringComparison.OrdinalIgnoreCase)
                ? string.Empty
                : language;

            if (stored.TryGetValue(key, out var existing))
            {
                existing.AddRange(phrases);
            }
            else
            {
                stored[key] = [.. phrases];
            }
        }

        return stored;
    }
}
