// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that rejects an AnalyseScenario — discards its proposed works via the existing
/// RejectAnalyseScenarioCommand pipeline. Used when AutoWizard output is unacceptable or supersedes
/// a prior scenario.
/// </summary>
/// <param name="scenarioId">UUID of the scenario to reject.</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.Exceptions;
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
        bool success;
        try
        {
            success = await _mediator.Send(new RejectAnalyseScenarioCommand(scenarioId), cancellationToken);
        }
        catch (ConflictException ex)
        {
            return SkillResult.Error($"Failed to reject scenario {scenarioId}: {ex.Message}");
        }

        if (!success)
        {
            return SkillResult.Error($"Failed to reject scenario {scenarioId} — it may already be accepted, rejected, or not exist.");
        }
        return SkillResult.SuccessResult(
            new { ScenarioId = scenarioId },
            $"Scenario {scenarioId} rejected — its works are discarded.");
    }
}
