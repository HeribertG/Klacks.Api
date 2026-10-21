// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// State of one range check while it walks the clients of the period: the inputs resolved once for the whole
/// range, the findings and collisions collected so far and the remaining travel-time lookup budget.
/// </summary>
/// <param name="ShiftAddresses">Location per shift id, loaded once for the distinct shifts of the range</param>
/// <param name="TravelLookupBudget">Routing lookups the remaining clients may still spend</param>
/// <param name="ClientsWithoutTravelCheck">Clients skipped because the budget was exhausted before them</param>

using Klacks.Api.Application.DTOs.Notifications;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Services.Schedules;

namespace Klacks.Api.Infrastructure.Services.ScheduleTimeline;

public sealed class ScheduleTimelineRangeRun
{
    public required DateOnly StartDate { get; init; }

    public required DateOnly EndDate { get; init; }

    public required Guid? AnalyseToken { get; init; }

    public required Dictionary<Guid, string> ClientNames { get; init; }

    public required IReadOnlyDictionary<Guid, SchedulingPolicy> Policies { get; init; }

    public required Dictionary<Guid, Address?> ShiftAddresses { get; init; }

    public required bool TravelApiKeyConfigured { get; init; }

    public required HolidayWorkSpillIn SpillIn { get; init; }

    public required IReadOnlySet<Guid> OnCallBreakIds { get; init; }

    public List<ScheduleValidationNotificationDto> Entries { get; } = [];

    public List<CollisionNotificationDto> Collisions { get; } = [];

    public int TravelLookupBudget { get; set; }

    public int ClientsWithoutTravelCheck { get; set; }
}
