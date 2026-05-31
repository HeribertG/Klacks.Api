// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Evaluates a proposed AnalyseScenario so Klacksy can recommend whether it is safe to accept:
/// it surfaces the scenario's rule violations (same engine as detect_conflicts) and the change-set
/// it introduces over the real plan (added works, replacements, absences). Thin wrapper that
/// dispatches <see cref="Klacks.Api.Application.Queries.Schedules.EvaluateScenarioQuery"/>. The
/// recommendation is advisory only — accepting a scenario stays a manual step (accept_scenario).
/// Pass either scenarioId or analyseToken (both come from list_scenarios).
/// </summary>
/// <param name="scenarioId">UUID of the scenario (from list_scenarios); required if analyseToken is omitted.</param>
/// <param name="analyseToken">UUID isolation token of the scenario; required if scenarioId is omitted.</param>

using Klacks.Api.Application.Queries.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("evaluate_scenario")]
public class EvaluateScenarioSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public EvaluateScenarioSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var scenarioIdRaw = GetParameter<string>(parameters, "scenarioId");
        var tokenRaw = GetParameter<string>(parameters, "analyseToken");

        Guid? scenarioId = null;
        if (!string.IsNullOrWhiteSpace(scenarioIdRaw))
        {
            if (!Guid.TryParse(scenarioIdRaw, out var parsedId))
            {
                return SkillResult.Error($"Invalid scenarioId: {scenarioIdRaw}.");
            }
            scenarioId = parsedId;
        }

        Guid? token = null;
        if (!string.IsNullOrWhiteSpace(tokenRaw))
        {
            if (!Guid.TryParse(tokenRaw, out var parsedToken))
            {
                return SkillResult.Error($"Invalid analyseToken: {tokenRaw}.");
            }
            token = parsedToken;
        }

        if (!scenarioId.HasValue && !token.HasValue)
        {
            return SkillResult.Error("Provide either scenarioId or analyseToken.");
        }

        var result = await _mediator.Send(new EvaluateScenarioQuery(scenarioId, token), cancellationToken);

        if (!result.Found)
        {
            var requested = token?.ToString() ?? scenarioId?.ToString() ?? string.Empty;
            return SkillResult.Error($"No scenario found for '{requested}'.");
        }

        var message =
            $"Scenario '{result.Name}' ({result.Status}) {result.FromDate}..{result.UntilDate}: " +
            $"{result.Errors} error(s), {result.Warnings} warning(s); " +
            $"{result.AddedEntryCount} added entr{(result.AddedEntryCount == 1 ? "y" : "ies")} " +
            $"({result.AddedWorkEntries} work, {result.AddedReplacementEntries} replacement, {result.AddedBreakEntries} break), " +
            $"{result.RemovedEntryCount} removed. {result.Recommendation}";

        return SkillResult.SuccessResult(result, message);
    }
}
