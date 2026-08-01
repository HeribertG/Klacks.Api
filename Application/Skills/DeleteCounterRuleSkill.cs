// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deletes a counter rule via DeleteCommand.
/// </summary>
/// <param name="counterRuleId">UUID of the counter rule to delete (required).</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_counter_rule")]
public class DeleteCounterRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteCounterRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var counterRuleId = GetRequiredGuid(parameters, "counterRuleId");

        var deleted = await _mediator.Send(
            new DeleteCommand<CounterRuleResource>(counterRuleId), cancellationToken);

        if (deleted == null)
        {
            return SkillResult.Error($"Counter rule {counterRuleId} not found.");
        }

        return SkillResult.SuccessResult(
            new
            {
                CounterRuleId = counterRuleId,
                EventType = deleted.EventType.ToString(),
                Period = deleted.Period.ToString(),
                deleted.Threshold
            },
            $"Counter rule for {deleted.EventType} per {deleted.Period} removed.");
    }
}
