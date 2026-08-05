// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Meta-skill that creates a new UiAction skill at runtime, persists it to the database,
/// and reloads the skill registry so the new skill is immediately available.
/// </summary>
/// <param name="name">Skill name in snake_case (required)</param>
/// <param name="description">LLM-facing description (required)</param>
/// <param name="category">Skill category (optional, default: Action)</param>
/// <param name="handlerSteps">JSON array of handler steps for UiAction (optional)</param>
/// <param name="triggerKeywords">Comma-separated keywords that trigger this skill (optional)</param>
/// <param name="synonyms">JSON object mapping language codes to synonym lists, e.g. {"de":["wort1"],"en":["word1"]} (optional)</param>

using System.Text.Json;
using Klacks.Api.Application.Services.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills.Meta;

[SkillImplementation("create_agent_skill")]
public class CreateAgentSkillSkill : BaseSkillImplementation
{
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly IAgentRepository _agentRepository;
    private readonly ISkillPhraseRepository _skillPhraseRepository;
    private readonly ISkillCatalogRefresher _skillCatalogRefresher;

    public CreateAgentSkillSkill(
        IAgentSkillRepository agentSkillRepository,
        IAgentRepository agentRepository,
        ISkillPhraseRepository skillPhraseRepository,
        ISkillCatalogRefresher skillCatalogRefresher)
    {
        _agentSkillRepository = agentSkillRepository;
        _agentRepository = agentRepository;
        _skillPhraseRepository = skillPhraseRepository;
        _skillCatalogRefresher = skillCatalogRefresher;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var name = GetRequiredString(parameters, "name");
        var description = GetRequiredString(parameters, "description");
        var category = GetParameter<string>(parameters, "category") ?? "Action";
        var handlerSteps = GetParameter<string>(parameters, "handlerSteps");
        var triggerKeywords = GetParameter<string>(parameters, "triggerKeywords");
        var synonymsJson = GetParameter<string>(parameters, "synonyms");

        name = name.Trim().ToLowerInvariant().Replace(' ', '_');

        if (!IsValidSnakeCase(name))
        {
            return SkillResult.Error($"Skill name '{name}' is invalid. Use snake_case with letters, digits, and underscores only.");
        }

        var existing = await _agentSkillRepository.GetAllEnabledAsync(cancellationToken);
        if (existing.Any(s => s.Name.Equals(name, StringComparison.OrdinalIgnoreCase)))
        {
            return SkillResult.Error($"A skill with the name '{name}' already exists. Use update_agent_skill to modify it.");
        }

        var agent = await _agentRepository.GetDefaultAgentAsync(cancellationToken);
        if (agent == null)
        {
            return SkillResult.Error("No default agent found. Cannot create skill.");
        }

        var handlerConfig = "{}";
        if (!string.IsNullOrWhiteSpace(handlerSteps))
        {
            handlerConfig = BuildHandlerConfig(handlerSteps);
        }

        List<string> keywordList = [];
        if (!string.IsNullOrWhiteSpace(triggerKeywords))
        {
            keywordList = [.. triggerKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];
        }

        // Serialized rather than concatenated: a hand-built literal breaks on any phrase containing a
        // quote or a backslash. The admin route carries no language, so the phrases land in the
        // undetermined group and stay searchable in every language until someone classifies them.
        var keywordsJson = keywordList.Count == 0
            ? "{}"
            : TriggerKeywordFormat.Write(
                new Dictionary<string, List<string>> { [SkillPhraseLanguages.Undetermined] = keywordList });

        Dictionary<string, List<string>>? synonyms = null;
        if (!string.IsNullOrWhiteSpace(synonymsJson))
        {
            var parseResult = ParseSynonyms(synonymsJson);
            if (parseResult.Error != null)
            {
                return SkillResult.Error(parseResult.Error);
            }
            synonyms = parseResult.Value;
        }

        var agentSkill = new AgentSkill
        {
            AgentId = agent.Id,
            Name = name,
            Description = description,
            Category = category,
            ExecutionType = LlmExecutionTypes.UiAction,
            HandlerConfig = handlerConfig,
            TriggerKeywords = keywordsJson,
            Synonyms = synonyms,
            IsEnabled = true,
            Version = 1
        };

        await _agentSkillRepository.AddAsync(agentSkill, cancellationToken);
        await WritePhrasesAsync(name, keywordList, synonyms, cancellationToken);
        await _skillCatalogRefresher.RefreshAsync($"creating skill '{name}'", cancellationToken);

        return SkillResult.SuccessResult(
            new { SkillName = name },
            $"Skill '{name}' created and immediately available.");
    }

    /// <summary>
    /// Writes the phrases of the newly created skill into skill_phrase next to the jsonb columns.
    /// The replacement covers every origin so that leftover rows of an earlier, removed skill of the
    /// same name cannot survive under the new one - the unique index carries no origin column, and a
    /// leftover row would otherwise collide with an identical new phrase.
    /// </summary>
    /// <param name="skillName">Name of the created skill</param>
    /// <param name="keywords">Trigger keywords of the new skill, possibly empty</param>
    /// <param name="synonyms">Synonyms per language of the new skill, or null</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task WritePhrasesAsync(
        string skillName,
        IReadOnlyList<string> keywords,
        IReadOnlyDictionary<string, List<string>>? synonyms,
        CancellationToken cancellationToken)
    {
        await _skillPhraseRepository.ReplaceForLanguageAsync(
            SkillPhraseOwnerKinds.Skill,
            skillName,
            SkillPhraseKinds.Keyword,
            SkillPhraseSources.Admin,
            SkillPhraseLanguages.Undetermined,
            keywords,
            SkillPhraseReplaceScope.AllSourcesOfOwner,
            cancellationToken);

        await _skillPhraseRepository.ReplaceAllLanguagesAsync(
            SkillPhraseOwnerKinds.Skill,
            skillName,
            SkillPhraseKinds.Synonym,
            SkillPhraseSources.Admin,
            synonyms,
            SkillPhraseReplaceScope.AllSourcesOfOwner,
            cancellationToken);
    }

    private static bool IsValidSnakeCase(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.All(c => char.IsLetterOrDigit(c) || c == '_');
    }

    private static string BuildHandlerConfig(string handlerSteps)
    {
        try
        {
            using var doc = JsonDocument.Parse(handlerSteps);
            return JsonSerializer.Serialize(new { steps = doc.RootElement });
        }
        catch
        {
            return $"{{\"steps\":{handlerSteps}}}";
        }
    }

    private static (Dictionary<string, List<string>>? Value, string? Error) ParseSynonyms(string json)
    {
        try
        {
            var result = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(json);
            if (result == null)
            {
                return (null, "Synonyms JSON parsed to null. Expected an object like {\"de\":[\"wort1\"],\"en\":[\"word1\"]}.");
            }
            return (result, null);
        }
        catch (JsonException ex)
        {
            return (null, $"Invalid synonyms JSON: {ex.Message}");
        }
    }
}
