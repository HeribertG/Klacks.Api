// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Static catalog of guided MCP workflow prompts: lists the available prompt templates and
/// renders a prompt into a user message with the provided argument values interpolated.
/// </summary>
/// <param name="name">Name of the prompt template to render</param>
/// <param name="arguments">Argument values keyed by argument name; required arguments must be present and non-empty</param>

using System.Text.Json;
using Klacks.Api.Domain.Constants;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;

namespace Klacks.Api.Presentation.Mcp;

public class McpPromptCatalog : IMcpPromptCatalog
{
    public const string OnboardEmployeePromptName = "onboard_employee";
    public const string PlanScheduleWeekPromptName = "plan_schedule_week";
    public const string CoverAbsenceGuidedPromptName = "cover_absence_guided";

    public const string FirstNameArgument = "firstName";
    public const string LastNameArgument = "lastName";
    public const string EmailArgument = "email";
    public const string GroupNameArgument = "groupName";
    public const string WeekStartArgument = "weekStart";
    public const string EmployeeNameArgument = "employeeName";
    public const string FromDateArgument = "fromDate";
    public const string UntilDateArgument = "untilDate";

    private const string CreateEmployeeSkill = "create_employee";
    private const string AddClientEmailSkill = "add_client_email";
    private const string ListContractsSkill = "list_contracts";
    private const string AssignContractByNameSkill = "assign_contract_by_name";
    private const string ListGroupsSkill = "list_groups";
    private const string ReadScheduleStateSkill = "read_schedule_state";
    private const string DetectConflictsSkill = "detect_conflicts";
    private const string PlaceWorkSkill = "place_work";
    private const string SearchEmployeesSkill = "search_employees";
    private const string ListAbsenceTypesSkill = "list_absence_types";
    private const string CoverAbsenceSkill = "cover_absence";
    private const string FindReplacementSkill = "find_replacement";
    private const string AcceptScenarioSkill = "accept_scenario";

    private static readonly string ConfirmationHint =
        $"Whenever a tool responds with a confirmation request instead of a result, call " +
        $"'{AutonomyDefaults.ConfirmPendingActionSkillName}' with the parameter " +
        $"'{AutonomyDefaults.ConfirmationTokenParameter}' set to the provided token to execute the held action.";

    private static readonly IReadOnlyList<PromptDefinition> Definitions =
    [
        new PromptDefinition(
            new Prompt
            {
                Name = OnboardEmployeePromptName,
                Title = "Onboard a new employee",
                Description = "Guided workflow that creates a new employee, adds the contact email and assigns an employment contract.",
                Arguments =
                [
                    new PromptArgument { Name = FirstNameArgument, Description = "First name of the new employee", Required = true },
                    new PromptArgument { Name = LastNameArgument, Description = "Last name of the new employee", Required = true },
                    new PromptArgument { Name = EmailArgument, Description = "Email address of the new employee", Required = false }
                ]
            },
            BuildOnboardEmployeeInstruction),
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

    public IList<Prompt> ListPrompts()
    {
        return Definitions.Select(definition => definition.Prompt).ToList();
    }

    public GetPromptResult GetPrompt(string name, IDictionary<string, JsonElement>? arguments)
    {
        var definition = Definitions.FirstOrDefault(candidate =>
                string.Equals(candidate.Prompt.Name, name, StringComparison.Ordinal))
            ?? throw new McpProtocolException($"Unknown prompt: '{name}'.", McpErrorCode.InvalidParams);

        var values = ResolveArguments(definition.Prompt, arguments);

        return new GetPromptResult
        {
            Description = definition.Prompt.Description,
            Messages =
            [
                new PromptMessage
                {
                    Role = Role.User,
                    Content = new TextContentBlock { Text = definition.BuildInstruction(values) }
                }
            ]
        };
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

    private static string BuildOnboardEmployeeInstruction(IReadOnlyDictionary<string, string> values)
    {
        var firstName = values[FirstNameArgument];
        var lastName = values[LastNameArgument];
        var emailStep = values.TryGetValue(EmailArgument, out var email)
            ? $"2. Add the email address '{email}' to the new employee with '{AddClientEmailSkill}' " +
              $"(firstName '{firstName}', lastName '{lastName}'), unless it was already stored in step 1."
            : $"2. Ask for the employee's email address and add it with '{AddClientEmailSkill}' " +
              $"(firstName '{firstName}', lastName '{lastName}'); without an email there is no email-based planning communication.";

        return $"Onboard the new employee {firstName} {lastName} step by step:\n" +
               $"1. Create the employee with '{CreateEmployeeSkill}' (firstName '{firstName}', lastName '{lastName}'). " +
               $"Ask the user for the missing required data first, especially gender and the start date (memberSince).\n" +
               $"{emailStep}\n" +
               $"3. List the available contract templates with '{ListContractsSkill}' and let the user pick one.\n" +
               $"4. Assign the chosen contract with '{AssignContractByNameSkill}' " +
               $"(firstName '{firstName}', lastName '{lastName}', contractName from step 3, fromDate = the employee's start date).\n" +
               $"{ConfirmationHint}";
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
