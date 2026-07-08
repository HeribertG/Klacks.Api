// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds a compact resource-load summary for the affected period and the affected employee's group,
/// for embedding into the email-analysis notification. Counts shift demand per day from the shift
/// definitions' weekday patterns, absence load from placeholders and the active headcount — directly
/// on the resource-monitor read repository, deliberately WITHOUT the per-user group-visibility scope
/// (there is no HTTP user in the polling context; recipients are planners/admins). Returns null when
/// the employee's group is not unambiguous.
/// </summary>

using System.Text;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Email;

namespace Klacks.Api.Infrastructure.Email;

public class EmailPeriodLoadService : IEmailPeriodLoadService
{
    private const int MaxSummaryDays = 92;

    private readonly IGroupMembershipService _groupMembershipService;
    private readonly IResourceMonitorReadRepository _readRepository;
    private readonly ILogger<EmailPeriodLoadService> _logger;

    public EmailPeriodLoadService(
        IGroupMembershipService groupMembershipService,
        IResourceMonitorReadRepository readRepository,
        ILogger<EmailPeriodLoadService> logger)
    {
        _groupMembershipService = groupMembershipService;
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<string?> BuildSummaryAsync(
        Guid clientId, DateOnly fromDate, DateOnly untilDate, CancellationToken cancellationToken = default)
    {
        try
        {
            if (untilDate < fromDate || untilDate.DayNumber - fromDate.DayNumber + 1 > MaxSummaryDays)
            {
                return null;
            }

            var groups = (await _groupMembershipService.GetClientGroupsAsync(clientId)).ToList();
            if (groups.Count != 1)
            {
                return null;
            }

            var group = groups[0];
            var shiftIds = await _readRepository.GetGroupShiftIds(group.Id, cancellationToken);
            if (shiftIds.Count == 0)
            {
                return null;
            }

            var clientIds = await _readRepository.GetClientIdsForShiftsInRange(
                shiftIds, fromDate, untilDate, cancellationToken);
            var headcount = (await _readRepository.GetEmployeeClientIds(clientIds, cancellationToken)).Count;

            var containedShiftIds = await _readRepository.GetContainedShiftIds(cancellationToken);
            var shifts = await _readRepository.GetActiveShifts(
                fromDate, untilDate, shiftIds, containedShiftIds, cancellationToken);
            var absences = await _readRepository.GetAbsences(
                fromDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                untilDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
                clientIds, cancellationToken);

            var peakShifts = 0;
            DateOnly peakDay = fromDate;
            var totalShiftDays = 0;
            var totalAbsenceLoad = 0.0;

            for (var day = fromDate; day <= untilDate; day = day.AddDays(1))
            {
                var shiftCount = shifts.Count(s => CoversDay(s, day));
                var absenceLoad = absences
                    .Where(a => DateOnly.FromDateTime(a.From) <= day && day <= DateOnly.FromDateTime(a.Until))
                    .Sum(a => a.DefaultValue);

                totalShiftDays += shiftCount;
                totalAbsenceLoad += absenceLoad;
                if (shiftCount > peakShifts)
                {
                    peakShifts = shiftCount;
                    peakDay = day;
                }
            }

            var days = untilDate.DayNumber - fromDate.DayNumber + 1;
            var avgShifts = days > 0 ? (double)totalShiftDays / days : 0;

            var builder = new StringBuilder();
            builder.AppendLine($"Resource load for group '{group.Name}' ({fromDate:yyyy-MM-dd} – {untilDate:yyyy-MM-dd}):");
            builder.AppendLine($"- Active employees planned in this period: {headcount}");
            builder.AppendLine($"- Shift demand: avg {avgShifts:0.#}/day, peak {peakShifts} on {peakDay:yyyy-MM-dd}");
            builder.Append($"- Absence load already booked in this period: {totalAbsenceLoad:0.#} employee-days");
            return builder.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Period load summary failed for client {ClientId}", clientId);
            return null;
        }
    }

    private static bool CoversDay(Application.DTOs.Dashboard.DashboardShiftRow shift, DateOnly day)
    {
        if (shift.FromDate > day || (shift.UntilDate != null && shift.UntilDate < day))
        {
            return false;
        }

        return day.DayOfWeek switch
        {
            DayOfWeek.Monday => shift.IsMonday,
            DayOfWeek.Tuesday => shift.IsTuesday,
            DayOfWeek.Wednesday => shift.IsWednesday,
            DayOfWeek.Thursday => shift.IsThursday,
            DayOfWeek.Friday => shift.IsFriday,
            DayOfWeek.Saturday => shift.IsSaturday,
            _ => shift.IsSunday
        };
    }
}
