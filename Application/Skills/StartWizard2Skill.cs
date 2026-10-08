// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that starts only Wizard 2 (Harmonizer — Fuzzy/Conductor). Smooths an existing schedule
/// without producing a coverage-first plan from scratch and without invoking the LLM Holistic
/// Harmonizer. Caller typically passes the analyseToken of a freshly applied Wizard 1 scenario.
/// </summary>

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("start_wizard2")]
public class StartWizard2Skill : BaseSkillImplementation
{
    private readonly IHarmonizerJobRunner _runner;
    private readonly IGroupPlanningAgentRepository _planningAgentRepository;

    public StartWizard2Skill(IHarmonizerJobRunner runner, IGroupPlanningAgentRepository planningAgentRepository)
    {
        _runner = runner;
        _planningAgentRepository = planningAgentRepository;
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

        if (periodFrom > periodUntil)
        {
            return SkillResult.Error($"periodFrom ({periodFrom}) must be on or before periodUntil ({periodUntil}).");
        }

        if (!ScenarioScopeParameter.TryRead(parameters, out var analyseToken, out var scopeError))
        {
            return SkillResult.Error(scopeError!);
        }

        var agentIds = await _planningAgentRepository.GetAgentIdsAsync(
            groupId, periodFrom, periodUntil, cancellationToken);
        if (agentIds.Count == 0)
        {
            return SkillResult.Error($"No agents in group {groupId} — abort.");
        }

        var request = new HarmonizerContextRequest(
            PeriodFrom: periodFrom,
            PeriodUntil: periodUntil,
            AgentIds: agentIds,
            AnalyseToken: analyseToken);

        Guid jobId;
        try
        {
            jobId = await _runner.StartAsync(request, CancellationToken.None);
        }
        catch (AutofillLimitExceededException ex)
        {
            // The guard already knows the measured and permitted figures; repeating the check here would
            // be a second copy that can drift from it.
            return SkillResult.Error(ex.Message);
        }
        catch (AutofillRunConflictException ex)
        {
            return SkillResult.Error(
                $"A {ex.Family} job is already running for this period (jobId {ex.RunningJobId}). "
                + "Join it or cancel it first.");
        }
        return SkillResult.SuccessResult(
            new
            {
                JobId = jobId,
                Stage = "Wizard2-Harmonizer",
                GroupId = groupId,
                PeriodFrom = periodFrom,
                PeriodUntil = periodUntil,
                AgentCount = agentIds.Count,
                SourceAnalyseToken = analyseToken
            },
            $"Wizard 2 (Harmonizer) job {jobId} started for group {groupId}, {periodFrom}..{periodUntil}.");
    }
}
