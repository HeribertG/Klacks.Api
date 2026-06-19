// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that starts only Wizard 1 (TokenEvolution Planner) — the first stage of the AutoWizard chain.
/// Use when the user wants an initial coverage-first plan without running the Harmonizer / Holistic
/// Harmonizer afterwards. Auto-resolves agents and shifts from the group when not supplied.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("start_wizard1")]
public class StartWizard1Skill : BaseSkillImplementation
{
    private readonly IWizardJobRunner _runner;
    private readonly IClientRepository _clientRepository;
    private readonly IShiftScheduleRepository _shiftScheduleRepository;

    public StartWizard1Skill(
        IWizardJobRunner runner,
        IClientRepository clientRepository,
        IShiftScheduleRepository shiftScheduleRepository)
    {
        _runner = runner;
        _clientRepository = clientRepository;
        _shiftScheduleRepository = shiftScheduleRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupId = GetRequiredGuid(parameters, "groupId");
        var periodFrom = GetParameter<DateOnly?>(parameters, "periodFrom")
            ?? throw new ArgumentException("Required parameter 'periodFrom' is missing");
        var periodUntil = GetParameter<DateOnly?>(parameters, "periodUntil")
            ?? throw new ArgumentException("Required parameter 'periodUntil' is missing");
        var analyseTokenRaw = GetParameter<string>(parameters, "analyseToken");

        if (periodFrom > periodUntil)
        {
            return SkillResult.Error($"periodFrom ({periodFrom}) must be on or before periodUntil ({periodUntil}).");
        }

        Guid? analyseToken = null;
        if (!string.IsNullOrWhiteSpace(analyseTokenRaw) && Guid.TryParse(analyseTokenRaw, out var atParsed))
        {
            analyseToken = atParsed;
        }

        var agents = await _clientRepository.GetActiveClientsWithAddressesForGroupsAsync(
            new List<Guid> { groupId }, cancellationToken);
        var agentIds = agents.Select(c => c.Id).Distinct().ToList();
        if (agentIds.Count == 0)
        {
            return SkillResult.Error($"No agents in group {groupId} — abort.");
        }

        var (shifts, _) = await _shiftScheduleRepository.GetShiftScheduleAsync(
            new ShiftScheduleFilter
            {
                StartDate = periodFrom, EndDate = periodUntil,
                SelectedGroup = groupId, AnalyseToken = analyseToken,
                StartRow = 0, RowCount = int.MaxValue
            },
            cancellationToken);
        var shiftIds = shifts.Select(s => s.ShiftId).Distinct().ToList();
        if (shiftIds.Count == 0)
        {
            return SkillResult.Error($"No shifts visible for group {groupId} in {periodFrom}..{periodUntil} — abort.");
        }

        var request = new WizardContextRequest(
            PeriodFrom: periodFrom,
            PeriodUntil: periodUntil,
            AgentIds: agentIds,
            ShiftIds: shiftIds,
            AnalyseToken: analyseToken);

        var jobId = await _runner.StartAsync(request, cancellationToken);
        return SkillResult.SuccessResult(
            new
            {
                JobId = jobId,
                Stage = "Wizard1-Planner",
                GroupId = groupId,
                PeriodFrom = periodFrom,
                PeriodUntil = periodUntil,
                AgentCount = agentIds.Count,
                ShiftCount = shiftIds.Count,
                Hint = "Polling: use list_open_wizard_jobs or wait for the wizard SignalR event."
            },
            $"Wizard 1 (Planner) job {jobId} started for group {groupId}, {periodFrom}..{periodUntil}.");
    }
}
