// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Deletes a period cap rule via DeleteCommand.
/// </summary>
/// <param name="periodCapRuleId">UUID of the period cap rule to delete (required).</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_period_cap_rule")]
public class DeletePeriodCapRuleSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeletePeriodCapRuleSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var periodCapRuleId = GetRequiredGuid(parameters, "periodCapRuleId");

        var deleted = await _mediator.Send(
            new DeleteCommand<PeriodCapRuleResource>(periodCapRuleId), cancellationToken);

        if (deleted == null)
        {
            return SkillResult.Error($"Period cap rule {periodCapRuleId} not found.");
        }

        return SkillResult.SuccessResult(
            new
            {
                PeriodCapRuleId = periodCapRuleId,
                Period = deleted.Period.ToString(),
                Scope = deleted.Scope.ToString(),
                deleted.CapHours
            },
            $"Period cap rule for {deleted.Scope} per {deleted.Period} removed.");
    }
}
