// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that locks a Work entry at LockLevel=Confirmed (1). After this, the cell is immutable for
/// Wizards 1+2+3. Wraps ConfirmWorkCommand so period-hour recalculation stays consistent.
/// </summary>
/// <param name="workId">UUID of the Work to confirm.</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("confirm_work")]
public class ConfirmWorkSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ConfirmWorkSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var workId = GetRequiredGuid(parameters, "workId");
        var result = await _mediator.Send(new ConfirmWorkCommand(workId), cancellationToken);
        if (result == null)
        {
            return SkillResult.Error($"Failed to confirm work {workId} — entry may not exist or is already at a higher lock level.");
        }
        return SkillResult.SuccessResult(
            new { WorkId = workId, LockLevel = result.LockLevel },
            $"Work {workId} confirmed (lock_level={result.LockLevel}). Wizards will no longer modify this cell.");
    }
}
