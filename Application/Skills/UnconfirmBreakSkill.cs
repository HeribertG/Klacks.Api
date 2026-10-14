// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Skill that drops a booked absence (Break) from its lock level back to None. Wraps UnconfirmBreakCommand, so
/// group visibility and the lock-level rules apply exactly as for the REST endpoint: Approved needs the
/// supervisor or admin role, Closed needs the admin role.
/// </summary>
/// <param name="breakId">UUID of the Break to unconfirm.</param>

using Klacks.Api.Application.Commands.Breaks;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("unconfirm_break")]
public class UnconfirmBreakSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UnconfirmBreakSkill(IMediator mediator)
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
            var result = await _mediator.Send(new UnconfirmBreakCommand(breakId), cancellationToken);
            if (result == null)
            {
                return SkillResult.Error($"Failed to unconfirm break {breakId}.");
            }

            return SkillResult.SuccessResult(
                new { BreakId = breakId, LockLevel = result.LockLevel },
                $"Break {breakId} unconfirmed (lock_level={result.LockLevel}). The absence can be deleted again by non-administrators.");
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Break {breakId} not found.");
        }
    }
}
