// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Updates an existing schedule note (a per-client/per-day annotation shown in the schedule). Only
/// fields supplied as parameters are changed. Loads the note via GetQuery&lt;ScheduleNoteResource&gt;,
/// mutates it and saves via PutCommand&lt;ScheduleNoteResource&gt;.
/// </summary>
/// <param name="noteId">Required. UUID of the schedule note to update.</param>
/// <param name="content">Optional. New note text.</param>
/// <param name="date">Optional. New day of the note in ISO yyyy-MM-dd.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("update_schedule_note")]
public class UpdateScheduleNoteSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public UpdateScheduleNoteSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var noteId = GetRequiredGuid(parameters, "noteId");

        ScheduleNoteResource note;
        try
        {
            note = await _mediator.Send(new GetQuery<ScheduleNoteResource>(noteId), cancellationToken);
        }
        catch (KeyNotFoundException)
        {
            return SkillResult.Error($"Schedule note '{noteId}' not found.");
        }

        var changed = new List<string>();

        var content = GetParameter<string>(parameters, "content");
        if (!string.IsNullOrWhiteSpace(content) && content.Trim() != note.Content)
        {
            note.Content = content.Trim();
            changed.Add("content");
        }

        var date = GetParameter<DateOnly?>(parameters, "date");
        if (date.HasValue && date.Value != note.CurrentDate)
        {
            note.CurrentDate = date.Value;
            changed.Add("date");
        }

        if (changed.Count == 0)
        {
            return SkillResult.SuccessResult(
                new { NoteId = noteId, ChangedFields = Array.Empty<string>() },
                "No fields supplied for update — schedule note left unchanged.");
        }

        var updated = await _mediator.Send(new PutCommand<ScheduleNoteResource>(note), cancellationToken);
        if (updated == null)
        {
            return SkillResult.Error($"Updating schedule note '{noteId}' failed.");
        }

        return SkillResult.SuccessResult(
            new
            {
                NoteId = noteId,
                ChangedFields = changed,
                updated.ClientId,
                Date = updated.CurrentDate,
                updated.Content
            },
            $"Schedule note updated ({string.Join(", ", changed)}).");
    }
}
