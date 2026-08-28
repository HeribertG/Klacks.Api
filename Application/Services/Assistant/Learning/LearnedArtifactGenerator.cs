// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The only part of the learning loop that talks to a language model. Which model that is belongs to the
/// installation, not to this code: it asks the shared cheapest-model resolver and works with whatever
/// provider comes back, so nothing here may assume a particular vendor, prompt dialect or tool syntax.
/// Two prompts, both answered as strict JSON. Classification runs once per run over every claimed cluster
/// at once, because a run handles a handful of clusters and one request each would multiply its cost
/// without improving the answer. Phrase generation runs once per round and per cluster, and is told what
/// the previous round produced and why the routing oracle rejected it.
/// The model never sees more than the stored excerpt of an utterance, and never a user id.
/// </summary>
/// <param name="modelResolver">Resolves the cheapest enabled model together with its provider</param>
/// <param name="logger">Reports unusable answers; a failed generation is a skipped cluster, never a failed run</param>

using System.Text;
using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class LearnedArtifactGenerator : ILearnedArtifactGenerator
{
    private const double GeneratorTemperature = 0.2;
    private const int ClassificationMaxTokens = 700;
    private const int PhraseMaxTokens = 300;

    private static readonly string ClassificationSystemPrompt =
        "You triage wishes that an assistant could not serve. For each numbered case you receive the user's " +
        "wish, its language, and the skills the assistant's search currently offers for that wish. " +
        "Classify every case into exactly one of: " +
        "\"phrase_gap\" - one of the offered skills, or another skill you are told the user expected, does " +
        "what the user asked for and was simply not recognised; name it in \"skill\". " +
        "\"composable\" - no single skill does it, but chaining several of the offered skills would. " +
        "\"needs_code\" - no combination of the offered skills can do it. " +
        "Never invent a skill name: \"skill\" must be copied character for character from the offered list. " +
        "Keep \"reason\" to one short English sentence. " +
        "Respond ONLY with a JSON object: {\"cases\":[{\"index\":0,\"kind\":\"phrase_gap\",\"skill\":\"...\",\"reason\":\"...\"}]}.";

    private static readonly string PhraseSystemPrompt =
        "You write trigger phrases for a skill of an assistant. The assistant finds skills by semantic " +
        "search over a text built from the skill's name, description and phrases, so a good phrase is how a " +
        "real user would ask for this skill in the given language. " +
        "Rules: write in the requested language and no other; each phrase is a short noun or verb phrase, " +
        "not a full sentence and not a question; no punctuation at the end; do not repeat a phrase that " +
        "already exists; do not describe what the skill does, write what a user would say. " +
        "Respond ONLY with a JSON object: {\"phrases\":[\"...\",\"...\",\"...\"]}.";

    private readonly ICheapestModelResolver _modelResolver;
    private readonly ILogger<LearnedArtifactGenerator> _logger;

    public LearnedArtifactGenerator(
        ICheapestModelResolver modelResolver,
        ILogger<LearnedArtifactGenerator> logger)
    {
        _modelResolver = modelResolver;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SkillLearningClassification>> ClassifyAsync(
        IReadOnlyList<SkillLearningTriageInput> inputs,
        CancellationToken cancellationToken = default)
    {
        if (inputs.Count == 0)
        {
            return [];
        }

        var content = await AskAsync(
            ClassificationSystemPrompt, BuildClassificationPrompt(inputs), ClassificationMaxTokens, cancellationToken);

        return content == null ? [] : ParseClassifications(content, inputs);
    }

    public async Task<IReadOnlyList<string>> GeneratePhrasesAsync(
        SkillLearningClusterContext cluster,
        string targetSkill,
        string targetDescription,
        IReadOnlyList<string> existingPhrases,
        string? failureHint,
        CancellationToken cancellationToken = default)
    {
        var content = await AskAsync(
            PhraseSystemPrompt,
            BuildPhrasePrompt(cluster, targetSkill, targetDescription, existingPhrases, failureHint),
            PhraseMaxTokens,
            cancellationToken);

        return content == null ? [] : ParsePhrases(content);
    }

    private static string BuildClassificationPrompt(IReadOnlyList<SkillLearningTriageInput> inputs)
    {
        var builder = new StringBuilder();

        for (var index = 0; index < inputs.Count; index++)
        {
            var input = inputs[index];
            builder.Append("Case ").Append(index).Append('\n');
            builder.Append("Wish: ").Append(input.Cluster.IntentExcerpt).Append('\n');
            builder.Append("Language: ").Append(input.Cluster.Locale).Append('\n');

            if (!string.IsNullOrWhiteSpace(input.Cluster.ExpectedSkill))
            {
                builder.Append("Skill the user said was expected: ").Append(input.Cluster.ExpectedSkill).Append('\n');
            }

            if (!string.IsNullOrWhiteSpace(input.Cluster.ChosenSkill))
            {
                builder.Append("Skill the assistant used instead: ").Append(input.Cluster.ChosenSkill).Append('\n');
            }

            builder.Append("Offered skills: ").Append(string.Join(", ", input.CandidateSkills)).Append("\n\n");
        }

        builder.Append("Classify every case.");
        return builder.ToString();
    }

    private static string BuildPhrasePrompt(
        SkillLearningClusterContext cluster,
        string targetSkill,
        string targetDescription,
        IReadOnlyList<string> existingPhrases,
        string? failureHint)
    {
        var builder = new StringBuilder();
        builder.Append("Skill name: ").Append(targetSkill).Append('\n');
        builder.Append("Skill description: ").Append(targetDescription).Append('\n');
        builder.Append("Language of the phrases: ").Append(cluster.Locale).Append('\n');
        builder.Append("A user asked for this and was not understood: ").Append(cluster.IntentExcerpt).Append('\n');

        if (existingPhrases.Count > 0)
        {
            builder.Append("Phrases that already exist: ").Append(string.Join(", ", existingPhrases)).Append('\n');
        }

        if (!string.IsNullOrWhiteSpace(failureHint))
        {
            builder.Append("Previous attempt failed because: ").Append(failureHint).Append('\n');
        }

        builder.Append("Write ").Append(SkillLearningDefaults.PhraseVariantsPerRound)
            .Append(" different phrases, ordered best first.");

        return builder.ToString();
    }

    private async Task<string?> AskAsync(
        string systemPrompt, string userMessage, int maxTokens, CancellationToken cancellationToken)
    {
        var (model, provider) = await _modelResolver.ResolveAsync(cancellationToken);
        if (model == null || provider == null)
        {
            _logger.LogInformation("Skill learning generation skipped: no enabled model or provider");
            return null;
        }

        var request = new LLMProviderRequest
        {
            Message = userMessage,
            SystemPrompt = systemPrompt,
            ModelId = model.ApiModelId,
            ConversationHistory = [],
            AvailableFunctions = [],
            Temperature = GeneratorTemperature,
            MaxTokens = maxTokens,
            SupportedParameters = model.SupportedParameters,
            CostPerInputToken = model.CostPerInputToken,
            CostPerOutputToken = model.CostPerOutputToken
        };

        var response = await provider.ProcessAsync(request, cancellationToken);
        if (!response.Success || string.IsNullOrWhiteSpace(response.Content))
        {
            _logger.LogWarning("Skill learning generation returned no content");
            return null;
        }

        return response.Content;
    }

    private IReadOnlyList<SkillLearningClassification> ParseClassifications(
        string content, IReadOnlyList<SkillLearningTriageInput> inputs)
    {
        var json = ExtractJsonObject(content);
        if (json == null)
        {
            _logger.LogWarning("Skill learning classification answer contained no JSON object");
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("cases", out var cases)
                || cases.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var results = new List<SkillLearningClassification>();

            foreach (var element in cases.EnumerateArray())
            {
                var classification = ReadClassification(element, inputs);
                if (classification != null)
                {
                    results.Add(classification);
                }
            }

            return results;
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Skill learning classification answer was not valid JSON");
            return [];
        }
    }

    // An answer that names a skill outside the offered list is dropped rather than trusted: the loop would
    // otherwise write a phrase for a skill that does not exist, and the routing oracle can only ever say
    // "not found" about it.
    private static SkillLearningClassification? ReadClassification(
        JsonElement element, IReadOnlyList<SkillLearningTriageInput> inputs)
    {
        if (!element.TryGetProperty("index", out var indexElement)
            || !indexElement.TryGetInt32(out var index)
            || index < 0
            || index >= inputs.Count)
        {
            return null;
        }

        var kind = ReadString(element, "kind");
        if (!SkillLearningClassifications.IsKnown(kind))
        {
            return null;
        }

        var input = inputs[index];
        var skill = ReadString(element, "skill");

        if (skill != null && !input.CandidateSkills.Contains(skill, StringComparer.OrdinalIgnoreCase)
            && !string.Equals(skill, input.Cluster.ExpectedSkill, StringComparison.OrdinalIgnoreCase))
        {
            skill = null;
        }

        return new SkillLearningClassification(input.Cluster.ClusterId, kind!, skill, ReadString(element, "reason"));
    }

    private IReadOnlyList<string> ParsePhrases(string content)
    {
        var json = ExtractJsonObject(content);
        if (json == null)
        {
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("phrases", out var phrases)
                || phrases.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return phrases
                .EnumerateArray()
                .Select(element => element.ValueKind == JsonValueKind.String ? element.GetString() : null)
                .Where(phrase => !string.IsNullOrWhiteSpace(phrase))
                .Select(phrase => phrase!.Trim())
                .Where(phrase => phrase.Length >= SkillLearningDefaults.MinPhraseLength
                    && phrase.Length <= SkillLearningDefaults.MaxPhraseLength)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(SkillLearningDefaults.PhraseVariantsPerRound)
                .ToList();
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Skill learning phrase answer was not valid JSON");
            return [];
        }
    }

    private static string? ReadString(JsonElement element, string property) =>
        element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()?.Trim()
            : null;

    private static string? ExtractJsonObject(string content)
    {
        var start = content.IndexOf('{');
        var end = content.LastIndexOf('}');
        return start < 0 || end <= start ? null : content.Substring(start, end - start + 1);
    }
}
