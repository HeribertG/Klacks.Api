// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Meta-skill that updates an existing skill's description, enabled status, handler steps,
/// trigger keywords, or synonyms, then reloads the skill registry so changes take effect immediately.
/// </summary>
/// <param name="skillName">Name of the skill to update (required)</param>
/// <param name="description">New LLM-facing description (optional)</param>
/// <param name="isEnabled">Enable or disable the skill (optional)</param>
/// <param name="handlerSteps">New handler steps JSON array (optional)</param>
/// <param name="triggerKeywords">New comma-separated trigger keywords (optional)</param>
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

[SkillImplementation("update_agent_skill")]
public class UpdateAgentSkillSkill : BaseSkillImplementation
{
    private readonly IAgentSkillRepository _agentSkillRepository;
    private readonly ISkillPhraseRepository _skillPhraseRepository;
    private readonly SkillRegistryInitializer _skillRegistryInitializer;
    private readonly ISkillCacheService _skillCacheService;

    public UpdateAgentSkillSkill(
        IAgentSkillRepository agentSkillRepository,
        ISkillPhraseRepository skillPhraseRepository,
        SkillRegistryInitializer skillRegistryInitializer,
        ISkillCacheService skillCacheService)
    {
        _agentSkillRepository = agentSkillRepository;
        _skillPhraseRepository = skillPhraseRepository;
        _skillRegistryInitializer = skillRegistryInitializer;
        _skillCacheService = skillCacheService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var skillName = GetRequiredString(parameters, "skillName");
        var description = GetParameter<string>(parameters, "description");
        var isEnabled = GetParameter<bool?>(parameters, "isEnabled");
        var handlerSteps = GetParameter<string>(parameters, "handlerSteps");
        var triggerKeywords = GetParameter<string>(parameters, "triggerKeywords");
        var synonymsJson = GetParameter<string>(parameters, "synonyms");

        var allSkills = await _agentSkillRepository.GetAllEnabledAsync(cancellationToken);
        var skill = allSkills.FirstOrDefault(s => s.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));

        if (skill == null)
        {
            return SkillResult.Error($"Skill '{skillName}' not found.");
        }

        var updated = false;

        if (!string.IsNullOrWhiteSpace(description))
        {
            skill.Description = description;
            updated = true;
        }

        if (isEnabled.HasValue)
        {
            skill.IsEnabled = isEnabled.Value;
            updated = true;
        }

        if (!string.IsNullOrWhiteSpace(handlerSteps))
        {
            skill.HandlerConfig = BuildHandlerConfig(handlerSteps);
            updated = true;
        }

        List<string>? keywordList = null;
        if (!string.IsNullOrWhiteSpace(triggerKeywords))
        {
            keywordList = [.. triggerKeywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

            // Serialized, not concatenated: the previous hand-built literal produced invalid JSON as
            // soon as a phrase contained a quote or a backslash. An administrator typing a phrase
            // like: sag "ja"  silently corrupted the column and the skill lost all its keywords.
            // The admin route carries no language, so the phrases land in the undetermined group.
            skill.TriggerKeywords = TriggerKeywordFormat.Write(
                new Dictionary<string, List<string>> { [SkillPhraseLanguages.Undetermined] = keywordList });
            updated = true;
        }

        Dictionary<string, List<string>>? synonyms = null;
        if (!string.IsNullOrWhiteSpace(synonymsJson))
        {
            var parseResult = ParseSynonyms(synonymsJson);
            if (parseResult.Error != null)
            {
                return SkillResult.Error(parseResult.Error);
            }
            synonyms = parseResult.Value;
            skill.Synonyms = synonyms;
            updated = true;
        }

        if (!updated)
        {
            return SkillResult.Error("No fields to update were provided.");
        }

        skill.Version += 1;

        await _agentSkillRepository.UpdateAsync(skill, cancellationToken);
        await WritePhrasesAsync(skill.Name, keywordList, synonyms, cancellationToken);
        _skillCacheService.InvalidateCache();
        await _skillRegistryInitializer.InitializeAsync(cancellationToken);

        return SkillResult.SuccessResult(
            new { SkillName = skill.Name, Version = skill.Version },
            $"Skill '{skill.Name}' updated to version {skill.Version}.");
    }

    /// <summary>
    /// Mirrors an administrative edit into skill_phrase. Only the parts the caller actually sent are
    /// rewritten; a null argument means the caller left that column alone.
    /// The replacement covers every origin because the jsonb column it mirrors is overwritten
    /// wholesale here: keeping seeded or language pack rows would both contradict the stored jsonb
    /// value and collide with the unique index, which does not include the origin.
    /// </summary>
    /// <param name="skillName">Name of the edited skill</param>
    /// <param name="keywords">New trigger keywords, or null if they were not part of the edit</param>
    /// <param name="synonyms">New synonyms per language, or null if they were not part of the edit</param>
    /// <param name="cancellationToken">Cancellation token</param>
    private async Task WritePhrasesAsync(
        string skillName,
        IReadOnlyList<string>? keywords,
        IReadOnlyDictionary<string, List<string>>? synonyms,
        CancellationToken cancellationToken)
    {
        if (keywords != null)
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
        }

        if (synonyms != null)
        {
            await _skillPhraseRepository.ReplaceAllLanguagesAsync(
                SkillPhraseOwnerKinds.Skill,
                skillName,
                SkillPhraseKinds.Synonym,
                SkillPhraseSources.Admin,
                synonyms,
                SkillPhraseReplaceScope.AllSourcesOfOwner,
                cancellationToken);
        }
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
