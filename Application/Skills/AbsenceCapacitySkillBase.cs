// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared groundwork for the two absence-capacity skills: reading the utilization ceiling,
/// resolving the daily weight of a request and loading the per-day capacity picture. Both skills
/// sit on this base so that judging a given period and searching for a fitting one can never
/// disagree about the threshold, the unit or the data they look at.
/// </summary>
/// <param name="mediator">Sends the resource-monitor query</param>
/// <param name="absenceRepository">Resolves an absence type to its DefaultValue</param>
/// <param name="settingsRepository">Reads the configured utilization ceiling</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Skills;

public abstract class AbsenceCapacitySkillBase : BaseSkillImplementation
{
    protected const double DefaultDailyValue = 1.0;
    protected const int MaxRangeDays = 400;

    private const int ContextDays = 7;

    private readonly IMediator _mediator;
    private readonly IAbsenceRepository _absenceRepository;
    private readonly ISettingsRepository _settingsRepository;

    protected AbsenceCapacitySkillBase(
        IMediator mediator,
        IAbsenceRepository absenceRepository,
        ISettingsRepository settingsRepository)
    {
        _mediator = mediator;
        _absenceRepository = absenceRepository;
        _settingsRepository = settingsRepository;
    }

    protected async Task<(double Value, string? Error)> ResolveDailyValueAsync(Dictionary<string, object> parameters)
    {
        var absenceTypeIdText = GetParameter<string>(parameters, "absenceTypeId");
        if (!string.IsNullOrWhiteSpace(absenceTypeIdText))
        {
            if (!Guid.TryParse(absenceTypeIdText, out var absenceTypeId))
            {
                return (0, $"Invalid absenceTypeId: {absenceTypeIdText}.");
            }

            var absences = await _absenceRepository.List();
            var absence = absences.FirstOrDefault(a => a.Id == absenceTypeId && !a.IsDeleted);
            if (absence == null)
            {
                return (0, $"Unknown absenceTypeId: {absenceTypeId}. Use list_absence_types to find the correct one.");
            }

            return (absence.DefaultValue, null);
        }

        var explicitValue = GetParameter<double?>(parameters, "dailyValue");
        if (explicitValue.HasValue)
        {
            return explicitValue.Value <= 0
                ? (0, "dailyValue must be greater than zero.")
                : (explicitValue.Value, null);
        }

        return (DefaultDailyValue, null);
    }

    protected async Task<double> ReadMaxUtilizationAsync()
    {
        var setting = await _settingsRepository.GetSettingNoTracking(
            Constants.Settings.SCHEDULING_MAX_CAPACITY_UTILIZATION);

        return CapacityUtilizationCeiling.Parse(setting?.Value);
    }

    protected async Task<List<CapacityDay>> LoadCapacityDaysAsync(
        DateOnly from,
        DateOnly until,
        Guid? groupId,
        CancellationToken cancellationToken)
    {
        var windowStart = from.AddDays(-ContextDays);
        var windowEnd = until.AddDays(ContextDays);

        var days = new List<CapacityDay>();
        for (var year = windowStart.Year; year <= windowEnd.Year; year++)
        {
            var monitor = await _mediator.Send(new GetResourceMonitorQuery(year, groupId), cancellationToken);
            if (monitor.DailyData == null)
            {
                continue;
            }

            days.AddRange(monitor.DailyData
                .Where(d => d.Date >= windowStart && d.Date <= windowEnd)
                .Select(d => new CapacityDay(d.Date, d.WunschCount, d.DienstCount, d.AbsenzCount)));
        }

        return days;
    }

    protected static (Guid? GroupId, string? Error) ParseGroupId(Dictionary<string, object> parameters)
    {
        var groupIdText = GetParameter<string>(parameters, "groupId");
        if (string.IsNullOrWhiteSpace(groupIdText))
        {
            return (null, null);
        }

        return Guid.TryParse(groupIdText, out var parsed)
            ? (parsed, null)
            : (null, $"Invalid groupId: {groupIdText}.");
    }

    protected static double ToPercent(double ratio) => CapacityUtilizationCeiling.ToPercent(ratio);
}
