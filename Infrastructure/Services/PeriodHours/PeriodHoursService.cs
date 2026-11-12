// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Infrastructure.Repositories.Associations;
using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;
using Klacks.Api.Infrastructure.Services.Clients;
using Klacks.Api.Infrastructure.Services.Schedules;

namespace Klacks.Api.Infrastructure.Services.PeriodHours;

public class PeriodHoursService : IPeriodHoursService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<PeriodHoursService> _logger;
    private readonly IWorkNotificationService _notificationService;
    private readonly IClientGroupFilterService _clientGroupFilterService;
    private readonly IClientContractDataProvider _contractDataProvider;
    private readonly IWeekConfiguration _weekConfiguration;
    private readonly IFrozenPeriodPolicy _frozenPeriodPolicy;

    public PeriodHoursService(
        DataBaseContext context,
        ILogger<PeriodHoursService> logger,
        IWorkNotificationService notificationService,
        IClientGroupFilterService clientGroupFilterService,
        IClientContractDataProvider contractDataProvider,
        IWeekConfiguration weekConfiguration,
        IFrozenPeriodPolicy frozenPeriodPolicy)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
        _clientGroupFilterService = clientGroupFilterService;
        _contractDataProvider = contractDataProvider;
        _weekConfiguration = weekConfiguration;
        _frozenPeriodPolicy = frozenPeriodPolicy;
    }

    public async Task<Dictionary<Guid, PeriodHoursResource>> GetPeriodHoursAsync(
        List<Guid> clientIds,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken = null)
    {
        if (clientIds.Count == 0)
        {
            return new Dictionary<Guid, PeriodHoursResource>();
        }

        var result = new Dictionary<Guid, PeriodHoursResource>();

        var cachedPeriodHours = await _context.ClientPeriodHours
            .Where(p => clientIds.Contains(p.ClientId)
                && p.StartDate == startDate
                && p.EndDate == endDate
                && p.AnalyseToken == analyseToken)
            .ToListAsync();

        var clientIdsWithCache = cachedPeriodHours.Select(p => p.ClientId).ToHashSet();
        var clientIdsWithoutCache = clientIds.Where(id => !clientIdsWithCache.Contains(id)).ToList();

        var effectiveDataByClient = await _contractDataProvider.GetEffectiveContractDataForClientsAsync(clientIds, startDate);
        var membershipWindows = await MembershipWindowQuery.LoadAsync(_context, clientIds, CancellationToken.None);

        decimal TargetOf(Guid clientId) => effectiveDataByClient.TryGetValue(clientId, out var data)
            ? MembershipTargetProration.ProratedTarget(
                data.GuaranteedHours, membershipWindows.GetValueOrDefault(clientId), startDate, endDate)
            : 0m;

        foreach (var ph in cachedPeriodHours)
        {
            result[ph.ClientId] = new PeriodHoursResource
            {
                Hours = ph.Hours,
                Surcharges = ph.Surcharges,
                GuaranteedHours = TargetOf(ph.ClientId)
            };
        }

        if (clientIdsWithoutCache.Count > 0)
        {
            var calculatedHours = await CalculatePeriodHoursForClientsAsync(
                clientIdsWithoutCache,
                startDate,
                endDate,
                analyseToken,
                await LoadOnCallAbsenceIdsAsync());

            foreach (var (clientId, hours) in calculatedHours)
            {
                result[clientId] = new PeriodHoursResource
                {
                    Hours = hours.Hours,
                    Surcharges = hours.Surcharges,
                    GuaranteedHours = TargetOf(clientId)
                };
            }
        }

        return result;
    }

    public async Task<PeriodHoursResource> CalculatePeriodHoursAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken = null)
    {
        var results = await CalculatePeriodHoursForClientsAsync(
            new List<Guid> { clientId },
            startDate,
            endDate,
            analyseToken,
            await LoadOnCallAbsenceIdsAsync());

        var effectiveData = await _contractDataProvider.GetEffectiveContractDataAsync(clientId, startDate);
        var membershipWindows = await MembershipWindowQuery.LoadAsync(_context, [clientId], CancellationToken.None);
        var guaranteedHours = MembershipTargetProration.ProratedTarget(
            effectiveData.GuaranteedHours, membershipWindows.GetValueOrDefault(clientId), startDate, endDate);

        if (results.TryGetValue(clientId, out var hours))
        {
            hours.GuaranteedHours = guaranteedHours;
            return hours;
        }

        return new PeriodHoursResource
        {
            Hours = 0m,
            Surcharges = 0m,
            GuaranteedHours = guaranteedHours
        };
    }

    public async Task<Dictionary<Guid, decimal>> CalculateHoursForClientsAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken = null)
    {
        if (clientIds.Count == 0)
        {
            return new Dictionary<Guid, decimal>();
        }

        var results = await CalculatePeriodHoursForClientsAsync(
            clientIds.Distinct().ToList(),
            startDate,
            endDate,
            analyseToken,
            await LoadOnCallAbsenceIdsAsync());

        return results.ToDictionary(entry => entry.Key, entry => entry.Value.Hours);
    }

    public async Task RecalculatePeriodHoursAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken = null,
        bool includeFrozen = false)
    {
        _logger.LogDebug(
            "Recalculating period hours for client {ClientId} from {StartDate} to {EndDate} (scenario={Token})",
            clientId,
            startDate,
            endDate,
            analyseToken);

        if (!includeFrozen && await IsFrozenAsync(clientId, startDate, endDate, analyseToken))
        {
            return;
        }

        var calculated = await CalculatePeriodHoursAsync(clientId, startDate, endDate, analyseToken);

        await StoreCalculatedPeriodHoursAsync(clientId, startDate, endDate, analyseToken, calculated, includeFrozen);
    }

    private async Task<bool> IsFrozenAsync(Guid clientId, DateOnly startDate, DateOnly endDate, Guid? analyseToken)
    {
        var frozen = await _frozenPeriodPolicy.GetFrozenClientIdsAsync([clientId], startDate, endDate, analyseToken);
        if (frozen.Contains(clientId))
        {
            _logger.LogDebug(
                "Period {StartDate} to {EndDate} of client {ClientId} is frozen (sealed and exported); cache left unchanged",
                startDate,
                endDate,
                clientId);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Stages the removal of the clients' cached rows that overlap the pay period with other boundaries (rows left behind
    /// by an earlier, differently cut window). Rows of other, non-overlapping pay periods stay, and so do frozen rows
    /// unless <paramref name="includeFrozen"/> is set. Saved by the caller.
    /// </summary>
    /// <param name="clientIds">The clients whose rows of this pay period are about to be written</param>
    /// <param name="startDate">First day of the real pay period</param>
    /// <param name="endDate">Last day of the real pay period</param>
    /// <param name="analyseToken">Scenario token of the rows, null for the real plan</param>
    /// <param name="includeFrozen">Admin override: frozen rows are removed as well</param>
    private async Task StageMiscutRowRemovalAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken,
        bool includeFrozen)
    {
        var miscutRows = await _context.ClientPeriodHours
            .Where(p => clientIds.Contains(p.ClientId)
                && p.AnalyseToken == analyseToken
                && p.StartDate <= endDate
                && p.EndDate >= startDate
                && (p.StartDate != startDate || p.EndDate != endDate))
            .ToListAsync();

        IReadOnlyList<ClientPeriodHours> removable = includeFrozen ? miscutRows : await _frozenPeriodPolicy.ExcludeFrozenRowsAsync(miscutRows);
        if (removable.Count > 0)
        {
            _context.ClientPeriodHours.RemoveRange(removable);
        }
    }

    /// <summary>
    /// Upserts the cached row of exactly this pay period and removes only the client's rows that overlap it with other
    /// boundaries (left behind by an earlier, differently cut period). Rows of other, non-overlapping pay periods stay,
    /// because the payroll export reads closed periods only from this cache. The callers check before that this period
    /// itself is not frozen (or the admin override is set).
    /// </summary>
    /// <param name="clientId">The client whose row is written</param>
    /// <param name="startDate">First day of the real pay period</param>
    /// <param name="endDate">Last day of the real pay period</param>
    /// <param name="analyseToken">Scenario token of the row, null for the real plan</param>
    /// <param name="calculated">The freshly calculated hours and surcharges of the period</param>
    /// <param name="includeFrozen">Admin override: overlapping frozen rows are removed as well</param>
    private async Task StoreCalculatedPeriodHoursAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken,
        PeriodHoursResource calculated,
        bool includeFrozen)
    {
        await StageMiscutRowRemovalAsync([clientId], startDate, endDate, analyseToken, includeFrozen);
        var paymentInterval = await GetGlobalPaymentIntervalAsync();

        var existing = await _context.ClientPeriodHours
            .FirstOrDefaultAsync(p =>
                p.ClientId == clientId
                && p.StartDate == startDate
                && p.EndDate == endDate
                && p.AnalyseToken == analyseToken);

        if (existing != null)
        {
            existing.Hours = calculated.Hours;
            existing.Surcharges = calculated.Surcharges;
            existing.PaymentInterval = paymentInterval;
            existing.CalculatedAt = DateTime.UtcNow;
        }
        else
        {
            _context.ClientPeriodHours.Add(new ClientPeriodHours
            {
                Id = Guid.NewGuid(),
                ClientId = clientId,
                StartDate = startDate,
                EndDate = endDate,
                Hours = calculated.Hours,
                Surcharges = calculated.Surcharges,
                PaymentInterval = paymentInterval,
                AnalyseToken = analyseToken,
                CalculatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task<IReadOnlySet<Guid>> RecalculateAllClientsAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? groupId = null,
        Guid? analyseToken = null,
        bool includeFrozen = false)
    {
        _logger.LogInformation(
            "Recalculating period hours for {Scope} from {StartDate} to {EndDate}",
            groupId.HasValue ? $"group {groupId}" : "all clients",
            startDate,
            endDate);

        var memberIds = await LoadMemberIdsAsync(startDate, endDate, groupId);
        var frozenIds = await _frozenPeriodPolicy.GetFrozenClientIdsAsync(memberIds, startDate, endDate, analyseToken);
        var clientIds = includeFrozen ? memberIds : memberIds.Where(id => !frozenIds.Contains(id)).ToList();

        _logger.LogInformation(
            "Found {ClientCount} clients to recalculate ({FrozenCount} with a frozen period, included={IncludeFrozen})",
            clientIds.Count,
            frozenIds.Count,
            includeFrozen);

        var calculatedHours = await CalculatePeriodHoursForClientsAsync(
            clientIds,
            startDate,
            endDate,
            analyseToken,
            await LoadOnCallAbsenceIdsAsync());

        await StageMiscutRowRemovalAsync(clientIds, startDate, endDate, analyseToken, includeFrozen);
        var paymentInterval = await GetGlobalPaymentIntervalAsync();

        var existingEntries = await _context.ClientPeriodHours
            .Where(p => clientIds.Contains(p.ClientId)
                && p.StartDate == startDate
                && p.EndDate == endDate
                && p.AnalyseToken == analyseToken)
            .ToDictionaryAsync(p => p.ClientId);

        foreach (var (clientId, hours) in calculatedHours)
        {
            if (existingEntries.TryGetValue(clientId, out var existing))
            {
                existing.Hours = hours.Hours;
                existing.Surcharges = hours.Surcharges;
                existing.PaymentInterval = paymentInterval;
                existing.CalculatedAt = DateTime.UtcNow;
            }
            else
            {
                var newEntry = new ClientPeriodHours
                {
                    Id = Guid.NewGuid(),
                    ClientId = clientId,
                    StartDate = startDate,
                    EndDate = endDate,
                    Hours = hours.Hours,
                    Surcharges = hours.Surcharges,
                    PaymentInterval = paymentInterval,
                    AnalyseToken = analyseToken,
                    CalculatedAt = DateTime.UtcNow
                };
                _context.ClientPeriodHours.Add(newEntry);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Completed recalculation for {ClientCount} clients",
            calculatedHours.Count);

        return frozenIds;
    }

    public async Task<IReadOnlySet<Guid>> GetFrozenClientIdsAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? groupId = null,
        Guid? analyseToken = null)
    {
        var memberIds = await LoadMemberIdsAsync(startDate, endDate, groupId);
        return await _frozenPeriodPolicy.GetFrozenClientIdsAsync(memberIds, startDate, endDate, analyseToken);
    }

    public async Task<IReadOnlyList<Guid>?> GetRestrictedCallerClientIdsAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? groupId = null)
    {
        if (await _clientGroupFilterService.IsCallerUnrestrictedAsync())
        {
            return null;
        }

        var clientIds = new HashSet<Guid>();
        foreach (var (periodStart, periodEnd) in await GetPeriodsInRangeAsync(startDate, endDate))
        {
            clientIds.UnionWith(await LoadMemberIdsAsync(periodStart, periodEnd, groupId));
        }

        return clientIds.ToList();
    }

    /// <summary>
    /// The non-customer clients (of the group and its subgroups) whose membership overlaps the pay period: the population
    /// of RecalculateAllClientsAsync and GetFrozenClientIdsAsync.
    /// </summary>
    private async Task<List<Guid>> LoadMemberIdsAsync(DateOnly startDate, DateOnly endDate, Guid? groupId)
    {
        var query = _context.Client
            .Where(c => c.Type != EntityTypeEnum.Customer)
            .Where(c => c.Membership != null
                && c.Membership.ValidFrom <= endDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc)
                && (!c.Membership.ValidUntil.HasValue || c.Membership.ValidUntil.Value >= startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc)));

        query = await _clientGroupFilterService.FilterClientsByGroupId(groupId, query);

        return await query
            .Select(c => c.Id)
            .ToListAsync();
    }

    public async Task RefreshCachedPeriodHoursAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        Guid? analyseToken = null)
    {
        var overlappingRows = await _context.ClientPeriodHours
            .Where(p => p.AnalyseToken == analyseToken
                && p.StartDate <= untilDate
                && p.EndDate >= fromDate)
            .ToListAsync();
        var cachedRows = await _frozenPeriodPolicy.ExcludeFrozenRowsAsync(overlappingRows);

        if (cachedRows.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var paymentInterval = await GetGlobalPaymentIntervalAsync();
        var onCallAbsenceIds = await LoadOnCallAbsenceIdsAsync();

        foreach (var period in cachedRows.GroupBy(p => (p.StartDate, p.EndDate)))
        {
            var clientIds = period.Select(p => p.ClientId).Distinct().ToList();
            var calculated = await CalculatePeriodHoursForClientsAsync(
                clientIds,
                period.Key.StartDate,
                period.Key.EndDate,
                analyseToken,
                onCallAbsenceIds);

            foreach (var row in period)
            {
                var hours = calculated[row.ClientId];
                row.Hours = hours.Hours;
                row.Surcharges = hours.Surcharges;
                row.PaymentInterval = paymentInterval;
                row.CalculatedAt = now;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Refreshed {RowCount} cached period-hours rows overlapping {FromDate} to {UntilDate} (scenario={Token})",
            cachedRows.Count,
            fromDate,
            untilDate,
            analyseToken);
    }

    public async Task InvalidateCacheAsync(
        Guid clientId,
        DateOnly date,
        Guid? analyseToken = null)
    {
        var overlappingEntries = await _context.ClientPeriodHours
            .Where(p => p.ClientId == clientId
                && p.AnalyseToken == analyseToken
                && p.StartDate <= date
                && p.EndDate >= date)
            .ToListAsync();
        var affectedEntries = await _frozenPeriodPolicy.ExcludeFrozenRowsAsync(overlappingEntries);

        if (affectedEntries.Count > 0)
        {
            _context.ClientPeriodHours.RemoveRange(affectedEntries);
            await _context.SaveChangesAsync();

            _logger.LogDebug(
                "Invalidated {Count} cached period hours entries for client {ClientId} (scenario={Token})",
                affectedEntries.Count,
                clientId,
                analyseToken);
        }
    }

    /// <summary>
    /// Sums the period hours and surcharges of the clients live: own works (WorkOwnHours), the breaks that count
    /// (BreakOwnHours, so an on-call absence adds nothing) and the work changes that count (WorkChangeOwnHours).
    /// </summary>
    /// <param name="clientIds">The clients to sum</param>
    /// <param name="startDate">First day of the range</param>
    /// <param name="endDate">Last day of the range</param>
    /// <param name="analyseToken">Scenario token of the rows, null for the real plan</param>
    /// <param name="onCallAbsenceIds">Ids of the on-call absence types, loaded once per caller</param>
    private async Task<Dictionary<Guid, PeriodHoursResource>> CalculatePeriodHoursForClientsAsync(
        List<Guid> clientIds,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken,
        IReadOnlyCollection<Guid> onCallAbsenceIds)
    {
        var result = new Dictionary<Guid, PeriodHoursResource>();

        if (clientIds.Count == 0)
        {
            return result;
        }

        var allWorks = await _context.Work
            .Where(w => clientIds.Contains(w.ClientId)
                && w.CurrentDate >= startDate
                && w.CurrentDate <= endDate
                && w.AnalyseToken == analyseToken)
            .Where(WorkOwnHours.Counts)
            .Select(w => new { w.ClientId, w.WorkTime, w.Surcharges, w.CurrentDate })
            .ToListAsync();

        var worksHours = allWorks
            .GroupBy(w => w.ClientId)
            .Select(g => new { ClientId = g.Key, TotalHours = g.Sum(w => w.WorkTime), TotalSurcharges = g.Sum(w => w.Surcharges) })
            .ToList();

        var breaksHours = await _context.Break
            .Where(b => clientIds.Contains(b.ClientId)
                && b.CurrentDate >= startDate
                && b.CurrentDate <= endDate
                && b.AnalyseToken == analyseToken)
            .Where(BreakOwnHours.Counts(onCallAbsenceIds))
            .GroupBy(b => b.ClientId)
            .Select(g => new { ClientId = g.Key, TotalBreaks = g.Sum(b => b.WorkTime), TotalBreakSurcharges = g.Sum(b => b.Surcharges) })
            .ToListAsync();

        var workChanges = await _context.WorkChange
            .Where(wc => (clientIds.Contains(wc.Work!.ClientId) ||
                         (wc.ReplaceClientId.HasValue && clientIds.Contains(wc.ReplaceClientId.Value)))
                && wc.Work.CurrentDate >= startDate
                && wc.Work.CurrentDate <= endDate
                && wc.Work.AnalyseToken == analyseToken)
            .Where(WorkChangeOwnHours.Counts)
            .Select(wc => new
            {
                wc.Work!.ClientId,
                wc.ChangeTime,
                wc.Surcharges,
                wc.Type,
                wc.ReplaceClientId,
                OriginalClientId = wc.Work.ClientId
            })
            .ToListAsync();

        var worksHoursDict = worksHours.ToDictionary(x => x.ClientId, x => (Hours: x.TotalHours, Surcharges: x.TotalSurcharges));
        var breaksHoursDict = breaksHours.ToDictionary(x => x.ClientId, x => (Hours: x.TotalBreaks, Surcharges: x.TotalBreakSurcharges));

        foreach (var clientId in clientIds)
        {
            var workData = worksHoursDict.TryGetValue(clientId, out var wd) ? wd : (Hours: 0m, Surcharges: 0m);
            var breakData = breaksHoursDict.TryGetValue(clientId, out var bd) ? bd : (Hours: 0m, Surcharges: 0m);

            var workChangeHours = 0m;
            var workChangeSurcharges = 0m;

            foreach (var wc in workChanges)
            {
                var isOriginalClient = wc.OriginalClientId == clientId;
                var isReplacementClient = wc.ReplaceClientId == clientId;

                if (CorrectionWorkChangeTypes.IsHourBearing(wc.Type))
                {
                    if (isOriginalClient)
                    {
                        workChangeHours += wc.ChangeTime;
                        workChangeSurcharges += wc.Surcharges;
                    }
                }
                else if (ReplacementWorkChangeTypes.IsReplacement(wc.Type))
                {
                    if (isOriginalClient)
                    {
                        workChangeHours -= wc.ChangeTime;
                        workChangeSurcharges -= wc.Surcharges;
                    }
                    if (isReplacementClient)
                    {
                        workChangeHours += wc.ChangeTime;
                        workChangeSurcharges += wc.Surcharges;
                    }
                }
            }

            result[clientId] = new PeriodHoursResource
            {
                Hours = workData.Hours + breakData.Hours + workChangeHours,
                Surcharges = workData.Surcharges + breakData.Surcharges + workChangeSurcharges,
                GuaranteedHours = 0m
            };
        }

        return result;
    }

    public (DateOnly StartDate, DateOnly EndDate) GetPeriodBoundaries(DateOnly date)
    {
        var startDate = new DateOnly(date.Year, date.Month, 1);
        var endDate = startDate.AddMonths(1).AddDays(-1);
        return (startDate, endDate);
    }

    public async Task<(DateOnly StartDate, DateOnly EndDate)> GetPeriodBoundariesAsync(DateOnly date)
    {
        var paymentInterval = await GetGlobalPaymentIntervalAsync();
        return await CalculatePeriodBoundariesAsync(date, paymentInterval);
    }

    public async Task<IReadOnlyList<(DateOnly StartDate, DateOnly EndDate)>> GetPeriodsInRangeAsync(
        DateOnly fromDate,
        DateOnly untilDate)
    {
        var periods = new List<(DateOnly StartDate, DateOnly EndDate)>();
        if (fromDate > untilDate)
        {
            return periods;
        }

        var paymentInterval = await GetGlobalPaymentIntervalAsync();
        var cursor = fromDate;
        while (true)
        {
            var period = await CalculatePeriodBoundariesAsync(cursor, paymentInterval);
            if (period.EndDate < cursor)
            {
                throw new InvalidOperationException(
                    $"The pay period {period.StartDate}..{period.EndDate} resolved for {cursor} ends before that day.");
            }

            periods.Add(period);
            if (period.EndDate >= untilDate)
            {
                return periods;
            }

            if (periods.Count >= PayPeriodLimits.MaxPeriodsInRange)
            {
                throw new InvalidRequestException(
                    $"The range {fromDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)}.." +
                    $"{untilDate.ToString(DateTimeFormats.IsoDate, CultureInfo.InvariantCulture)} spans more than " +
                    $"{PayPeriodLimits.MaxPeriodsInRange} pay periods.");
            }

            cursor = period.EndDate.AddDays(1);
        }
    }

    public async Task<(DateOnly StartDate, DateOnly EndDate)> GetPeriodBoundariesForContractAsync(
        Guid contractId,
        DateOnly date)
    {
        var contract = await _context.Contract
            .AsNoTracking()
            .Include(c => c.IndividualPeriod!)
                .ThenInclude(i => i.Periods)
            .FirstOrDefaultAsync(c => c.Id == contractId);

        if (contract == null)
        {
            throw new InvalidRequestException($"Contract {contractId} was not found.");
        }

        if (contract.PaymentInterval != PaymentInterval.Individual)
        {
            return await CalculatePeriodBoundariesAsync(date, contract.PaymentInterval);
        }

        if (contract.IndividualPeriod == null)
        {
            throw new InvalidRequestException(
                $"Contract '{contract.Name}' ({contractId}) uses PaymentInterval.Individual but has no " +
                "individual period assigned. Link an individual period or choose a fixed payment interval.");
        }

        return IndividualPeriodBoundaryResolver.Resolve(
            contract.IndividualPeriod.Periods.ToList(),
            date,
            contract.IndividualPeriod.Name);
    }

    private Task<IReadOnlySet<Guid>> LoadOnCallAbsenceIdsAsync()
        => OnCallAbsenceQuery.LoadIdsAsync(_context, CancellationToken.None);

    private async Task<PaymentInterval> GetGlobalPaymentIntervalAsync()
    {
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Type == SettingKeys.PaymentInterval);

        if (setting != null && int.TryParse(setting.Value, out var value))
        {
            return (PaymentInterval)value;
        }

        return PaymentInterval.Monthly;
    }

    private async Task<(DateOnly StartDate, DateOnly EndDate)> CalculatePeriodBoundariesAsync(
        DateOnly date,
        PaymentInterval paymentInterval)
    {
        switch (paymentInterval)
        {
            case PaymentInterval.Weekly:
                var weekStart = await _weekConfiguration.GetWeekStartAsync(date);
                return (weekStart, weekStart.AddDays(6));

            case PaymentInterval.Biweekly:
                var biweeklyWeekStart = await _weekConfiguration.GetWeekStartAsync(date);
                return BiweeklyPayPeriod.Containing(date, biweeklyWeekStart.DayOfWeek);

            case PaymentInterval.Individual:
                // The global setting cannot name an IndividualPeriod, so no custom boundaries exist
                // here. Callers that need them must use GetPeriodBoundariesForContractAsync.
                _logger.LogWarning(
                    "Global PaymentInterval is Individual but individual periods are only defined per contract. " +
                    "Falling back to calendar month boundaries for {Date}. " +
                    "Use GetPeriodBoundariesForContractAsync for contract-specific periods.",
                    date);
                return MonthBoundaries(date);

            case PaymentInterval.Monthly:
            default:
                return MonthBoundaries(date);
        }
    }

    private static (DateOnly StartDate, DateOnly EndDate) MonthBoundaries(DateOnly date)
    {
        var monthStart = new DateOnly(date.Year, date.Month, 1);
        return (monthStart, monthStart.AddMonths(1).AddDays(-1));
    }

    public async Task<PeriodHoursResource> RecalculateAndNotifyAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken,
        string? excludeConnectionId = null)
    {
        _logger.LogDebug(
            "Recalculating period hours for client {ClientId} for period {StartDate} to {EndDate} (scenario={Token})",
            clientId,
            startDate,
            endDate,
            analyseToken);

        if (await IsFrozenAsync(clientId, startDate, endDate, analyseToken))
        {
            var cached = await GetPeriodHoursAsync([clientId], startDate, endDate, analyseToken);
            return cached[clientId];
        }

        var calculated = await CalculatePeriodHoursAsync(clientId, startDate, endDate, analyseToken);

        await StoreCalculatedPeriodHoursAsync(clientId, startDate, endDate, analyseToken, calculated, includeFrozen: false);

        var notification = new PeriodHoursNotificationDto
        {
            ClientId = clientId,
            StartDate = startDate,
            EndDate = endDate,
            Hours = calculated.Hours,
            Surcharges = calculated.Surcharges,
            GuaranteedHours = calculated.GuaranteedHours,
            SourceConnectionId = excludeConnectionId ?? string.Empty,
            AnalyseToken = analyseToken
        };

        await _notificationService.NotifyPeriodHoursUpdated(notification);

        return calculated;
    }
}