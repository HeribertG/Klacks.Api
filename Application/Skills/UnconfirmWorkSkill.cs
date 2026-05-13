// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that unlocks a previously confirmed Work entry (drops LockLevel to None). After this, the cell
/// becomes mutable again for Wizards 1+2+3. Wraps UnconfirmWorkCommand.
/// </summary>
/// <param name="workId">UUID of the Work to unconfirm.</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("unconfirm_work")]
public class UnconfirmWorkSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UnconfirmWorkSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var workId = GetRequiredGuid(parameters, "workId");
        var result = await _mediator.Send(new UnconfirmWorkCommand(workId), cancellationToken);
        if (result == null)
        {
            return SkillResult.Error($"Failed to unconfirm work {workId} — entry may not exist or is at a higher lock level (Approved/Closed) that this skill cannot drop.");
        }
        return SkillResult.SuccessResult(
            new { WorkId = workId, LockLevel = result.LockLevel },
            $"Work {workId} unconfirmed (lock_level={result.LockLevel}). Wizards may now modify this cell.");
    }
}
