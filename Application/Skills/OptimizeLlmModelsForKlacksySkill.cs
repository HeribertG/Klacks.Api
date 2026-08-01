// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Runs the suitability check over every configured language model via
/// OptimizeModelsForKlacksyCommand: each one is probed for reachability, tool calling, latency and
/// cost, unsuitable ones are switched off, and the best remaining one becomes the default.
/// </summary>

using Klacks.Api.Application.Commands.Assistant;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("optimize_llm_models_for_klacksy")]
public class OptimizeLlmModelsForKlacksySkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public OptimizeLlmModelsForKlacksySkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var response = await _mediator.Send(new OptimizeModelsForKlacksyCommand(), cancellationToken);

        var qualifying = response.Models.Count(m => m.Qualifies);
        var enabled = response.Models.Count(m => m.IsEnabled);

        var projected = response.Models
            .OrderByDescending(m => m.Qualifies)
            .ThenBy(m => m.LatencyMs)
            .Select(m => new
            {
                m.ModelId,
                m.DisplayName,
                m.ProviderId,
                m.IsHealthy,
                m.SupportsToolCalling,
                m.LatencyMs,
                m.Qualifies,
                m.IsEnabled,
                m.IsDefault,
                m.Error
            })
            .ToList();

        return SkillResult.SuccessResult(
            new { response.DefaultModelId, Checked = projected.Count, Qualifying = qualifying, Enabled = enabled, Models = projected },
            $"{projected.Count} model(s) checked, {qualifying} suitable, {enabled} left switched on. " +
            $"Default is now {response.DefaultModelId ?? "unset"}.");
    }
}
