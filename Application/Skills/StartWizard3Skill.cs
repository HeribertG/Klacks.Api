// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that starts only Wizard 3 (Holistic Harmonizer — LLM Vision-Bitmap). Polishes an already
/// harmonized schedule with multi-agent LLM voting. Consumes LLM credits; use sparingly.
/// </summary>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("start_wizard3")]
public class StartWizard3Skill : BaseSkillImplementation
{
    private readonly IHolisticHarmonizerJobRunner _runner;
    private readonly IClientRepository _clientRepository;

    public StartWizard3Skill(IHolisticHarmonizerJobRunner runner, IClientRepository clientRepository)
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
        var language = GetParameter<string>(parameters, "language");

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

        var input = new HolisticHarmonizerRunInput(
            PeriodFrom: periodFrom,
            PeriodUntil: periodUntil,
            AgentIds: agentIds,
            AnalyseToken: analyseToken,
            Language: language);

        var jobId = await _runner.StartAsync(input, cancellationToken);
        return SkillResult.SuccessResult(
            new
            {
                JobId = jobId,
                Stage = "Wizard3-HolisticHarmonizer",
                GroupId = groupId,
                PeriodFrom = periodFrom,
                PeriodUntil = periodUntil,
                AgentCount = agentIds.Count,
                SourceAnalyseToken = analyseToken,
                Language = language
            },
            $"Wizard 3 (Holistic Harmonizer) job {jobId} started for group {groupId}, {periodFrom}..{periodUntil}. " +
            "Note: this stage consumes LLM credits.");
    }
}
