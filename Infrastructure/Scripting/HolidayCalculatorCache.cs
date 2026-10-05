// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Process-wide cache of computed holiday calculators per calendar selection and year. Every invalidation bumps
/// a generation counter; a calculator whose factory was still loading while an invalidation ran is returned to
/// its caller but never kept, so rules read before a calendar change cannot stay cached until restart.
/// Store checks the generation twice: before adding (covered by HolidayCalculatorCacheTests) and after adding,
/// for an invalidation that lands between that check and GetOrAdd; the second branch has no deterministic test.
/// </summary>

using System.Collections.Concurrent;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Infrastructure.Scripting;

public class HolidayCalculatorCache : IHolidayCalculatorCache
{
    private readonly ConcurrentDictionary<(Guid CalendarSelectionId, int Year), IHolidaysListCalculator> _cache = new();
    private long _generation;

    public IHolidaysListCalculator GetOrCreate(Guid calendarSelectionId, int year, Func<IHolidaysListCalculator> factory)
    {
        var key = (calendarSelectionId, year);
        if (_cache.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var generation = Interlocked.Read(ref _generation);
        return Store(key, factory(), generation);
    }

    public async Task<IHolidaysListCalculator> GetOrCreateAsync(Guid calendarSelectionId, int year, Func<Task<IHolidaysListCalculator>> factory)
    {
        var key = (calendarSelectionId, year);
        if (_cache.TryGetValue(key, out var existing))
        {
            return existing;
        }

        var generation = Interlocked.Read(ref _generation);
        var calculator = await factory();
        return Store(key, calculator, generation);
    }

    /// <summary>
    /// Drops one selection's calculators. Bumps the global generation, so loads of other selections running at
    /// the same moment are not stored either - a harmless extra cache miss, never a stale entry.
    /// </summary>
    public void Invalidate(Guid calendarSelectionId)
    {
        Interlocked.Increment(ref _generation);
        var keysToRemove = _cache.Keys.Where(k => k.CalendarSelectionId == calendarSelectionId).ToList();
        foreach (var key in keysToRemove)
        {
            _cache.TryRemove(key, out _);
        }
    }

    public void InvalidateAll()
    {
        Interlocked.Increment(ref _generation);
        _cache.Clear();
    }

    private IHolidaysListCalculator Store(
        (Guid CalendarSelectionId, int Year) key,
        IHolidaysListCalculator calculator,
        long generationAtLoadStart)
    {
        if (Interlocked.Read(ref _generation) != generationAtLoadStart)
        {
            return calculator;
        }

        var stored = _cache.GetOrAdd(key, calculator);

        if (Interlocked.Read(ref _generation) != generationAtLoadStart)
        {
            _cache.TryRemove(new KeyValuePair<(Guid CalendarSelectionId, int Year), IHolidaysListCalculator>(key, stored));
        }

        return stored;
    }
}
