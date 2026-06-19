// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that starts only Wizard 2 (Harmonizer — Fuzzy/Conductor). Smooths an existing schedule
/// without producing a coverage-first plan from scratch and without invoking the LLM Holistic
/// Harmonizer. Caller typically passes the analyseToken of a freshly applied Wizard 1 scenario.
/// </summary>

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
    private readonly IClientRepository _clientRepository;

    public StartWizard2Skill(IHarmonizerJobRunner runner, IClientRepository clientRepository)
    {
        _runner = runner;
        _clientRepository = clientRepository;
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

        var request = new HarmonizerContextRequest(
            PeriodFrom: periodFrom,
            PeriodUntil: periodUntil,
            AgentIds: agentIds,
            AnalyseToken: analyseToken);

        var jobId = await _runner.StartAsync(request, cancellationToken);
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
