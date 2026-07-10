// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the seal/unseal audit trail of billing periods (the protocol card of the
/// period-closing page): action, date range, group, reason, affected item count and
/// timestamp — answers "when and why was this period sealed or re-opened".
/// </summary>
/// <param name="startDate">Optional. Range start (yyyy-MM-dd); defaults to 365 days ago.</param>
/// <param name="endDate">Optional. Range end (yyyy-MM-dd); defaults to today.</param>
/// <param name="limit">Optional. Maximum number of entries to return (default 25, newest first).</param>

using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_period_audit_log")]
public class ListPeriodAuditLogSkill : BaseSkillImplementation
{
    private const int DefaultLimit = 25;
    private const int DefaultRangeDays = 365;

    private readonly IMediator _mediator;
    private readonly ICompanyClock _companyClock;

    public ListPeriodAuditLogSkill(IMediator mediator, ICompanyClock companyClock)
    {
        _mediator = mediator;
        _companyClock = companyClock;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(await _companyClock.GetTodayAsync(cancellationToken));
        var startDate = GetParameter<DateOnly?>(parameters, "startDate") ?? today.AddDays(-DefaultRangeDays);
        var endDate = GetParameter<DateOnly?>(parameters, "endDate") ?? today;
        if (startDate > endDate)
        {
            return SkillResult.Error($"startDate ({startDate}) must be on or before endDate ({endDate}).");
        }

        var limit = GetParameter<int?>(parameters, "limit") ?? DefaultLimit;
        if (limit < 1)
        {
            limit = DefaultLimit;
        }

        var entries = await _mediator.Send(
            new GetPeriodAuditLogQuery(startDate, endDate), cancellationToken);

        var listed = entries
            .OrderByDescending(e => e.PerformedAt)
            .Take(limit)
            .Select(e => new
            {
                Action = e.Action.ToString(),
                StartDate = e.StartDate.ToString("yyyy-MM-dd"),
                EndDate = e.EndDate.ToString("yyyy-MM-dd"),
                e.GroupId,
                GroupName = e.GroupName ?? "(all groups)",
                e.Reason,
                e.AffectedCount,
                PerformedAt = e.PerformedAt.ToString("yyyy-MM-dd HH:mm")
            })
            .ToList();

        var truncatedNote = entries.Count > limit
            ? $" Showing the newest {limit} of {entries.Count} entries."
            : string.Empty;

        return SkillResult.SuccessResult(
            new
            {
                RangeStart = startDate,
                RangeEnd = endDate,
                TotalEntries = entries.Count,
                Entries = listed
            },
            entries.Count == 0
                ? $"No seal/unseal audit entries between {startDate} and {endDate}."
                : $"{entries.Count} seal/unseal audit entr(ies) between {startDate} and {endDate}.{truncatedNote}");
    }
}
