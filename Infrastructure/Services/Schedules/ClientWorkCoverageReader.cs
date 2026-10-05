// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IClientWorkCoverageReader"/>. Loads the person's timeline for the day before and the day itself
/// through ClientTimelineLoader and keeps the working blocks whose covered days (WorkedCalendarDates) include the
/// date - the same rule the holiday-work warning applies, so the diagnosis lists exactly the works the warning sees.
/// </summary>
/// <param name="dbContext">Scoped database context</param>
/// <param name="timelineCalculationService">Converts works and work changes into schedule blocks</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Services.Schedules;
using Klacks.Api.Infrastructure.Persistence;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public sealed class ClientWorkCoverageReader : IClientWorkCoverageReader
{
    private readonly DataBaseContext _dbContext;
    private readonly ITimelineCalculationService _timelineCalculationService;

    public ClientWorkCoverageReader(DataBaseContext dbContext, ITimelineCalculationService timelineCalculationService)
    {
        _dbContext = dbContext;
        _timelineCalculationService = timelineCalculationService;
    }

    public async Task<IReadOnlyList<HolidayOutcomeWork>> GetWorksTouchingAsync(
        Guid clientId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var timeline = await ClientTimelineLoader.LoadAsync(
            _dbContext, _timelineCalculationService, clientId, date.AddDays(-1), date, null, cancellationToken);

        return timeline.Blocks
            .Where(WorkedCalendarDates.IsWork)
            .Where(block => WorkedCalendarDates.FromBlocks([block]).Contains(date))
            .Select(block => new HolidayOutcomeWork(
                DateOnly.FromDateTime(block.CalendarStart),
                TimeOnly.FromDateTime(block.CalendarStart),
                TimeOnly.FromDateTime(block.CalendarEnd),
                DateOnly.FromDateTime(block.CalendarStart) < date))
            .ToList();
    }
}
