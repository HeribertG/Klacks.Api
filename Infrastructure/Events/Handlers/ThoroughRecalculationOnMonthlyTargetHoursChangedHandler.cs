// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reacts to a committed change of a company-wide monthly target hours row by queueing a thorough
/// recalculation of exactly that calendar month. The month value feeds inheriting and
/// MonthlyTargetHours-interval contracts as well as the absence macros, and clients without a
/// contract, so the window is queued without a client scope; sealed works and breaks are skipped by
/// the recalculation pipeline itself. Runs post-commit and fully defensive: a failure is logged and
/// never affects the committed row change.
/// </summary>
/// <param name="queue">Thorough recalculation queue fed with the affected month window</param>
/// <param name="logger">Logs queued windows and failures</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Events;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Events.Handlers;

public sealed class ThoroughRecalculationOnMonthlyTargetHoursChangedHandler : IDomainEventHandler<MonthlyTargetHoursChangedEvent>
{
    private readonly IThoroughRecalculationQueue _queue;
    private readonly ILogger<ThoroughRecalculationOnMonthlyTargetHoursChangedHandler> _logger;

    public ThoroughRecalculationOnMonthlyTargetHoursChangedHandler(
        IThoroughRecalculationQueue queue,
        ILogger<ThoroughRecalculationOnMonthlyTargetHoursChangedHandler> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    public Task HandleAsync(MonthlyTargetHoursChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        try
        {
            var from = new DateOnly(domainEvent.Year, domainEvent.Month, 1);
            var until = from.AddMonths(1).AddDays(-1);

            _queue.QueueRecalculation(from, until, null, null);
            _logger.LogInformation(
                "Monthly target hours for {Year}-{Month:00} changed: queued thorough recalculation for {From}..{Until}",
                domainEvent.Year,
                domainEvent.Month,
                from,
                until);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to queue thorough recalculation for changed monthly target hours {Year}-{Month:00}; the row change is committed and remains unaffected",
                domainEvent.Year,
                domainEvent.Month);
        }

        return Task.CompletedTask;
    }
}
