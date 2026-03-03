// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public class BreakMacroService : IBreakMacroService
{
    private readonly ILogger<BreakMacroService> _logger;

    public BreakMacroService(ILogger<BreakMacroService> logger)
    {
        _logger = logger;
    }

    public Task ProcessBreakMacroAsync(Break breakEntry)
    {
        try
        {
            CalculateWorkTime(breakEntry);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing Break {BreakId}", breakEntry.Id);
        }

        return Task.CompletedTask;
    }

    private static void CalculateWorkTime(Break breakEntry)
    {
        var start = breakEntry.StartTime.ToTimeSpan();
        var end = breakEntry.EndTime.ToTimeSpan();

        TimeSpan duration;
        if (end >= start)
        {
            duration = end - start;
        }
        else
        {
            duration = TimeSpan.FromHours(24) - start + end;
        }

        breakEntry.WorkTime = (decimal)duration.TotalHours;
    }
}
