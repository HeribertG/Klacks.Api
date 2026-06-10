// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists schedule notes (per-client/per-day annotations shown in the schedule) via
/// ListQuery&lt;ScheduleNoteResource&gt;. An optional clientId narrows the result to one
/// client. Use this to find a noteId before delete_schedule_note.
/// </summary>
/// <param name="clientId">Optional. UUID of a client to filter the notes by.</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Queries;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_schedule_notes")]
public class ListScheduleNotesSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public ListScheduleNotesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientIdRaw = GetParameter<string>(parameters, "clientId");
        Guid? clientId = null;

        if (!string.IsNullOrWhiteSpace(clientIdRaw))
        {
            if (!Guid.TryParse(clientIdRaw, out var parsed))
            {
                return SkillResult.Error($"Parameter 'clientId' is not a valid UUID: '{clientIdRaw}'.");
            }

            clientId = parsed;
        }

        var notes = (await _mediator.Send(new ListQuery<ScheduleNoteResource>(), cancellationToken)).ToList();

        if (clientId.HasValue)
        {
            notes = notes.Where(n => n.ClientId == clientId.Value).ToList();
        }

        var projected = notes
            .OrderBy(n => n.CurrentDate)
            .Select(n => new
            {
                n.Id,
                n.ClientId,
                Date = n.CurrentDate,
                n.Content,
                n.AnalyseToken
            })
            .ToList();

        var message = $"Found {projected.Count} schedule note(s)" +
                      (clientId.HasValue ? $" for client {clientId.Value}" : string.Empty) + ".";

        return SkillResult.SuccessResult(new { Count = projected.Count, Notes = projected }, message);
    }
}
