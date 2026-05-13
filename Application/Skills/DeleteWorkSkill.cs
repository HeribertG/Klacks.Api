// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that soft-deletes a single Work entry (the assignment of a client to a shift on a date) via the
/// existing DeleteWorkCommand pipeline. Period range is taken from the work itself so period-hour
/// recalculation kicks in correctly.
/// </summary>
/// <param name="workId">UUID of the Work entry to delete.</param>

using Klacks.Api.Application.Commands.Works;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_work")]
public class DeleteWorkSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;
    private readonly IWorkRepository _workRepository;

    public DeleteWorkSkill(IMediator mediator, IWorkRepository workRepository)
    {
        _mediator = mediator;
        _workRepository = workRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var workId = GetRequiredGuid(parameters, "workId");

        var existing = await _workRepository.Get(workId);
        if (existing == null)
        {
            return SkillResult.Error($"Work {workId} not found.");
        }

        var date = existing.CurrentDate;
        var result = await _mediator.Send(
            new DeleteWorkCommand(workId, date, date),
            cancellationToken);

        if (result == null)
        {
            return SkillResult.Error($"Delete of work {workId} did not return a result — operation may have failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                WorkId = workId,
                ClientId = existing.ClientId,
                ShiftId = existing.ShiftId,
                Date = date,
                LockLevelBefore = existing.LockLevel
            },
            $"Work {workId} (client {existing.ClientId} on {date}) deleted.");
    }
}
