// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deletes a restricted time window rule via DeleteCommand.
/// </summary>
/// <param name="restrictedTimeWindowRuleId">UUID of the rule to delete (required).</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_restricted_time_window_rule")]
public class DeleteRestrictedTimeWindowRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteRestrictedTimeWindowRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var restrictedTimeWindowRuleId = GetRequiredGuid(parameters, "restrictedTimeWindowRuleId");

        var deleted = await _mediator.Send(
            new DeleteCommand<RestrictedTimeWindowRuleResource>(restrictedTimeWindowRuleId), cancellationToken);

        if (deleted == null)
        {
            return SkillResult.Error($"Restricted time window rule {restrictedTimeWindowRuleId} not found.");
        }

        return SkillResult.SuccessResult(
            new
            {
                RestrictedTimeWindowRuleId = restrictedTimeWindowRuleId,
                DailyStart = deleted.DailyStart.ToString("HH:mm"),
                DailyEnd = deleted.DailyEnd.ToString("HH:mm"),
                deleted.AppliesToGroupTag
            },
            $"Restricted time window rule {deleted.DailyStart:HH\\:mm}-{deleted.DailyEnd:HH\\:mm} removed.");
    }
}
