// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.DTOs.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IPeriodHoursService
{
    Task<Dictionary<Guid, PeriodHoursResource>> GetPeriodHoursAsync(
        List<Guid> clientIds,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken = null);

    Task<PeriodHoursResource> CalculatePeriodHoursAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken = null);

    Task RecalculatePeriodHoursAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken = null);

    Task RecalculateAllClientsAsync(
        DateOnly startDate,
        DateOnly endDate,
        Guid? groupId = null,
        Guid? analyseToken = null);

    Task InvalidateCacheAsync(
        Guid clientId,
        DateOnly date,
        Guid? analyseToken = null);

    Task<PeriodHoursResource> RecalculateAndNotifyAsync(
        Guid clientId,
        DateOnly startDate,
        DateOnly endDate,
        Guid? analyseToken,
        string? excludeConnectionId = null);

    (DateOnly StartDate, DateOnly EndDate) GetPeriodBoundaries(DateOnly date);

    Task<(DateOnly StartDate, DateOnly EndDate)> GetPeriodBoundariesAsync(DateOnly date);
}
