// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that accepts an AnalyseScenario — promotes its works/breaks to the main schedule via the
/// existing AcceptAnalyseScenarioCommand pipeline. Used after AutoWizard or any other scenario producer
/// has run and the user (or planning agent) wants to commit the proposed plan.
/// </summary>
/// <param name="scenarioId">UUID of the scenario to accept.</param>

using Klacks.Api.Application.Commands.AnalyseScenarios;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("accept_scenario")]
public class AcceptScenarioSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public AcceptScenarioSkill(IMediator mediator)
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
            success = await _mediator.Send(new AcceptAnalyseScenarioCommand(scenarioId), cancellationToken);
        }
        catch (ConflictException ex)
        {
            return SkillResult.Error($"Failed to accept scenario {scenarioId}: {ex.Message}");
        }

        if (!success)
        {
            return SkillResult.Error($"Failed to accept scenario {scenarioId} — it may already be accepted, rejected, or not exist.");
        }
        return SkillResult.SuccessResult(
            new { ScenarioId = scenarioId },
            $"Scenario {scenarioId} accepted — its works are now part of the main schedule.");
    }
}
