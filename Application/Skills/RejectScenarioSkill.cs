// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that rejects an AnalyseScenario — discards its proposed works via the existing
/// RejectAnalyseScenarioCommand pipeline. Used when AutoWizard output is unacceptable or supersedes
/// a prior scenario.
/// </summary>
/// <param name="scenarioId">UUID of the scenario to reject.</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("reject_scenario")]
public class RejectScenarioSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public RejectScenarioSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var scenarioId = GetRequiredGuid(parameters, "scenarioId");
        var success = await _mediator.Send(new RejectAnalyseScenarioCommand(scenarioId), cancellationToken);
        if (!success)
        {
            return SkillResult.Error($"Failed to reject scenario {scenarioId} — it may already be accepted, rejected, or not exist.");
        }
        return SkillResult.SuccessResult(
            new { ScenarioId = scenarioId },
            $"Scenario {scenarioId} rejected — its works are discarded.");
    }
}
