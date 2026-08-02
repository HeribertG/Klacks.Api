// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves a sealed order by name/customer and returns the work entries booked inside it
/// (date, employee, times, hours, surcharges) for the optional date range, via
/// ListSealedOrdersQuery for resolution and GetSealedOrderDetailsQuery for the content.
/// </summary>
/// <param name="orderName">Name, abbreviation or customer text identifying the sealed order (required).</param>
/// <param name="fromDate">Optional lower bound for the booked work date.</param>
/// <param name="untilDate">Optional upper bound for the booked work date.</param>

using Klacks.Api.Application.Queries.Exports;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_sealed_order_work_entries")]
public class GetSealedOrderWorkEntriesSkill : BaseSkillImplementation
{
    private const int MaxReturnedEntries = 50;
    private const int MaxReportedCandidates = 10;
    private const string HoursFormat = "0.##";

    private readonly IMediator _mediator;

    public GetSealedOrderWorkEntriesSkill(IMediator mediator)
    {
        _mediator = mediator;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var orderName = GetRequiredString(parameters, "orderName");
        var fromDate = GetParameter<DateOnly?>(parameters, "fromDate");
        var untilDate = GetParameter<DateOnly?>(parameters, "untilDate");

        if (fromDate.HasValue && untilDate.HasValue && fromDate.Value > untilDate.Value)
        {
            return SkillResult.Error("fromDate must not be after untilDate.");
        }

        var candidates = await _mediator.Send(
            new ListSealedOrdersQuery(null, null, null, orderName), cancellationToken);

        if (candidates.Count == 0)
        {
            return SkillResult.Error($"No sealed order found matching '{orderName}'.");
        }

        if (candidates.Count > 1)
        {
            var shown = candidates.Take(MaxReportedCandidates);
            var options = string.Join(", ", shown.Select(c =>
                $"{c.Name} ({c.Abbreviation}, customer {c.CustomerName ?? "n/a"}, id {c.Id})"));
            var suffix = candidates.Count > MaxReportedCandidates
                ? $", and {candidates.Count - MaxReportedCandidates} more"
                : string.Empty;
            return SkillResult.Error(
                $"'{orderName}' matches {candidates.Count} sealed orders — {options}{suffix}. " +
                "Ask the user which one they mean; do not guess.");
        }

        var match = candidates[0];

        var details = await _mediator.Send(
            new GetSealedOrderDetailsQuery(match.Id, fromDate, untilDate), cancellationToken);

        if (details == null)
        {
            return SkillResult.Error($"Sealed order '{match.Name}' ({match.Id}) could not be loaded.");
        }

        var allEntries = details.WorkEntries;
        var totalCount = allEntries.Count;
        var totalHours = allEntries.Sum(e => e.Hours);
        var totalSurcharges = allEntries.Sum(e => e.Surcharges);
        var truncated = totalCount > MaxReturnedEntries;

        var returnedEntries = allEntries
            .Take(MaxReturnedEntries)
            .Select(e => new
            {
                e.WorkId,
                e.EmployeeName,
                e.EmployeeIdNumber,
                e.WorkDate,
                e.StartTime,
                e.EndTime,
                e.Hours,
                e.Surcharges,
                e.LockLevel,
                e.PeriodClosed
            })
            .ToList();

        var data = new
        {
            details.Id,
            details.Name,
            details.Abbreviation,
            details.CustomerName,
            details.CustomerNumber,
            TotalCount = totalCount,
            ReturnedCount = returnedEntries.Count,
            Truncated = truncated,
            TotalHours = totalHours,
            TotalSurcharges = totalSurcharges,
            Entries = returnedEntries
        };

        var hoursText = totalHours.ToString(HoursFormat);
        var surchargesText = totalSurcharges.ToString(HoursFormat);

        var message = truncated
            ? $"Order '{details.Name}' has {totalCount} work entries totaling {hoursText}h " +
              $"(surcharges {surchargesText}h) — showing the first {MaxReturnedEntries}, " +
              "sorted by date and start time."
            : $"Order '{details.Name}' has {totalCount} work entries totaling {hoursText}h " +
              $"(surcharges {surchargesText}h).";

        return SkillResult.SuccessResult(data, message);
    }
}
