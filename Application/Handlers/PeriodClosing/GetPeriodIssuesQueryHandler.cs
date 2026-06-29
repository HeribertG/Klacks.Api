// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces.PeriodClosing;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PeriodClosing;

/// <summary>
/// Aggregates ScheduleNote entries plus the live validation findings (collisions,
/// rest, overtime, consecutive-days) within a billing period into PeriodIssueDto.
/// Group filter joins via Client → GroupItem. Limited to 500 issues per source for performance.
/// </summary>
/// <param name="readRepository">Read-side repository for the ScheduleNote/Client join</param>
/// <param name="validationLoader">Replays the schedule-validator over the period</param>
public class GetPeriodIssuesQueryHandler : BaseHandler, IRequestHandler<GetPeriodIssuesQuery, List<PeriodIssueDto>>
{
    private const int MaxNotes = 500;
    private const string ScheduleNoteCode = "ScheduleNote";
    private const string ScheduleNoteMessageKey = "periodClosing.issues.code.scheduleNote";

    private readonly IPeriodClosingReadRepository _readRepository;
    private readonly IPeriodValidationLoader _validationLoader;

    public GetPeriodIssuesQueryHandler(
        IPeriodClosingReadRepository readRepository,
        IPeriodValidationLoader validationLoader,
        ILogger<GetPeriodIssuesQueryHandler> logger)
        : base(logger)
    {
        _readRepository = readRepository;
        _validationLoader = validationLoader;
    }

    public async Task<List<PeriodIssueDto>> Handle(GetPeriodIssuesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var noteIssues = await LoadScheduleNoteIssuesAsync(request, cancellationToken);
            var validationIssues = await _validationLoader.LoadAsync(
                request.From, request.To, request.GroupId, cancellationToken: cancellationToken);

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
        var rows = await _readRepository.GetScheduleNoteIssues(
            request.From, request.To, request.GroupId, MaxNotes, cancellationToken);

        return rows.Select(r => new PeriodIssueDto
        {
            Date = r.Date,
            ClientId = r.ClientId,
            ClientName = string.IsNullOrWhiteSpace(r.ClientFirstName)
                ? r.ClientName
                : $"{r.ClientFirstName} {r.ClientName}",
            Severity = ScheduleValidationType.Info,
            Code = ScheduleNoteCode,
            MessageKey = ScheduleNoteMessageKey,
            MessageParams = new Dictionary<string, string>
            {
                ["content"] = r.Content
            }
        }).ToList();
    }
}
