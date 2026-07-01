// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Catalog of guided MCP workflow prompts: a small set of hand-authored multi-skill workflows that
/// have no equivalent recipe yet, plus one auto-generated prompt per enabled AgentRecipe. Recipes are
/// the same guided ask/search/mutate flows Klacksy's chat engine executes internally, so external MCP
/// clients get the exact same, always-current step sequence without anyone hand-maintaining a copy.
/// </summary>
/// <param name="name">Name of the prompt template to render</param>
/// <param name="arguments">Argument values keyed by argument name, used to pre-fill known slots; nothing is required for recipe-derived prompts</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Assistant.Recipes;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Klacks.Api.Presentation.Mcp;

public class McpPromptCatalog : IMcpPromptCatalog
{
    public const string PlanScheduleWeekPromptName = "plan_schedule_week";
    public const string CoverAbsenceGuidedPromptName = "cover_absence_guided";

    public const string GroupNameArgument = "groupName";
    public const string WeekStartArgument = "weekStart";
    public const string EmployeeNameArgument = "employeeName";
    public const string FromDateArgument = "fromDate";
    public const string UntilDateArgument = "untilDate";

    private const string ListGroupsSkill = "list_groups";
    private const string ReadScheduleStateSkill = "read_schedule_state";
    private const string DetectConflictsSkill = "detect_conflicts";
    private const string PlaceWorkSkill = "place_work";
    private const string SearchEmployeesSkill = "search_employees";
    private const string ListAbsenceTypesSkill = "list_absence_types";
    private const string CoverAbsenceSkill = "cover_absence";
    private const string FindReplacementSkill = "find_replacement";
    private const string AcceptScenarioSkill = "accept_scenario";

    private static readonly JsonSerializerOptions StepsJsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static readonly string ConfirmationHint =
        $"Whenever a tool responds with a confirmation request instead of a result, call " +
        $"'{AutonomyDefaults.ConfirmPendingActionSkillName}' with the parameter " +
        $"'{AutonomyDefaults.ConfirmationTokenParameter}' set to the provided token to execute the held action.";

    private static readonly IReadOnlyList<PromptDefinition> StaticDefinitions =
    [
        new PromptDefinition(
            new Prompt
            {
                Name = PlanScheduleWeekPromptName,
                Title = "Plan a schedule week",
                Description = "Guided workflow that reads the current schedule of a group, detects rule conflicts and fills gaps by placing work entries.",
                Arguments =
                [
                    new PromptArgument { Name = GroupNameArgument, Description = "Name of the group / planning blade to plan", Required = true },
                    new PromptArgument { Name = WeekStartArgument, Description = "First day of the week to plan (YYYY-MM-DD)", Required = true }
                ]
            },
            BuildPlanScheduleWeekInstruction),
        new PromptDefinition(
            new Prompt
            {
                Name = CoverAbsenceGuidedPromptName,
                Title = "Cover an absence",
                Description = "Guided workflow that records an employee absence and finds rule-compliant replacements for the affected shifts.",
                Arguments =
                [
                    new PromptArgument { Name = EmployeeNameArgument, Description = "Name of the absent employee", Required = true },
                    new PromptArgument { Name = FromDateArgument, Description = "First day of the absence (YYYY-MM-DD)", Required = true },
                    new PromptArgument { Name = UntilDateArgument, Description = "Last day of the absence (YYYY-MM-DD)", Required = true }
                ]
            },
            BuildCoverAbsenceGuidedInstruction)
    ];

    private readonly IAgentRecipeRepository _recipeRepository;
    private readonly ILogger<McpPromptCatalog> _logger;

    public McpPromptCatalog(IAgentRecipeRepository recipeRepository, ILogger<McpPromptCatalog> logger)
    {
        _recipeRepository = recipeRepository;
        _logger = logger;
    }

    public async Task<IList<Prompt>> ListPromptsAsync(CancellationToken cancellationToken = default)
    {
        var recipes = await _recipeRepository.GetAllEnabledAsync(cancellationToken);

        var prompts = StaticDefinitions.Select(definition => definition.Prompt).ToList();
        prompts.AddRange(recipes
            .Select(recipe => (Recipe: recipe, Steps: ParseSteps(recipe)))
            .Where(parsed => parsed.Steps.Count > 0)
            .Select(parsed => ToPrompt(parsed.Recipe, parsed.Steps)));

        return prompts;
    }

    public async Task<GetPromptResult> GetPromptAsync(
        string name,
        IDictionary<string, JsonElement>? arguments,
        CancellationToken cancellationToken = default)
    {
        var staticDefinition = StaticDefinitions.FirstOrDefault(candidate =>
            string.Equals(candidate.Prompt.Name, name, StringComparison.Ordinal));
        if (staticDefinition != null)
        {
            var values = ResolveArguments(staticDefinition.Prompt, arguments);
            return new GetPromptResult
            {
                Description = staticDefinition.Prompt.Description,
                Messages = [BuildUserMessage(staticDefinition.BuildInstruction(values))]
            };
        }

        var recipe = await _recipeRepository.GetByNameAsync(name, cancellationToken);
        var steps = recipe == null || !recipe.IsEnabled ? [] : ParseSteps(recipe);
        if (recipe == null || !recipe.IsEnabled || steps.Count == 0)
        {
            throw new McpProtocolException($"Unknown prompt: '{name}'.", McpErrorCode.InvalidParams);
        }

        var prompt = ToPrompt(recipe, steps);
        var recipeValues = ResolveArguments(prompt, arguments);

        return new GetPromptResult
        {
            Description = prompt.Description,
            Messages = [BuildUserMessage(BuildRecipeInstruction(recipe, steps, recipeValues))]
        };
    }

    private List<RecipeStep> ParseSteps(AgentRecipe recipe)
    {
        if (string.IsNullOrWhiteSpace(recipe.StepsJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<RecipeStep>>(recipe.StepsJson, StepsJsonOptions) ?? [];
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Recipe '{Recipe}' has invalid StepsJson; excluding it from the MCP prompt catalog.", recipe.Name);
            return [];
        }
    }

    private static PromptMessage BuildUserMessage(string text) => new()
    {
        Role = Role.User,
        Content = new TextContentBlock { Text = text }
    };

    private static Prompt ToPrompt(AgentRecipe recipe, List<RecipeStep> steps)
    {
        var arguments = steps
            .Where(step => string.Equals(step.Kind, RecipeStepKinds.Ask, StringComparison.OrdinalIgnoreCase)
                && !string.IsNullOrWhiteSpace(step.Slot))
            .GroupBy(step => step.Slot!, StringComparer.OrdinalIgnoreCase)
            .Select(group => new PromptArgument
            {
                Name = group.Key,
                Description = group.First().Description ?? group.First().Prompt ?? group.Key,
                Required = false
            })
            .ToList();

        return new Prompt
        {
            Name = recipe.Name,
            Title = Humanize(recipe.Name),
            Description = recipe.Goal,
            Arguments = arguments
        };
    }

    private static string Humanize(string slug)
    {
        return string.Join(' ', slug.Split('-').Select(word =>
            word.Length == 0 ? word : char.ToUpperInvariant(word[0]) + word[1..]));
    }

    private static string BuildRecipeInstruction(
        AgentRecipe recipe,
        IReadOnlyList<RecipeStep> steps,
        IReadOnlyDictionary<string, string> values)
    {
        var lines = new List<string> { $"{recipe.Goal} Follow these steps in order:" };
        var stepNumber = 1;

        foreach (var step in steps)
        {
            if (string.Equals(step.Kind, RecipeStepKinds.Ask, StringComparison.OrdinalIgnoreCase))
            {
                lines.Add(!string.IsNullOrWhiteSpace(step.Slot) && values.TryGetValue(step.Slot, out var known)
                    ? $"{stepNumber}. {step.Description ?? step.Slot} is already known: '{known}'. Do not ask for it again."
                    : $"{stepNumber}. {step.Prompt}");
            }
            else if (!string.IsNullOrWhiteSpace(step.Skill))
            {
                lines.Add($"{stepNumber}. Call '{step.Skill}'.{(string.IsNullOrWhiteSpace(step.Note) ? string.Empty : " " + step.Note)}");
            }
            else
            {
                continue;
            }

            stepNumber++;
        }

        lines.Add(ConfirmationHint);

        return string.Join("\n", lines);
    }

    private static IReadOnlyDictionary<string, string> ResolveArguments(
        Prompt prompt,
        IDictionary<string, JsonElement>? arguments)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var argument in prompt.Arguments ?? [])
        {
            var value = ReadArgumentValue(arguments, argument.Name);
            if (string.IsNullOrWhiteSpace(value))
            {
                if (argument.Required == true)
                {
                    throw new McpProtocolException(
                        $"Missing required argument '{argument.Name}' for prompt '{prompt.Name}'.",
                        McpErrorCode.InvalidParams);
                }

                continue;
            }

            values[argument.Name] = value;
        }

        return values;
    }

    private static string? ReadArgumentValue(IDictionary<string, JsonElement>? arguments, string name)
    {
        if (arguments == null || !arguments.TryGetValue(name, out var element))
        {
            return null;
        }

        return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
    }

    private static string BuildPlanScheduleWeekInstruction(IReadOnlyDictionary<string, string> values)
    {
        var groupName = values[GroupNameArgument];
        var weekStart = values[WeekStartArgument];

        return $"Plan the schedule week starting on {weekStart} for the group '{groupName}' step by step:\n" +
               $"1. Resolve the group ID for '{groupName}' with '{ListGroupsSkill}'.\n" +
               $"2. Read the current plan with '{ReadScheduleStateSkill}' (groupId from step 1, " +
               $"fromDate '{weekStart}', untilDate = '{weekStart}' plus six days) to see assignments and gaps.\n" +
               $"3. Check the plan with '{DetectConflictsSkill}' for the same group and period and report any violations.\n" +
               $"4. Fill open slots one by one with '{PlaceWorkSkill}' (clientId, shiftId, date), " +
               $"then re-run '{DetectConflictsSkill}' after each placement and repeat until the week is covered without errors.\n" +
               $"{ConfirmationHint}";
    }

    private static string BuildCoverAbsenceGuidedInstruction(IReadOnlyDictionary<string, string> values)
    {
        var employeeName = values[EmployeeNameArgument];
        var fromDate = values[FromDateArgument];
        var untilDate = values[UntilDateArgument];

        return $"Cover the absence of {employeeName} from {fromDate} until {untilDate} step by step:\n" +
               $"1. Find the employee with '{SearchEmployeesSkill}' (searchTerm '{employeeName}') and note the client ID.\n" +
               $"2. Resolve the absence type with '{ListAbsenceTypesSkill}' (ask the user which type applies, e.g. sick or vacation).\n" +
               $"3. For each day from {fromDate} to {untilDate}, record the absence and get cover proposals with " +
               $"'{CoverAbsenceSkill}' (clientId, date, groupId, absenceId); the result is an isolated scenario that does not touch the real plan.\n" +
               $"4. For affected shifts without a proposal, rank candidates with '{FindReplacementSkill}' (shiftId, date, groupId).\n" +
               $"5. Present the scenario to the user and apply it with '{AcceptScenarioSkill}' only after explicit approval.\n" +
               $"{ConfirmationHint}";
    }

    private sealed record PromptDefinition(
        Prompt Prompt,
        Func<IReadOnlyDictionary<string, string>, string> BuildInstruction);
}
