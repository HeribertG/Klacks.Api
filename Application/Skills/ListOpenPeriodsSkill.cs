// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lists the billing periods that actually contain works or breaks (the period dropdown of
/// the period-closing page) together with each period's sealing state — the basis for
/// "which periods are ready to be closed". Periods are group-aware via the group's payment
/// interval.
/// </summary>
/// <param name="limit">Optional. Maximum number of periods to return (default 12, newest first).</param>

using Klacks.Api.Application.Queries.PeriodClosing;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_open_periods")]
public class ListOpenPeriodsSkill : BaseSkillImplementation
{
    private const int DefaultLimit = 12;

    private readonly IMediator _mediator;

    public ListOpenPeriodsSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var limit = GetParameter<int?>(parameters, "limit") ?? DefaultLimit;
        if (limit < 1)
        {
            limit = DefaultLimit;
        }

        var periods = await _mediator.Send(new GetUsedPeriodsQuery(), cancellationToken);

        var newest = periods
            .OrderByDescending(p => p.StartDate)
            .Take(limit)
            .ToList();

        var listed = new List<object>();
        var fullySealedCount = 0;
        foreach (var period in newest)
        {
            var days = await _mediator.Send(
                new GetSealedPeriodsQuery(period.StartDate, period.EndDate, period.GroupId), cancellationToken);
            var sealedDays = days.Count(d => d.IsDaySealed);
            var isFullySealed = days.Count > 0 && days.All(d => d.IsDaySealed);
            if (isFullySealed)
            {
                fullySealedCount++;
            }

            listed.Add(new
            {
                StartDate = period.StartDate.ToString("yyyy-MM-dd"),
                EndDate = period.EndDate.ToString("yyyy-MM-dd"),
                PaymentInterval = period.PaymentInterval.ToString(),
                period.GroupId,
                GroupName = period.GroupName ?? "(all groups)",
                SealedDays = sealedDays,
                TotalDays = days.Count,
                IsFullySealed = isFullySealed
            });
        }

        var truncatedNote = periods.Count > limit
            ? $" Showing the newest {limit} of {periods.Count} periods."
            : string.Empty;

        return SkillResult.SuccessResult(
            new
            {
                TotalPeriods = periods.Count,
                FullySealedInList = fullySealedCount,
                Periods = listed
            },
            periods.Count == 0
                ? "No populated billing periods found."
                : $"{periods.Count} populated billing period(s); {fullySealedCount} of the listed ones are fully " +
                  $"sealed, the rest are open or partially sealed.{truncatedNote} " +
                  "Use get_period_status and list_period_issues before sealing with close_period.");
    }
}
