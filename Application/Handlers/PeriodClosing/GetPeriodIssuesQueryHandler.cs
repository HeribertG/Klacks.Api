// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Common;
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces.PeriodClosing;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Handlers.PeriodClosing;

/// <summary>
/// Aggregates ScheduleNote entries plus the live validation findings (collisions,
/// rest, overtime, consecutive-days) within a billing period into PeriodIssueDto.
/// Group filter joins via Client → GroupItem. Limited to 500 issues per source for performance.
/// </summary>
/// <param name="context">EF Core DbContext used directly for the read-only ScheduleNote join</param>
/// <param name="validationLoader">Replays the schedule-validator over the period</param>
public class GetPeriodIssuesQueryHandler : BaseHandler, IRequestHandler<GetPeriodIssuesQuery, List<PeriodIssueDto>>
{
    private const int MaxNotes = 500;
    private const string ScheduleNoteCode = "ScheduleNote";
    private const string ScheduleNoteMessageKey = "periodClosing.issues.code.scheduleNote";

    private readonly DataBaseContext _context;
    private readonly IPeriodValidationLoader _validationLoader;

    public GetPeriodIssuesQueryHandler(
        DataBaseContext context,
        IPeriodValidationLoader validationLoader,
        ILogger<GetPeriodIssuesQueryHandler> logger)
        : base(logger)
    {
        _context = context;
        _validationLoader = validationLoader;
    }

    public async Task<List<PeriodIssueDto>> Handle(GetPeriodIssuesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var noteIssues = await LoadScheduleNoteIssuesAsync(request, cancellationToken);
            var validationIssues = await _validationLoader.LoadAsync(
                request.From, request.To, request.GroupId, cancellationToken);

            return noteIssues
                .Concat(validationIssues)
                .OrderBy(i => i.Date)
                .ThenBy(i => i.Severity)
                .ThenBy(i => i.ClientName)
                .ToList();
        },
        "loading period issues",
        new { request.From, request.To, request.GroupId });
    }

    private async Task<List<PeriodIssueDto>> LoadScheduleNoteIssuesAsync(
        GetPeriodIssuesQuery request,
        CancellationToken cancellationToken)
    {
        var query = from note in _context.ScheduleNotes.AsNoTracking()
                    where !note.IsDeleted
                          && note.AnalyseToken == null
                          && note.CurrentDate >= request.From
                          && note.CurrentDate <= request.To
                    join client in _context.Client.AsNoTracking() on note.ClientId equals client.Id
                    where !client.IsDeleted
                    select new { note, client };

        if (request.GroupId.HasValue)
        {
            var groupId = request.GroupId.Value;
            query = query.Where(x =>
                _context.GroupItem.AsNoTracking()
                    .Any(gi => !gi.IsDeleted && gi.ClientId == x.client.Id && gi.GroupId == groupId));
        }

        var rows = await query
            .OrderBy(x => x.note.CurrentDate)
            .ThenBy(x => x.client.FirstName)
            .ThenBy(x => x.client.Name)
            .Take(MaxNotes)
            .ToListAsync(cancellationToken);

        return rows.Select(r => new PeriodIssueDto
        {
            Date = r.note.CurrentDate,
            ClientId = r.client.Id,
            ClientName = string.IsNullOrWhiteSpace(r.client.FirstName)
                ? r.client.Name
                : $"{r.client.FirstName} {r.client.Name}",
            Severity = ScheduleValidationType.Info,
            Code = ScheduleNoteCode,
            MessageKey = ScheduleNoteMessageKey,
            MessageParams = new Dictionary<string, string>
            {
                ["content"] = r.note.Content
            }
        }).ToList();
    }
}
