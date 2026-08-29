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
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Application.Services.Assistant.Learning;

public class LearnedArtifactGenerator : ILearnedArtifactGenerator
{
    private const double GeneratorTemperature = 0.2;
    private const int ClassificationMaxTokens = 700;
    private const int PhraseMaxTokens = 300;
    private const int CapabilityMaxTokens = 2500;

    private static readonly JsonSerializerOptions CapabilityJsonOptions =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };

    private static readonly string ClassificationSystemPrompt =
        "You triage wishes that an assistant could not serve. For each numbered case you receive the user's " +
        "wish, its language, the skills the assistant offered for that wish, and the wider list of skills " +
        "that exist for it. A skill can exist and still not have been offered - that is the most common " +
        "reason a wish was not served, so prefer a skill from the wider list when it fits better. " +
        "Classify every case into exactly one of: " +
        "\"phrase_gap\" - one existing skill, offered or not, does what the user asked for and was simply " +
        "not recognised; name it in \"skill\". " +
        "\"composable\" - no single skill does it, but chaining several of the offered skills would. " +
        "\"needs_code\" - no existing skill and no combination of them can do it. " +
        "Never invent a skill name: \"skill\" must be copied character for character from one of the two " +
        "lists you are given. " +
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

    // The rules are the ones .claude/rules/recipe-authoring.md imposes on hand-written recipes, restated
    // for a model that has never read that file. Two of them are stricter here than for a human author:
    // no question step, because its answer would have to be invented before the execution oracle could
    // run anything, and no step kind beyond search and mutate, because the engine executes no others.
    private static readonly string CapabilitySystemPrompt =
        "You compose new capabilities for an assistant by chaining skills it already has. A capability is " +
        "a recipe: a trigger that recognises the request, and an ordered list of steps that serve it. " +
        "Rules, all mandatory: " +
        "use ONLY the skills you are given, copied character for character; " +
        "every step is \"search\" (reads) or \"mutate\" (writes) - never \"ask\", \"guard\" or \"verify\"; " +
        "a step takes its parameters from \"inject\", whose values are either plain string constants or " +
        "\"$slot\" references to a value an EARLIER step captured with \"capture\": \"field[].id as slot\"; " +
        "never reference a slot nothing captured; " +
        "a \"$slot\" reference is the ENTIRE value and may never be joined to anything else - " +
        "\"$month-01\" is not a reference to $month, it asks for a slot named \"month-01\" that cannot " +
        "exist, and there is no template syntax either, so \"{{month}}-01\" is handed to the skill as " +
        "those literal characters; " +
        "there are NO built-in variables: nothing supplies $today, $now or $currentMonth, and a date, " +
        "month or year is therefore written as a plain literal exactly as the request words it, or the " +
        "parameter is left out when it is not required; " +
        "\"capture\" reads one field out of a LIST a search step returns, so it fits a step that looks " +
        "entities up and never one that returns a single value such as the current time - when in doubt " +
        "omit \"capture\" entirely and give each step its own literals; " +
        "prefer compositions that only read, because those can be verified before activation; " +
        "the trigger has \"allOf\" conditions, and ALL of them must match at once, while the stems INSIDE " +
        "one condition are alternatives of which any one suffices; " +
        "so give two to four conditions, each capturing ONE distinctive concept of this request with at " +
        "most three close wordings of that same word - never one long condition listing every word, " +
        "which fires on any single one of them; " +
        "every stem is at least five characters, is a noun specific to THIS request, and is never a " +
        "generic verb or interface word such as show, list, report, display or their translations, " +
        "because those are how users reach ordinary skills; " +
        "write each stem exactly as it is spelled in the request, not in its dictionary form; " +
        "\"anyWordStart\" only matches where a word BEGINS, which in German compounds it often does not - " +
        "\"reserve\" does not begin a word inside \"kapazitaetsreserve\" - so whenever a stem could sit " +
        "inside a longer compound noun, put it in \"anySubstring\" instead, or the condition can never " +
        "match the very request it came from; " +
        "the name is an English lower-case kebab-case slug; " +
        "\"goal\" is one English sentence, and \"goalTranslations\" gives it in de, en, fr and it. " +
        "Respond ONLY with a JSON object: {\"capabilities\":[{\"name\":\"...\",\"goal\":\"...\"," +
        "\"goalTranslations\":{\"de\":\"...\",\"en\":\"...\",\"fr\":\"...\",\"it\":\"...\"}," +
        "\"trigger\":{\"allOf\":[{\"anyWordStart\":[\"...\"]},{\"anySubstring\":[\"...\"]}]}," +
        "\"steps\":[{\"kind\":\"search\",\"skill\":\"...\",\"inject\":{\"param\":\"literal value\"}}]}]}. " +
        "Add \"capture\":\"items[].id as itemId\" to a step ONLY when a later step needs an id that step " +
        "looked up; most compositions need no capture at all. " +
        "Omit a parameter entirely when the step does not need it; never bind one to an empty string.";

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

    public async Task<IReadOnlyList<LearnedRecipeDraft>> GenerateCapabilitiesAsync(
        SkillLearningClusterContext cluster,
        IReadOnlyList<CapabilityBuildingBlock> blocks,
        IReadOnlyList<string> examples,
        string? failureHint,
        CancellationToken cancellationToken = default)
    {
        if (blocks.Count == 0)
        {
            return [];
        }

        var content = await AskAsync(
            CapabilitySystemPrompt,
            BuildCapabilityPrompt(cluster, blocks, examples, failureHint),
            CapabilityMaxTokens,
            cancellationToken);

        return content == null ? [] : ParseCapabilities(content);
    }

    private static string BuildCapabilityPrompt(
        SkillLearningClusterContext cluster,
        IReadOnlyList<CapabilityBuildingBlock> blocks,
        IReadOnlyList<string> examples,
        string? failureHint)
    {
        var builder = new StringBuilder();
        builder.Append("A user asked for this and no single skill could serve it: ")
            .Append(cluster.IntentExcerpt).Append('\n');
        builder.Append("Language of the request: ").Append(cluster.Locale).Append("\n\n");

        builder.Append("Skills you may use:\n");
        foreach (var block in blocks)
        {
            builder.Append("- ").Append(block.Name).Append(block.ReadOnly ? " (reads)" : " (writes)")
                .Append(": ").Append(block.Description);

            if (block.Parameters.Count > 0)
            {
                builder.Append(" | parameters: ").Append(string.Join(", ", block.Parameters));
            }

            builder.Append('\n');
        }

        if (examples.Count > 0)
        {
            builder.Append("\nExisting capabilities, as the format to follow:\n");
            foreach (var example in examples)
            {
                builder.Append(example).Append('\n');
            }
        }

        if (!string.IsNullOrWhiteSpace(failureHint))
        {
            builder.Append("\nPrevious attempt failed because: ").Append(failureHint).Append('\n');
        }

        builder.Append("\nWrite ").Append(SkillLearningDefaults.CapabilityVariantsPerRound)
            .Append(" different capabilities, ordered best first, each with at most ")
            .Append(SkillLearningDefaults.MaxCapabilityStepCount).Append(" steps.");

        return builder.ToString();
    }

    private IReadOnlyList<LearnedRecipeDraft> ParseCapabilities(string content)
    {
        var json = ExtractJsonObject(content);
        if (json == null)
        {
            _logger.LogWarning("Skill learning capability answer contained no JSON object");
            return [];
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            if (!document.RootElement.TryGetProperty("capabilities", out var capabilities)
                || capabilities.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            return capabilities
                .EnumerateArray()
                .Select(ReadCapability)
                .Where(draft => draft != null)
                .Select(draft => draft!)
                .Take(SkillLearningDefaults.CapabilityVariantsPerRound)
                .ToList();
        }
        catch (JsonException exception)
        {
            _logger.LogWarning(exception, "Skill learning capability answer was not valid JSON");
            return [];
        }
    }

    // Nothing is repaired here. A draft that does not carry a name, a goal, a trigger and steps is
    // dropped rather than completed with guesses, because every field the generator omits is one the
    // validator would then be judging against something no model actually proposed.
    private static LearnedRecipeDraft? ReadCapability(JsonElement element)
    {
        var name = ReadString(element, "name");
        var goal = ReadString(element, "goal");
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(goal))
        {
            return null;
        }

        var trigger = ReadSection<RecipeTrigger>(element, "trigger");
        var steps = ReadSection<List<RecipeStep>>(element, "steps");
        if (trigger == null || steps == null || steps.Count == 0)
        {
            return null;
        }

        var translations = ReadSection<Dictionary<string, string>>(element, "goalTranslations")
            ?? new Dictionary<string, string>();

        return new LearnedRecipeDraft(name!.Trim(), goal!.Trim(), translations, trigger, steps);
    }

    private static T? ReadSection<T>(JsonElement element, string property)
        where T : class
    {
        if (!element.TryGetProperty(property, out var section))
        {
            return null;
        }

        try
        {
            return section.Deserialize<T>(CapabilityJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
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

            builder.Append("Offered skills: ").Append(string.Join(", ", input.CandidateSkills)).Append('\n');

            // Only the difference, never the whole union: repeating the offered names under a second
            // heading is tokens spent to tell a cheap model the same thing twice, and it blurs exactly the
            // distinction this list exists to draw.
            var alsoExisting = input.ReachableSkills
                .Except(input.CandidateSkills, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (alsoExisting.Count > 0)
            {
                builder.Append("Skills that also exist for this wish but were not offered: ")
                    .Append(string.Join(", ", alsoExisting)).Append('\n');
            }

            builder.Append('\n');
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

    // An answer that names a skill outside the REACHABLE list is dropped rather than trusted: the loop
    // would otherwise write a phrase for a skill that does not exist, and the routing oracle can only ever
    // say "not found" about it. Reachable, not offered - that distinction is the whole point. Validating
    // against the offered list made this guard and the "already routed" dismissal in
    // SkillLearningLoop.LearnPhraseAsync test the same set, so every skill the classifier was allowed to
    // name was one the dismissal then threw out. Existence is what has to be checked here; whether the
    // skill is already served is a different question and is answered in the loop.
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

        if (skill != null && !input.ReachableSkills.Contains(skill, StringComparer.OrdinalIgnoreCase)
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
