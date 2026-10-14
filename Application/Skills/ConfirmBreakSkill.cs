// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that locks a booked absence (Break) at LockLevel=Confirmed. Afterwards only an administrator can delete
/// the entry and the confirmation is audit-logged; editing stays possible. Wraps ConfirmBreakCommand, so group visibility, the
/// lock-level rules and the period audit log apply exactly as for the REST endpoint.
/// </summary>
/// <param name="breakId">UUID of the Break to confirm.</param>

using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("confirm_break")]
public class ConfirmBreakSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ConfirmBreakSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var breakId = GetRequiredGuid(parameters, "breakId");

        try
        {
            var result = await _mediator.Send(new ConfirmBreakCommand(breakId), cancellationToken);
            if (result == null)
            {
                return SkillResult.Error($"Failed to confirm break {breakId}.");
            }

            return SkillResult.SuccessResult(
                new { BreakId = breakId, LockLevel = result.LockLevel },
                $"Break {breakId} confirmed (lock_level={result.LockLevel}). Only an administrator can delete this absence now.");
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Break {breakId} not found.");
        }
    }
}
