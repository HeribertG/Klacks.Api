using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Hubs;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Presentation.DTOs.Notifications;
using Klacks.Api.Presentation.DTOs.Schedules;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.PeriodHours;

public class PeriodHoursService : IPeriodHoursService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<PeriodHoursService> _logger;
    private readonly IWorkNotificationService _notificationService;

    public PeriodHoursService(
        DataBaseContext context,
        ILogger<PeriodHoursService> logger,
        IWorkNotificationService notificationService)
    {
        _context = context;
        _logger = logger;
        _notificationService = notificationService;
    }

    public async Task<Dictionary<Guid, PeriodHoursResource>> GetPeriodHoursAsync(
        List<Guid> clientIds,
        DateOnly startDate,
        DateOnly endDate)
    {
        if (clientIds.Count == 0)
        {
            return new Dictionary<Guid, PeriodHoursResource>();
        }

        var result = new Dictionary<Guid, PeriodHoursResource>();

        var cachedPeriodHours = await _context.ClientPeriodHours
            .Where(p => clientIds.Contains(p.ClientId)
                && p.StartDate == startDate
                && p.EndDate == endDate)
            .ToListAsync();

        var clientIdsWithCache = cachedPeriodHours.Select(p => p.ClientId).ToHashSet();
        var clientIdsWithoutCache = clientIds.Where(id => !clientIdsWithCache.Contains(id)).ToList();

        var contractByClient = await GetContractsByClientAsync(clientIds, startDate);

        foreach (var ph in cachedPeriodHours)
        {
            var guaranteedHours = contractByClient.TryGetValue(ph.ClientId, out var contract)
                ? contract.GuaranteedHours ?? 0m
                : 0m;

            result[ph.ClientId] = new PeriodHoursResource
            {
                Hours = ph.Hours,
                Surcharges = ph.Surcharges,
                GuaranteedHours = guaranteedHours
            };
        }

        if (clientIdsWithoutCache.Count > 0)
        {
            var calculatedHours = await CalculatePeriodHoursForClientsAsync(
                clientIdsWithoutCache,
                startDate,
                endDate);

            foreach (var (clientId, hours) in calculatedHours)
            {
                var guaranteedHours = contractByClient.TryGetValue(clientId, out var contract)
                    ? contract.GuaranteedHours ?? 0m
                    : 0m;

                result[clientId] = new PeriodHoursResource
                {
                    Hours = hours.Hours,
                    Surcharges = hours.Surcharges,
                    GuaranteedHours = guaranteedHours
                };
            }
        }

        return result;
    }

    public async Task<PeriodHoursResource> CalculatePeriodHoursAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate)
    {
        var results = await CalculatePeriodHoursForClientsAsync(
            new List<Guid> { clientId },
            startDate,
            endDate);

        if (results.TryGetValue(clientId, out var hours))
        {
            return hours;
        }

        return new PeriodHoursResource
        {
            Hours = 0m,
            Surcharges = 0m,
            GuaranteedHours = 0m
        };
    }

    public async Task RecalculatePeriodHoursAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate)
    {
        _logger.LogDebug(
            "Recalculating period hours for client {ClientId} from {StartDate} to {EndDate}",
            clientId,
            startDate,
            endDate);

        var calculated = await CalculatePeriodHoursAsync(clientId, startDate, endDate);

        var otherCacheEntries = await _context.ClientPeriodHours
            .Where(p => p.ClientId == clientId
                && (p.StartDate != startDate || p.EndDate != endDate))
            .ToListAsync();

        if (otherCacheEntries.Count > 0)
        {
            _context.ClientPeriodHours.RemoveRange(otherCacheEntries);
        }

        var existing = await _context.ClientPeriodHours
            .FirstOrDefaultAsync(p =>
                p.ClientId == clientId
                && p.StartDate == startDate
                && p.EndDate == endDate);

        if (existing != null)
        {
            existing.Hours = calculated.Hours;
            existing.Surcharges = calculated.Surcharges;
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
                Hours = calculated.Hours,
                Surcharges = calculated.Surcharges,
                CalculatedAt = DateTime.UtcNow
            };
            _context.ClientPeriodHours.Add(newEntry);
        }

        await _context.SaveChangesAsync();
    }

    public async Task RecalculateAllClientsAsync(
        DateOnly startDate,
        DateOnly endDate)
    {
        _logger.LogInformation(
            "Recalculating period hours for all clients from {StartDate} to {EndDate}",
            startDate,
            endDate);

        var clientIds = await _context.Client
            .Where(c => c.Type != EntityTypeEnum.Customer)
            .Where(c => c.Membership != null
                && c.Membership.ValidFrom <= endDate.ToDateTime(TimeOnly.MaxValue)
                && (!c.Membership.ValidUntil.HasValue || c.Membership.ValidUntil.Value >= startDate.ToDateTime(TimeOnly.MinValue)))
            .Select(c => c.Id)
            .ToListAsync();

        _logger.LogInformation("Found {ClientCount} clients to recalculate", clientIds.Count);

        var calculatedHours = await CalculatePeriodHoursForClientsAsync(
            clientIds,
            startDate,
            endDate);

        var existingEntries = await _context.ClientPeriodHours
            .Where(p => clientIds.Contains(p.ClientId)
                && p.StartDate == startDate
                && p.EndDate == endDate)
            .ToDictionaryAsync(p => p.ClientId);

        foreach (var (clientId, hours) in calculatedHours)
        {
            if (existingEntries.TryGetValue(clientId, out var existing))
            {
                existing.Hours = hours.Hours;
                existing.Surcharges = hours.Surcharges;
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
                    CalculatedAt = DateTime.UtcNow
                };
                _context.ClientPeriodHours.Add(newEntry);
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Completed recalculation for {ClientCount} clients",
            calculatedHours.Count);
    }

    public async Task InvalidateCacheAsync(
        Guid clientId,
        DateOnly date)
    {
        var affectedEntries = await _context.ClientPeriodHours
            .Where(p => p.ClientId == clientId
                && p.StartDate <= date
                && p.EndDate >= date)
            .ToListAsync();

        if (affectedEntries.Count > 0)
        {
            _context.ClientPeriodHours.RemoveRange(affectedEntries);
            await _context.SaveChangesAsync();

            _logger.LogDebug(
                "Invalidated {Count} cached period hours entries for client {ClientId}",
                affectedEntries.Count,
                clientId);
        }
    }

    private async Task<Dictionary<Guid, PeriodHoursResource>> CalculatePeriodHoursForClientsAsync(
        List<Guid> clientIds,
        DateOnly startDate,
        DateOnly endDate)
    {
        var result = new Dictionary<Guid, PeriodHoursResource>();

        if (clientIds.Count == 0)
        {
            return result;
        }

        var startDateTime = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var endDateTime = endDate.ToDateTime(new TimeOnly(23, 59, 59), DateTimeKind.Utc);

        var allWorks = await _context.Work
            .Where(w => clientIds.Contains(w.ClientId)
                && w.CurrentDate >= startDateTime
                && w.CurrentDate <= endDateTime)
            .Select(w => new { w.ClientId, w.WorkTime, w.Surcharges, w.CurrentDate })
            .ToListAsync();

        _logger.LogInformation(
            "CalculatePeriodHours for clients {ClientIds} from {Start} to {End}: Found {Count} works",
            string.Join(", ", clientIds),
            startDate,
            endDate,
            allWorks.Count);

        foreach (var work in allWorks)
        {
            _logger.LogInformation(
                "  Work: Client={ClientId}, Date={Date}, Hours={Hours}, Surcharges={Surcharges}",
                work.ClientId,
                work.CurrentDate,
                work.WorkTime,
                work.Surcharges);
        }

        var worksHours = allWorks
            .GroupBy(w => w.ClientId)
            .Select(g => new { ClientId = g.Key, TotalHours = g.Sum(w => w.WorkTime), TotalSurcharges = g.Sum(w => w.Surcharges) })
            .ToList();

        var breaksHours = await _context.Break
            .Where(b => clientIds.Contains(b.ClientId)
                && b.CurrentDate >= startDateTime
                && b.CurrentDate <= endDateTime)
            .GroupBy(b => b.ClientId)
            .Select(g => new { ClientId = g.Key, TotalBreaks = g.Sum(b => b.WorkTime) })
            .ToListAsync();

        var workChanges = await _context.WorkChange
            .Where(wc => clientIds.Contains(wc.Work!.ClientId)
                && wc.Work.CurrentDate >= startDateTime
                && wc.Work.CurrentDate <= endDateTime)
            .Select(wc => new
            {
                wc.Work!.ClientId,
                wc.ChangeTime,
                wc.Type,
                wc.ToInvoice,
                wc.ReplaceClientId,
                OriginalClientId = wc.Work.ClientId
            })
            .ToListAsync();

        var worksHoursDict = worksHours.ToDictionary(x => x.ClientId, x => (Hours: x.TotalHours, Surcharges: x.TotalSurcharges));
        var breaksHoursDict = breaksHours.ToDictionary(x => x.ClientId, x => x.TotalBreaks);

        foreach (var clientId in clientIds)
        {
            var workData = worksHoursDict.TryGetValue(clientId, out var wd) ? wd : (Hours: 0m, Surcharges: 0m);
            var breaks = breaksHoursDict.TryGetValue(clientId, out var b) ? b : 0m;

            var workChangeHours = 0m;
            var workChangeSurcharges = 0m;

            foreach (var wc in workChanges)
            {
                if (wc.ToInvoice && wc.OriginalClientId == clientId)
                {
                    workChangeSurcharges += wc.ChangeTime;
                }

                var isOriginalClient = wc.OriginalClientId == clientId;
                var isReplacementClient = wc.ReplaceClientId == clientId;

                if (wc.Type == WorkChangeType.CorrectionEnd || wc.Type == WorkChangeType.CorrectionStart)
                {
                    if (isOriginalClient) workChangeHours += wc.ChangeTime;
                }
                else if (wc.Type == WorkChangeType.ReplacementStart || wc.Type == WorkChangeType.ReplacementEnd)
                {
                    if (isOriginalClient) workChangeHours -= wc.ChangeTime;
                    if (isReplacementClient) workChangeHours += wc.ChangeTime;
                }
            }

            result[clientId] = new PeriodHoursResource
            {
                Hours = workData.Hours + breaks + workChangeHours,
                Surcharges = workData.Surcharges + workChangeSurcharges,
                GuaranteedHours = 0m
            };
        }

        return result;
    }

    private async Task<Dictionary<Guid, Domain.Models.Associations.Contract>> GetContractsByClientAsync(
        List<Guid> clientIds,
        DateOnly refDate)
    {
        var contractData = await _context.ClientContract
            .Where(cc => clientIds.Contains(cc.ClientId)
                && cc.FromDate <= refDate
                && (cc.UntilDate == null || cc.UntilDate >= refDate))
            .Include(cc => cc.Contract)
            .ToListAsync();

        return contractData
            .GroupBy(cc => cc.ClientId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(cc => cc.FromDate).First().Contract);
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
        return CalculatePeriodBoundaries(date, paymentInterval);
    }

    private async Task<PaymentInterval> GetGlobalPaymentIntervalAsync()
    {
        var setting = await _context.Settings
            .FirstOrDefaultAsync(s => s.Type == "paymentInterval");

        if (setting != null && int.TryParse(setting.Value, out var value))
        {
            return (PaymentInterval)value;
        }

        return PaymentInterval.Monthly;
    }

    private (DateOnly StartDate, DateOnly EndDate) CalculatePeriodBoundaries(
        DateOnly date,
        PaymentInterval paymentInterval)
    {
        switch (paymentInterval)
        {
            case PaymentInterval.Weekly:
                var weekStart = date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday);
                if (date.DayOfWeek == DayOfWeek.Sunday)
                    weekStart = weekStart.AddDays(-7);
                return (weekStart, weekStart.AddDays(6));

            case PaymentInterval.Biweekly:
                var biweekStart = date.AddDays(-(int)date.DayOfWeek + (int)DayOfWeek.Monday);
                if (date.DayOfWeek == DayOfWeek.Sunday)
                    biweekStart = biweekStart.AddDays(-7);
                var weekNumber = System.Globalization.ISOWeek.GetWeekOfYear(date.ToDateTime(TimeOnly.MinValue));
                if (weekNumber % 2 == 0)
                    biweekStart = biweekStart.AddDays(-7);
                return (biweekStart, biweekStart.AddDays(13));

            case PaymentInterval.Monthly:
            default:
                var monthStart = new DateOnly(date.Year, date.Month, 1);
                var monthEnd = monthStart.AddMonths(1).AddDays(-1);
                return (monthStart, monthEnd);
        }
    }

    public async Task<PeriodHoursResource> RecalculateAndNotifyAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        string? excludeConnectionId = null)
    {
        _logger.LogDebug(
            "Recalculating period hours for client {ClientId} for period {StartDate} to {EndDate}",
            clientId,
            startDate,
            endDate);

        var calculated = await CalculatePeriodHoursAsync(clientId, startDate, endDate);

        var otherCacheEntries = await _context.ClientPeriodHours
            .Where(p => p.ClientId == clientId
                && (p.StartDate != startDate || p.EndDate != endDate))
            .ToListAsync();

        if (otherCacheEntries.Count > 0)
        {
            _context.ClientPeriodHours.RemoveRange(otherCacheEntries);
        }

        var existing = await _context.ClientPeriodHours
            .FirstOrDefaultAsync(p =>
                p.ClientId == clientId
                && p.StartDate == startDate
                && p.EndDate == endDate);

        if (existing != null)
        {
            existing.Hours = calculated.Hours;
            existing.Surcharges = calculated.Surcharges;
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
                Hours = calculated.Hours,
                Surcharges = calculated.Surcharges,
                CalculatedAt = DateTime.UtcNow
            };
            _context.ClientPeriodHours.Add(newEntry);
        }

        await _context.SaveChangesAsync();

        var notification = new PeriodHoursNotificationDto
        {
            ClientId = clientId,
            StartDate = startDate,
            EndDate = endDate,
            Hours = calculated.Hours,
            Surcharges = calculated.Surcharges,
            GuaranteedHours = calculated.GuaranteedHours,
            SourceConnectionId = excludeConnectionId ?? string.Empty
        };

        await _notificationService.NotifyPeriodHoursUpdated(notification);

        return calculated;
    }
}
