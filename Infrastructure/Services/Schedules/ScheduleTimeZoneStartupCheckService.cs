// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Warns once at startup when the schedule collision pipeline's own Schedule:TimeZoneId (a second,
/// independent zone source alongside the company's configured zone - see TimelineCalculationService,
/// a Singleton) has drifted from the company's own configured zone. Only relevant when
/// Schedule:DstAware is actually on, in which case the two zones silently disagreeing would place
/// ScheduleBlock instants in the wrong zone without any error. The cheap config check runs first so an
/// installation that never enabled DstAware never pays for opening a scope. ICompanyClock is Scoped, so
/// it is resolved through a short-lived scope here rather than injected into this Singleton-friendly
/// hosted service's constructor - never inject ICompanyClock directly into a Singleton or hosted
/// service (see CompanyClockCaptiveDependencyGuardTests).
/// </summary>
/// <param name="serviceScopeFactory">Creates the short-lived scope ICompanyClock is resolved through.</param>
/// <param name="scheduleTimeOptions">The Schedule:DstAware / Schedule:TimeZoneId configuration.</param>
/// <param name="logger">Structured log for the startup diagnostic.</param>

using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Domain.Services.Settings;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class ScheduleTimeZoneStartupCheckService : IHostedService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ScheduleTimeOptions _scheduleTimeOptions;
    private readonly ILogger<ScheduleTimeZoneStartupCheckService> _logger;

    public ScheduleTimeZoneStartupCheckService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<ScheduleTimeOptions> scheduleTimeOptions,
        ILogger<ScheduleTimeZoneStartupCheckService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _scheduleTimeOptions = scheduleTimeOptions.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_scheduleTimeOptions.DstAware || string.IsNullOrWhiteSpace(_scheduleTimeOptions.TimeZoneId))
        {
            return;
        }

        try
        {
            await CompareAgainstCompanyZoneAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Schedule:TimeZoneId startup check failed - skipping, never blocking startup");
        }
    }

    private async Task CompareAgainstCompanyZoneAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var companyClock = scope.ServiceProvider.GetRequiredService<ICompanyClock>();
        var companyZone = await companyClock.GetTimeZoneAsync(cancellationToken);
        var companyIanaId = IanaTimeZoneId.From(companyZone);

        if (!IanaTimeZoneId.TryFrom(_scheduleTimeOptions.TimeZoneId, out var scheduleIanaId)
            || !string.Equals(scheduleIanaId, companyIanaId, StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Schedule:DstAware is enabled with Schedule:TimeZoneId '{ScheduleTimeZoneId}', which " +
                "differs from the company's own configured time zone '{CompanyTimeZoneId}'. The schedule " +
                "collision pipeline and the rest of the application will compute business days in " +
                "different zones - align Schedule:TimeZoneId with the company zone unless this is " +
                "intentional.",
                _scheduleTimeOptions.TimeZoneId,
                companyIanaId);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
