// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Collections.Concurrent;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Infrastructure.Scripting;

public class HolidayCalculatorCache : IHolidayCalculatorCache
{
    private readonly ConcurrentDictionary<(Guid CalendarSelectionId, int Year), IHolidaysListCalculator> _cache = new();

    public IHolidaysListCalculator GetOrCreate(Guid calendarSelectionId, int year, Func<IHolidaysListCalculator> factory)
    {
        var key = (calendarSelectionId, year);
        return _cache.GetOrAdd(key, _ => factory());
    }

    public async Task<IHolidaysListCalculator> GetOrCreateAsync(Guid calendarSelectionId, int year, Func<Task<IHolidaysListCalculator>> factory)
    {
        var key = (calendarSelectionId, year);
        if (_cache.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var calculator = await factory();
        return _cache.GetOrAdd(key, _ => calculator);
    }

    public void Invalidate(Guid calendarSelectionId)
    {
        var keysToRemove = _cache.Keys.Where(k => k.CalendarSelectionId == calendarSelectionId).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public void InvalidateAll()
    {
        _cache.Clear();
    }
}
