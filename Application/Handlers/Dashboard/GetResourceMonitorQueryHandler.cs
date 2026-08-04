// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handler for computing per-day employee headcount for the resource monitor dashboard card.
/// Y-axis unit: Mitarbeiter (MA). Five series per day:
///   Wunsch        = desired daily readiness (rosa gepunktet). Per employee:
///                   24/7 contracts smoothed to min(maxWorkDays, 7)/7 every day;
///                   restricted patterns realistic — 1.0 on flagged days, 0 on rest days.
///   Max           = maximum daily readiness (rot gestrichelt). Same logic with maxConsecutiveDays cap.
///   Total         = total headcount (blau). Count of distinct employees considered active on date
///                   (either has an active contract OR is a non-deleted Employee/ExternEmp client).
///   Dienste       = number of shifts scheduled on date (each shift = 1 employee position).
///                   Container shifts count as 1; sub-shifts referenced via ContainerTemplateItem excluded.
///   Absenzen      = sum of Absence.DefaultValue per active BreakPlaceholder, taken literally as entered
///                   (vacation/sickness include weekends — no FTE weighting, no weekday filter).
/// Per-employee WorkOn pattern + caps resolution rules:
///   • If the employee has at least one active contract on the date AND every active contract has
///     flaggedDays > 0 → use the first such contract's pattern and EffectiveCap (per-contract
///     SchedulingRule override with Settings fallback).
///   • Otherwise (no active contract, or any active contract has flaggedDays == 0 → Mischform) →
///     fall back to Settings.SCHEDULING_DEFAULT_WORK_ON_* and Settings caps.
/// </summary>
/// <param name="readRepository">Read-side repository for contracts, clients, shifts, absences and settings</param>
/// <param name="logger">Logger for error handling via BaseHandler</param>
using System.Globalization;
using Klacks.Api.Application.DTOs.Dashboard;
using Klacks.Api.Application.Handlers;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Queries.Dashboard;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Mediator;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Application.Handlers.Dashboard;

public class GetResourceMonitorQueryHandler : BaseHandler, IRequestHandler<GetResourceMonitorQuery, ResourceMonitorResource>
{
    private readonly IResourceMonitorReadRepository _readRepository;
    private readonly IGroupVisibilityService _groupVisibilityService;

    public GetResourceMonitorQueryHandler(
        IResourceMonitorReadRepository readRepository,
        IGroupVisibilityService groupVisibilityService,
        ILogger<GetResourceMonitorQueryHandler> logger)
        : base(logger)
    {
        _readRepository = readRepository;
        _groupVisibilityService = groupVisibilityService;
    }

    public async Task<ResourceMonitorResource> Handle(GetResourceMonitorQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var startDate = new DateOnly(request.Year, 1, 1);
            var endDate = new DateOnly(request.Year, 12, 31);

            var settings = await ResourceMonitorSettingsReader.ReadAsync(_readRepository, cancellationToken);

            var scope = await _groupVisibilityService.GetVisibilityScopeAsync();
            if (!scope.HasVisibleGroups)
            {
                return new ResourceMonitorResource { DailyData = [] };
            }

            HashSet<Guid>? groupShiftIds = null;
            if (request.GroupId.HasValue)
            {
                if (!scope.IsUnrestricted && !scope.VisibleGroupIds.Contains(request.GroupId.Value))
                {
                    return new ResourceMonitorResource { DailyData = [] };
                }

                groupShiftIds = await _readRepository.GetGroupShiftIds(request.GroupId.Value, cancellationToken);
            }
            else if (!scope.IsUnrestricted)
            {
                groupShiftIds = await _readRepository.GetShiftIdsForGroups(scope.VisibleGroupIds, cancellationToken);
            }

            HashSet<Guid>? groupClientIds = null;
            if (groupShiftIds != null)
            {
                groupClientIds = await _readRepository.GetClientIdsForShiftsInRange(groupShiftIds, startDate, endDate, cancellationToken);
            }

            var contracts = await _readRepository.GetActiveContracts(startDate, endDate, groupClientIds, cancellationToken);

            var contractsByClient = contracts
                .GroupBy(cc => cc.ClientId)
                .ToDictionary(g => g.Key, g => g.ToList());

            var employeeClientIds = await _readRepository.GetEmployeeClientIds(groupClientIds, cancellationToken);

            var allClientIds = new HashSet<Guid>(employeeClientIds);
            foreach (var cc in contracts)
                allClientIds.Add(cc.ClientId);

            var containedShiftIds = await _readRepository.GetContainedShiftIds(cancellationToken);

            var shifts = await _readRepository.GetActiveShifts(startDate, endDate, groupShiftIds, containedShiftIds, cancellationToken);

            var periodStart = startDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var periodEnd   = endDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

            var absences = await _readRepository.GetAbsences(periodStart, periodEnd, groupClientIds, cancellationToken);

            var absenzByDate = new Dictionary<DateOnly, double>();
            foreach (var bp in absences)
            {
                if (bp.DefaultValue <= 0) continue;

                var fromDay = DateOnly.FromDateTime(bp.From);
                var untilDay = DateOnly.FromDateTime(bp.Until);
                if (fromDay < startDate) fromDay = startDate;
                if (untilDay > endDate) untilDay = endDate;

                for (var d = fromDay; d <= untilDay; d = d.AddDays(1))
                {
                    absenzByDate[d] = absenzByDate.GetValueOrDefault(d) + bp.DefaultValue;
                }
            }

            var dailyData = DailyReadinessCalculator.Build(
                startDate,
                endDate,
                allClientIds,
                contractsByClient,
                employeeClientIds,
                shifts,
                absenzByDate,
                settings.DefaultPattern,
                settings.MaxWorkDays,
                settings.MaxConsecutiveDays);

            return new ResourceMonitorResource { DailyData = dailyData };
        }, nameof(Handle));
    }

}
