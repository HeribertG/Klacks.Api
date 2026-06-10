// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a schedule note via DeleteCommand&lt;ScheduleNoteResource&gt;. Use
/// list_schedule_notes first to resolve the noteId.
/// </summary>
/// <param name="noteId">Required. UUID of the schedule note to delete.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_schedule_note")]
public class DeleteScheduleNoteSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public DeleteScheduleNoteSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var noteId = GetRequiredGuid(parameters, "noteId");

        var deleted = await _mediator.Send(new DeleteCommand<ScheduleNoteResource>(noteId), cancellationToken);
        if (deleted == null)
        {
            return SkillResult.Error($"Schedule note {noteId} not found.");
        }

        return SkillResult.SuccessResult(
            new { deleted.Id, deleted.ClientId, Date = deleted.CurrentDate },
            $"Schedule note from {deleted.CurrentDate:yyyy-MM-dd} deleted.");
    }
}
