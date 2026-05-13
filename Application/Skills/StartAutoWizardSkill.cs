// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that kicks off the AutoWizard chain (Wizard 1 Planner + Wizard 2 Harmonizer + Wizard 3 Holistic
/// Harmonizer) for a given group and period. When agentIds / shiftIds are omitted, the skill resolves
/// them from the group via group_item membership and the visible-shift SP. Returns the orchestrator
/// jobId; final completion arrives via the AutoWizard SignalR hub.
/// </summary>
/// <param name="groupId">Required group UUID (the "selectedGroup" of the schedule view).</param>
/// <param name="periodFrom">Period start date (ISO yyyy-MM-dd).</param>
/// <param name="periodUntil">Period end date (ISO yyyy-MM-dd, inclusive).</param>
/// <param name="agentIds">Optional comma-separated client UUIDs; defaults to all clients in the group.</param>
/// <param name="shiftIds">Optional comma-separated shift UUIDs; defaults to visible shifts via GetShiftSchedule.</param>
/// <param name="analyseToken">Optional source scenario token; null = main scenario.</param>
/// <param name="language">Optional UI language for Wizard 3 (LLM stage), e.g. "de", "en". Falls back to engine default.</param>

using Klacks.Api.Application.Constants;
using Klacks.Api.Application.DTOs.Schedules.AutoWizard;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules.AutoWizard;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("start_autowizard")]
public class StartAutoWizardSkill : BaseSkillImplementation
{
    private readonly IAutoWizardJobRunner _autoWizardJobRunner;
    private readonly IGroupRepository _groupRepository;
    private readonly IClientRepository _clientRepository;
    private readonly IShiftScheduleRepository _shiftScheduleRepository;

    public StartAutoWizardSkill(
        IAutoWizardJobRunner autoWizardJobRunner,
        IGroupRepository groupRepository,
        IClientRepository clientRepository,
        IShiftScheduleRepository shiftScheduleRepository)
    {
        _autoWizardJobRunner = autoWizardJobRunner;
        _groupRepository = groupRepository;
        _clientRepository = clientRepository;
        _shiftScheduleRepository = shiftScheduleRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupIdStr = GetRequiredString(parameters, "groupId");
        var periodFromStr = GetRequiredString(parameters, "periodFrom");
        var periodUntilStr = GetRequiredString(parameters, "periodUntil");
        var agentIdsRaw = GetParameter<string>(parameters, "agentIds");
        var shiftIdsRaw = GetParameter<string>(parameters, "shiftIds");
        var analyseTokenStr = GetParameter<string>(parameters, "analyseToken");
        var language = GetParameter<string>(parameters, "language");

        if (!Guid.TryParse(groupIdStr, out var groupId))
        {
            return SkillResult.Error($"Invalid groupId format: '{groupIdStr}'. Expected UUID.");
        }

        if (!DateOnly.TryParse(periodFromStr, out var periodFrom))
        {
            return SkillResult.Error($"Invalid periodFrom format: '{periodFromStr}'. Expected ISO yyyy-MM-dd.");
        }

        if (!DateOnly.TryParse(periodUntilStr, out var periodUntil))
        {
            return SkillResult.Error($"Invalid periodUntil format: '{periodUntilStr}'. Expected ISO yyyy-MM-dd.");
        }

        if (periodFrom > periodUntil)
        {
            return SkillResult.Error($"periodFrom ({periodFrom}) must be on or before periodUntil ({periodUntil}).");
        }

        var group = await _groupRepository.Get(groupId);
        if (group == null)
        {
            return SkillResult.Error($"Group with ID {groupId} not found.");
        }

        Guid? analyseToken = null;
        if (!string.IsNullOrWhiteSpace(analyseTokenStr) && Guid.TryParse(analyseTokenStr, out var parsedToken))
        {
            analyseToken = parsedToken;
        }

        var agentIds = await ResolveAgentIdsAsync(agentIdsRaw, groupId, cancellationToken);
        if (agentIds.Count == 0)
        {
            return SkillResult.Error(
                $"No agents resolved for group '{group.Name}'. Either pass explicit agentIds or assign clients to the group first.");
        }

        var shiftIds = await ResolveShiftIdsAsync(shiftIdsRaw, groupId, periodFrom, periodUntil, analyseToken, cancellationToken);
        if (shiftIds.Count == 0)
        {
            return SkillResult.Error(
                $"No shifts visible for group '{group.Name}' in period {periodFrom}..{periodUntil}. " +
                "Either pass explicit shiftIds or assign shifts to the group's hierarchy first.");
        }

        var request = new StartAutoWizardRequest(
            PeriodFrom: periodFrom,
            PeriodUntil: periodUntil,
            AgentIds: agentIds,
            ShiftIds: shiftIds,
            GroupId: groupId,
            AnalyseToken: analyseToken,
            Language: language,
            ContextDaysBefore: ScenarioConstants.BoundaryDays,
            ContextDaysAfter: ScenarioConstants.BoundaryDays);

        var jobId = await _autoWizardJobRunner.StartAsync(request, cancellationToken);

        var result = new
        {
            JobId = jobId,
            GroupId = groupId,
            GroupName = group.Name,
            PeriodFrom = periodFrom,
            PeriodUntil = periodUntil,
            AgentCount = agentIds.Count,
            ShiftCount = shiftIds.Count,
            SourceAnalyseToken = analyseToken,
            Hint = "Listen to the AutoWizard SignalR hub for completion, or call list_open_wizard_jobs to poll status."
        };

        return SkillResult.SuccessResult(
            result,
            $"AutoWizard job {jobId} started for '{group.Name}' covering {periodFrom}..{periodUntil} " +
            $"({agentIds.Count} agents, {shiftIds.Count} shifts). Wizards 1+2+3 will run sequentially " +
            $"and emit one SignalR 'OnCompleted' event when the chain finishes.");
    }

    private async Task<IReadOnlyList<Guid>> ResolveAgentIdsAsync(
        string? agentIdsRaw,
        Guid groupId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(agentIdsRaw))
        {
            return agentIdsRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .Distinct()
                .ToList();
        }

        var clients = await _clientRepository.GetActiveClientsWithAddressesForGroupsAsync(
            new List<Guid> { groupId },
            cancellationToken);
        return clients.Select(c => c.Id).Distinct().ToList();
    }

    private async Task<IReadOnlyList<Guid>> ResolveShiftIdsAsync(
        string? shiftIdsRaw,
        Guid groupId,
        DateOnly periodFrom,
        DateOnly periodUntil,
        Guid? analyseToken,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(shiftIdsRaw))
        {
            return shiftIdsRaw
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .Distinct()
                .ToList();
        }

        var filter = new ShiftScheduleFilter
        {
            StartDate = periodFrom,
            EndDate = periodUntil,
            SelectedGroup = groupId,
            AnalyseToken = analyseToken,
            StartRow = 0,
            RowCount = int.MaxValue
        };

        var (shifts, _) = await _shiftScheduleRepository.GetShiftScheduleAsync(filter, cancellationToken);
        return shifts.Select(s => s.ShiftId).Distinct().ToList();
    }
}
