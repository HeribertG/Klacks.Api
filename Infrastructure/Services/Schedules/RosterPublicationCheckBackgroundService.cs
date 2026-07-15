// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// K15: periodically warns when a Work row is still below WorkLockLevel.Approved ("published") with
/// less than COMPLIANCE_ROSTER_PUBLICATION_MIN_LEAD_DAYS lead time before its shift date. Inert (a
/// cheap no-op tick) on any installation that never configures a positive lead time, so it can ship
/// on by default. Log-only in this first stage — no notification/audit entity is introduced.
/// Known simplification: COMPLIANCE_ROSTER_PUBLICATION_COUNT_WORKDAYS_ONLY excludes the configured
/// weekend days only; public holidays are not excluded yet (would require resolving a calendar
/// selection per client/group, deferred to a follow-up).
/// </summary>
/// <param name="scopeFactory">Factory for creating DI scopes in background tasks</param>
/// <param name="logger">Logs one warning per Work row that misses its publication deadline</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class RosterPublicationCheckBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private static readonly TimeSpan InitialDelay = TimeSpan.FromMinutes(5);
    private const int MaxLookaheadDays = 60;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RosterPublicationCheckBackgroundService> _logger;

    public RosterPublicationCheckBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<RosterPublicationCheckBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Roster publication check background service started");

        try
        {
            await Task.Delay(InitialDelay, stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckPublicationDeadlinesAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in roster publication check background service");
                }

                await Task.Delay(Interval, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        _logger.LogInformation("Roster publication check background service stopped");
    }

    private async Task CheckPublicationDeadlinesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var settingsReader = scope.ServiceProvider.GetRequiredService<ISettingsReader>();

        var minLeadDays = await ReadIntAsync(settingsReader, SettingKeys.ComplianceRosterPublicationMinLeadDays);
        if (minLeadDays <= 0)
        {
            return;
        }

        var countWorkdaysOnly = await ReadBoolAsync(settingsReader, SettingKeys.ComplianceRosterPublicationCountWorkdaysOnly);
        var weekConfiguration = scope.ServiceProvider.GetRequiredService<IWeekConfiguration>();
        var context = scope.ServiceProvider.GetRequiredService<DataBaseContext>();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizon = today.AddDays(MaxLookaheadDays);

        var candidates = await context.Work
            .AsNoTracking()
            .Where(w => !w.IsDeleted
                && w.AnalyseToken == null
                && w.CurrentDate >= today
                && w.CurrentDate <= horizon
                && w.LockLevel < WorkLockLevel.Approved)
            .Select(w => new { w.Id, w.ClientId, w.ShiftId, w.CurrentDate, w.LockLevel })
            .ToListAsync(cancellationToken);

        foreach (var work in candidates)
        {
            var leadDays = countWorkdaysOnly
                ? await CountWorkdaysAsync(today, work.CurrentDate, weekConfiguration, cancellationToken)
                : work.CurrentDate.DayNumber - today.DayNumber;

            if (leadDays < minLeadDays)
            {
                _logger.LogWarning(
                    "Roster publication deadline missed: work {WorkId} (client {ClientId}, shift {ShiftId}) on {Date} " +
                    "is still {LockLevel} with only {LeadDays} day(s) lead time (required {MinLeadDays}).",
                    work.Id,
                    work.ClientId,
                    work.ShiftId,
                    work.CurrentDate,
                    work.LockLevel,
                    leadDays,
                    minLeadDays);
            }
        }
    }

    private static async Task<int> CountWorkdaysAsync(
        DateOnly from, DateOnly to, IWeekConfiguration weekConfiguration, CancellationToken cancellationToken)
    {
        var count = 0;
        for (var date = from; date < to; date = date.AddDays(1))
        {
            if (!await weekConfiguration.IsWeekendAsync(date.DayOfWeek, cancellationToken))
            {
                count++;
            }
        }

        return count;
    }

    private static async Task<int> ReadIntAsync(ISettingsReader settingsReader, string key)
    {
        var setting = await settingsReader.GetSetting(key);
        return int.TryParse(setting?.Value, out var value) ? value : 0;
    }

    private static async Task<bool> ReadBoolAsync(ISettingsReader settingsReader, string key)
    {
        var setting = await settingsReader.GetSetting(key);
        return setting?.Value?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;
    }
}
