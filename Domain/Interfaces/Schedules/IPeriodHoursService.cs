// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IPeriodHoursService
{
    Task<Dictionary<Guid, PeriodHoursResource>> GetPeriodHoursAsync(
        List<Guid> clientIds,
        DateOnly startDate,
        DateOnly endDate);

    Task<PeriodHoursResource> CalculatePeriodHoursAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate);

    Task RecalculatePeriodHoursAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate);

    Task RecalculateAllClientsAsync(
        DateOnly startDate,
        DateOnly endDate);

    Task InvalidateCacheAsync(
        Guid clientId,
        DateOnly date);

    Task<PeriodHoursResource> RecalculateAndNotifyAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        string? excludeConnectionId = null);

    (DateOnly StartDate, DateOnly EndDate) GetPeriodBoundaries(DateOnly date);

    Task<(DateOnly StartDate, DateOnly EndDate)> GetPeriodBoundariesAsync(DateOnly date);
}
