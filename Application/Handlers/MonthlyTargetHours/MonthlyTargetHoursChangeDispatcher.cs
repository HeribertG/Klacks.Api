// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared post-commit dispatch of MonthlyTargetHoursChangedEvent for the create, update and delete
/// handlers. Duplicate months are dispatched once; a dispatch failure is logged and never affects
/// the committed row change.
/// </summary>
/// <param name="eventDispatcher">Dispatcher the events are raised on</param>
/// <param name="logger">Logs dispatch failures</param>
/// <param name="months">Affected (year, month) pairs; an update supplies old and new month</param>

using Klacks.Api.Domain.Events;

namespace Klacks.Api.Application.Handlers.MonthlyTargetHours;

public static class MonthlyTargetHoursChangeDispatcher
{
    public static async Task DispatchAsync(
        IDomainEventDispatcher eventDispatcher, ILogger logger, params (int Year, int Month)[] months)
    {
        foreach (var (year, month) in months.Distinct())
        {
            try
            {
                await eventDispatcher.DispatchAsync(new MonthlyTargetHoursChangedEvent(year, month), CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Post-commit dispatch of {EventName} failed for {Year}-{Month:00}; the row change is persisted and remains unaffected.",
                    nameof(MonthlyTargetHoursChangedEvent),
                    year,
                    month);
            }
        }
    }
}
