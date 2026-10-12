// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.DTOs.PeriodClosing;
using Klacks.Api.Application.Interfaces.PeriodClosing;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.PeriodClosing;

/// <summary>
/// Aggregates ScheduleNote entries plus the live validation findings (collisions,
/// rest, overtime, consecutive-days) within a billing period into PeriodIssueDto.
/// Group filter joins via Client → GroupItem. Limited to 500 issues per source for performance.
/// The result is bounded by the caller's group visibility: a group outside it yields nothing, client-bound findings
/// of hidden clients are dropped, and findings without a client (e.g. understaffing) are only kept when a visible
/// group was selected. Unrestricted callers (admins, no calling user, installations without groups) see everything.
/// Assumed callers are an Admin (endpoint and list_period_issues are Admin-only) or a user-less background service
/// (PeriodAutoCloseService); the visibility filter is defense in depth and must never run under a restricted ambient
/// user for the auto-close error gate, which would then count fewer errors.
/// </summary>
/// <param name="readRepository">Read-side repository for the ScheduleNote/Client join</param>
/// <param name="validationLoader">Replays the schedule-validator over the period</param>
/// <param name="escalationService">Raises Warning to Error for rules configured as Block</param>
/// <param name="groupVisibilityGuard">Tells whether the caller is restricted and may see the selected group</param>
/// <param name="clientVisibilityGuard">Keeps only the findings of clients the caller may see</param>
public class GetPeriodIssuesQueryHandler : BaseHandler, IRequestHandler<GetPeriodIssuesQuery, List<PeriodIssueDto>>
{
    private const int MaxNotes = 500;
    private const string ScheduleNoteCode = "ScheduleNote";
    private const string ScheduleNoteMessageKey = "periodClosing.issues.code.scheduleNote";

    private readonly IPeriodClosingReadRepository _readRepository;
    private readonly IPeriodValidationLoader _validationLoader;
    private readonly IComplianceEscalationService _escalationService;
    private readonly IGroupVisibilityGuard _groupVisibilityGuard;
    private readonly IClientVisibilityGuard _clientVisibilityGuard;

    public GetPeriodIssuesQueryHandler(
        IPeriodClosingReadRepository readRepository,
        IPeriodValidationLoader validationLoader,
        IComplianceEscalationService escalationService,
        IGroupVisibilityGuard groupVisibilityGuard,
        IClientVisibilityGuard clientVisibilityGuard,
        ILogger<GetPeriodIssuesQueryHandler> logger)
        : base(logger)
    {
        _readRepository = readRepository;
        _validationLoader = validationLoader;
        _escalationService = escalationService;
        _groupVisibilityGuard = groupVisibilityGuard;
        _clientVisibilityGuard = clientVisibilityGuard;
    }

    public async Task<List<PeriodIssueDto>> Handle(GetPeriodIssuesQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var isUnrestricted = await _groupVisibilityGuard.IsUnrestrictedAsync(cancellationToken);
            if (!isUnrestricted && request.GroupId is { } groupId
                && !await _groupVisibilityGuard.IsGroupVisibleAsync(groupId, cancellationToken))
            {
                return new List<PeriodIssueDto>();
            }

            var noteIssues = await LoadScheduleNoteIssuesAsync(request, cancellationToken);
            var validationIssues = await _validationLoader.LoadAsync(
                request.From, request.To, request.GroupId, cancellationToken: cancellationToken);

            // Must run before the sort: escalation rewrites Severity, which the ordering keys on.
            await _escalationService.EscalateBlockedIssuesAsync(validationIssues);

            var issues = noteIssues
                .Concat(validationIssues)
                .OrderBy(i => i.Date)
                .ThenBy(i => i.Severity)
                .ThenBy(i => i.ClientName)
                .ToList();

            return isUnrestricted
                ? issues
                : await KeepVisibleAsync(issues, request.GroupId.HasValue, cancellationToken);
        },
        "loading period issues",
        new { request.From, request.To, request.GroupId });
    }

    private async Task<List<PeriodIssueDto>> KeepVisibleAsync(
        List<PeriodIssueDto> issues, bool visibleGroupSelected, CancellationToken cancellationToken)
    {
        var clientBound = issues.Where(i => i.ClientId != Guid.Empty).ToList();
        var visibleClientIds = (await _clientVisibilityGuard.FilterVisibleAsync(clientBound, i => i.ClientId, cancellationToken))
            .Select(i => i.ClientId)
            .ToHashSet();

        return issues
            .Where(i => i.ClientId == Guid.Empty ? visibleGroupSelected : visibleClientIds.Contains(i.ClientId))
            .ToList();
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
