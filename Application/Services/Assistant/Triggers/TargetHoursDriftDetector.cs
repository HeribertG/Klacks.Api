// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scans the last completed calendar month for clients whose accumulated hours diverge from their
/// guaranteed hours by more than DriftThresholdHours. Emits one TargetHoursDriftTriggerEvent
/// per affected client; severity is set per absolute drift magnitude in the event record itself.
/// Uses IClientRepository.GetQuery to enumerate the workforce and IWorkRepository.GetPeriodHoursForClients
/// for the bulk hours read. Customers are excluded, see BuildCandidateQuery. Unlike its sibling
/// detectors this one carries no cap - the roster it scans is the entire active, non-customer client
/// base, so GetActiveFingerprintsAsync re-runs the identical candidate query and threshold check
/// instead of lifting a cap, projecting only the client id (no name, no navigation properties).
/// </summary>
/// <param name="clientRepository">Active client roster, customers included.</param>
/// <param name="workRepository">Bulk period-hours read with GuaranteedHours per client.</param>
/// <param name="activityProbe">Answers whether the scanned period was ever planned at all.</param>
/// <param name="logger">Structured log per tick.</param>
/// <param name="timeProvider">Clock used to derive the last completed month.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Assistant;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class TargetHoursDriftDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    private const decimal DriftThresholdHours = 12m;

    private readonly IClientRepository _clientRepository;
    private readonly IWorkRepository _workRepository;
    private readonly IScheduleActivityProbe _activityProbe;
    private readonly ILogger<TargetHoursDriftDetector> _logger;
    private readonly TimeProvider _timeProvider;

    public TargetHoursDriftDetector(
        IClientRepository clientRepository,
        IWorkRepository workRepository,
        IScheduleActivityProbe activityProbe,
        ILogger<TargetHoursDriftDetector> logger,
        TimeProvider timeProvider)
    {
        _clientRepository = clientRepository;
        _workRepository = workRepository;
        _activityProbe = activityProbe;
        _logger = logger;
        _timeProvider = timeProvider;
    }

    public string Kind => AgentTriggerKinds.TargetHoursDrift;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var (periodStart, periodEnd, periodLabel) = ComputePeriod();

        if (!await WasPeriodPlannedAsync(periodStart, periodEnd, periodLabel, cancellationToken))
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var clients = await BuildCandidateQuery()
            .Select(c => new { c.Id, c.FirstName, c.Name })
            .ToListAsync(cancellationToken);
        if (clients.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var clientIds = clients.Select(c => c.Id).ToList();
        var hoursMap = await _workRepository.GetPeriodHoursForClients(clientIds, periodStart, periodEnd, analyseToken: null, cancellationToken);

        var events = new List<IAgentTriggerEvent>();
        foreach (var client in clients)
        {
            if (!hoursMap.TryGetValue(client.Id, out var hours)) continue;
            if (!ExceedsThreshold(hours, out var drift)) continue;

            var clientName = $"{client.FirstName} {client.Name}".Trim();
            events.Add(new TargetHoursDriftTriggerEvent(
                client.Id,
                string.IsNullOrEmpty(clientName) ? client.Id.ToString() : clientName,
                drift,
                periodLabel));
        }

        _logger.LogInformation(
            "TargetHoursDrift scan for {Period}: {Clients} client(s) scanned, {Events} drift event(s) emitted",
            periodLabel, clients.Count, events.Count);

        return events;
    }

    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var (periodStart, periodEnd, periodLabel) = ComputePeriod();

        if (!await WasPeriodPlannedAsync(periodStart, periodEnd, periodLabel, cancellationToken))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var clientIds = await BuildCandidateQuery()
            .Select(c => c.Id)
            .ToListAsync(cancellationToken);
        if (clientIds.Count == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        var hoursMap = await _workRepository.GetPeriodHoursForClients(clientIds, periodStart, periodEnd, analyseToken: null, cancellationToken);

        return clientIds
            .Where(id => hoursMap.TryGetValue(id, out var hours) && ExceedsThreshold(hours, out _))
            .Select(id => AgentConditionLedgerPolicy.FingerprintFor(
                Kind,
                TargetHoursDriftTriggerEvent.DedupKeyFor(id, periodLabel)))
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// A period in which nobody was scheduled at all reports the full guaranteed hours as a deficit for
    /// every single employee — the same defect the ComputePeriod comment describes for the running
    /// month, one level up: arithmetic, not a finding. Observed on 2026-09-07 in an installation with
    /// zero work rows, where every employee was reported with a deficit of 170 to 180 hours. Gating the
    /// fingerprint source too keeps the condition ledger from opening rows the scan will never report.
    /// </summary>
    private async Task<bool> WasPeriodPlannedAsync(
        DateOnly periodStart,
        DateOnly periodEnd,
        string periodLabel,
        CancellationToken cancellationToken)
    {
        if (await _activityProbe.HasAnyWorkInRangeAsync(periodStart, periodEnd, cancellationToken))
        {
            return true;
        }

        _logger.LogInformation(
            "TargetHoursDrift scan for {Period} skipped — the period holds no work assignment at all, so every deviation would be the contract itself",
            periodLabel);

        return false;
    }

    private static bool ExceedsThreshold(PeriodHoursResource hours, out decimal drift)
    {
        drift = 0m;
        if (hours.GuaranteedHours <= 0) return false;

        drift = (hours.Hours + hours.Surcharges) - hours.GuaranteedHours;
        return Math.Abs(drift) >= DriftThresholdHours;
    }

    // The running month is always short on hours simply because it has not happened yet, so
    // scanning it reports a deficit for everyone. Only the last completed month is a period
    // for which time entry is actually expected.
    private (DateOnly PeriodStart, DateOnly PeriodEnd, string PeriodLabel) ComputePeriod()
    {
        var today = DateOnly.FromDateTime(_timeProvider.GetUtcNow().UtcDateTime);
        var periodEnd = new DateOnly(today.Year, today.Month, 1).AddDays(-1);
        var periodStart = new DateOnly(periodEnd.Year, periodEnd.Month, 1);
        var periodLabel = $"{periodEnd.Year:0000}-{periodEnd.Month:00}";

        return (periodStart, periodEnd, periodLabel);
    }

    // A customer is never scheduled and ClientBaseQueryService keeps them out of the schedule
    // entirely, so a drift message about one leads to a page that cannot show them.
    private IQueryable<Client> BuildCandidateQuery() =>
        _clientRepository.GetQuery()
            .Where(c => !c.IsDeleted && c.Type != EntityTypeEnum.Customer);
}
