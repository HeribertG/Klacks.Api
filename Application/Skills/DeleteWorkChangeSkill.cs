// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a WorkChange (correction / replacement / travel / briefing on a Work entry) via
/// DeleteCommand&lt;WorkChangeResource&gt;. Use get_work_details / search first to resolve the id.
/// </summary>
/// <param name="workChangeId">Required. UUID of the WorkChange to delete.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_workchange")]
public class DeleteWorkChangeSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteWorkChangeSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var workChangeId = GetRequiredGuid(parameters, "workChangeId");

        var deleted = await _mediator.Send(new DeleteCommand<WorkChangeResource>(workChangeId), cancellationToken);
        if (deleted == null)
        {
            return SkillResult.Error($"WorkChange {workChangeId} not found.");
        }

        return SkillResult.SuccessResult(
            new { deleted.Id, deleted.WorkId, Type = deleted.Type.ToString() },
            "WorkChange deleted.");
    }
}
