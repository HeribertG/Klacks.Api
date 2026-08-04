// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Judges an absence request that arrived by email against the staffing reserve, and looks for
/// alternative periods when it does not fit. Loads its data directly from the resource-monitor read
/// repository, deliberately WITHOUT the per-user group-visibility scope: there is no HTTP user in the
/// polling context, so the scoped query would return an empty series and the check would silently
/// pass on nothing at all. Numbers come from the same DailyReadinessCalculator and
/// AbsenceCapacityCalculator the chat skills use, so the email path and check_absence_capacity_reserve
/// cannot disagree. Returns NotEvaluated instead of guessing when the group is ambiguous.
/// </summary>

using System.Text;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Email;
using Klacks.Api.Domain.Models.Email;
using Klacks.Api.Domain.Services.Schedules;
using SettingKeys = Klacks.Api.Application.Constants.Settings;

namespace Klacks.Api.Infrastructure.Email;

public class EmailCapacityAdvisor : IEmailCapacityAdvisor
{
    private const int MaxRequestDays = 92;
    private const int ContextDays = 7;
    private const int MaxReportedWindows = 3;
    private const int MaxSuggestions = 3;
    private const int AlternativeSearchDays = 60;

    private readonly IGroupMembershipService _groupMembershipService;
    private readonly IResourceMonitorReadRepository _readRepository;
    private readonly ILogger<EmailCapacityAdvisor> _logger;

    public EmailCapacityAdvisor(
        IGroupMembershipService groupMembershipService,
        IResourceMonitorReadRepository readRepository,
        ILogger<EmailCapacityAdvisor> logger)
    {
        _groupMembershipService = groupMembershipService;
        _readRepository = readRepository;
        _logger = logger;
    }

    public async Task<EmailCapacityVerdict> JudgeAsync(
        Guid clientId,
        DateOnly fromDate,
        DateOnly untilDate,
        double requestedDailyValue,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (untilDate < fromDate || untilDate.DayNumber - fromDate.DayNumber + 1 > MaxRequestDays)
            {
                return EmailCapacityVerdict.NotEvaluated();
            }

            var groups = (await _groupMembershipService.GetClientGroupsAsync(clientId)).ToList();
            if (groups.Count != 1)
            {
                return EmailCapacityVerdict.NotEvaluated();
            }

            var windowStart = fromDate.AddDays(-ContextDays);
            var windowEnd = untilDate.AddDays(AlternativeSearchDays);

            var days = await LoadDaysAsync(groups[0].Id, windowStart, windowEnd, cancellationToken);
            if (days.Count == 0)
            {
                return EmailCapacityVerdict.NotEvaluated();
            }

            var ceilingRaw = await _readRepository.GetSettingValue(
                SettingKeys.SCHEDULING_MAX_CAPACITY_UTILIZATION, cancellationToken);
            var ceiling = CapacityUtilizationCeiling.Parse(ceilingRaw);

            var findings = AbsenceCapacityCalculator.Evaluate(days, fromDate, untilDate, requestedDailyValue);
            var critical = AbsenceCapacityCalculator.CriticalOnly(findings, ceiling);

            var ceilingText = CapacityUtilizationCeiling.ToPercent(ceiling).ToString("0.#");

            if (critical.Count == 0)
            {
                var peak = findings.Where(f => f.Utilization.HasValue).Select(f => f.Utilization!.Value).DefaultIfEmpty(0).Max();
                return new EmailCapacityVerdict(true, false,
                    $"Capacity reserve holds for group '{groups[0].Name}': peak utilization " +
                    $"{CapacityUtilizationCeiling.ToPercent(peak):0.#}% stays within the {ceilingText}% ceiling.");
            }

            var durationDays = untilDate.DayNumber - fromDate.DayNumber + 1;
            var alternatives = AbsenceCapacityCalculator.FindFittingPeriods(
                days, fromDate, untilDate.AddDays(AlternativeSearchDays), durationDays, requestedDailyValue, ceiling);

            return new EmailCapacityVerdict(true, true,
                BuildGapNote(groups[0].Name, ceilingText, critical, alternatives));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Capacity judgement failed for client {ClientId}", clientId);
            return EmailCapacityVerdict.NotEvaluated();
        }
    }

    private static string BuildGapNote(
        string groupName,
        string ceilingText,
        IReadOnlyList<CapacityWindowFinding> critical,
        IReadOnlyList<CapacityWindowCandidate> alternatives)
    {
        var builder = new StringBuilder();
        builder.AppendLine(
            $"Capacity reserve for group '{groupName}' is not sufficient: {critical.Count} time window(s) " +
            $"exceed the {ceilingText}% ceiling.");

        foreach (var finding in critical
                     .OrderByDescending(f => f.NoCapacityLeft)
                     .ThenByDescending(f => f.Utilization ?? double.MaxValue)
                     .Take(MaxReportedWindows))
        {
            var value = finding.NoCapacityLeft
                ? "no capacity left"
                : $"{CapacityUtilizationCeiling.ToPercent(finding.Utilization!.Value):0.#}% utilization";
            builder.AppendLine($"- {finding.Kind} {finding.From:yyyy-MM-dd} to {finding.Until:yyyy-MM-dd}: {value}");
        }

        var fitting = alternatives
            .Where(c => c.Fits)
            .OrderBy(c => c.PeakUtilization ?? double.MaxValue)
            .ThenBy(c => c.From)
            .Take(MaxSuggestions)
            .ToList();

        if (fitting.Count == 0)
        {
            builder.Append("No alternative period of the same length fits within the searched range either.");
            return builder.ToString();
        }

        builder.AppendLine("Alternative periods of the same length that would fit:");
        foreach (var candidate in fitting)
        {
            builder.AppendLine(
                $"- {candidate.From:yyyy-MM-dd} to {candidate.Until:yyyy-MM-dd} " +
                $"(peak {CapacityUtilizationCeiling.ToPercent(candidate.PeakUtilization ?? 0):0.#}%)");
        }

        return builder.ToString().TrimEnd();
    }

    private async Task<List<CapacityDay>> LoadDaysAsync(
        Guid groupId, DateOnly start, DateOnly end, CancellationToken cancellationToken)
    {
        var shiftIds = await _readRepository.GetGroupShiftIds(groupId, cancellationToken);
        if (shiftIds.Count == 0)
        {
            return [];
        }

        var clientIds = await _readRepository.GetClientIdsForShiftsInRange(shiftIds, start, end, cancellationToken);
        var contracts = await _readRepository.GetActiveContracts(start, end, clientIds, cancellationToken);
        var employeeClientIds = await _readRepository.GetEmployeeClientIds(clientIds, cancellationToken);
        var containedShiftIds = await _readRepository.GetContainedShiftIds(cancellationToken);
        var shifts = await _readRepository.GetActiveShifts(start, end, shiftIds, containedShiftIds, cancellationToken);
        var absences = await _readRepository.GetAbsences(
            start.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
            end.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
            clientIds, cancellationToken);

        var contractsByClient = contracts
            .GroupBy(cc => cc.ClientId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var allClientIds = new HashSet<Guid>(employeeClientIds);
        foreach (var cc in contracts)
        {
            allClientIds.Add(cc.ClientId);
        }

        var absenceByDate = new Dictionary<DateOnly, double>();
        foreach (var bp in absences)
        {
            if (bp.DefaultValue <= 0)
            {
                continue;
            }

            var fromDay = DateOnly.FromDateTime(bp.From);
            var untilDay = DateOnly.FromDateTime(bp.Until);
            if (fromDay < start) fromDay = start;
            if (untilDay > end) untilDay = end;

            for (var d = fromDay; d <= untilDay; d = d.AddDays(1))
            {
                absenceByDate[d] = absenceByDate.GetValueOrDefault(d) + bp.DefaultValue;
            }
        }

        var settings = await ResourceMonitorSettingsReader.ReadAsync(_readRepository, cancellationToken);

        return DailyReadinessCalculator
            .Build(start, end, allClientIds, contractsByClient, employeeClientIds, shifts, absenceByDate,
                settings.DefaultPattern, settings.MaxWorkDays, settings.MaxConsecutiveDays)
            .Select(d => new CapacityDay(d.Date, d.WunschCount, d.DienstCount, d.AbsenzCount))
            .ToList();
    }
}
