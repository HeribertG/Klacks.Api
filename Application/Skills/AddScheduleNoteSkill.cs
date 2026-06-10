// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Adds a schedule note (a per-client/per-day annotation shown in the schedule) via
/// PostCommand&lt;ScheduleNoteResource&gt;.
/// </summary>
/// <param name="clientId">Required. UUID of the client the note belongs to.</param>
/// <param name="date">Required. Day of the note in ISO yyyy-MM-dd.</param>
/// <param name="content">Required. The note text.</param>
/// <param name="analyseToken">Optional scenario UUID; null = main schedule.</param>

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_schedule_note")]
public class AddScheduleNoteSkill : BaseSkillImplementation
{
    private readonly IMediator _mediator;

    public AddScheduleNoteSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var clientId = GetRequiredGuid(parameters, "clientId");
        var date = GetParameter<DateOnly?>(parameters, "date");
        if (date == null)
        {
            return SkillResult.Error("Required parameter 'date' is missing or not a valid ISO date (yyyy-MM-dd).");
        }

        var content = GetParameter<string>(parameters, "content");
        if (string.IsNullOrWhiteSpace(content))
        {
            return SkillResult.Error("Required parameter 'content' must not be empty.");
        }

        var analyseTokenRaw = GetParameter<string>(parameters, "analyseToken");
        Guid? analyseToken = null;
        if (!string.IsNullOrWhiteSpace(analyseTokenRaw) && Guid.TryParse(analyseTokenRaw, out var parsedToken))
        {
            analyseToken = parsedToken;
        }

        var resource = new ScheduleNoteResource
        {
            Id = Guid.NewGuid(),
            ClientId = clientId,
            CurrentDate = date.Value,
            Content = content.Trim(),
            AnalyseToken = analyseToken
        };

        var created = await _mediator.Send(new PostCommand<ScheduleNoteResource>(resource), cancellationToken);
        if (created == null)
        {
            return SkillResult.Error("Schedule note could not be created.");
        }

        return SkillResult.SuccessResult(
            new { created.Id, created.ClientId, Date = created.CurrentDate, created.Content },
            $"Schedule note added for client {created.ClientId} on {created.CurrentDate:yyyy-MM-dd}.");
    }
}
