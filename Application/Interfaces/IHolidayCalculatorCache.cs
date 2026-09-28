// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Application.Interfaces;

public interface IHolidayCalculatorCache
{
    IHolidaysListCalculator GetOrCreate(Guid calendarSelectionId, int year, Func<IHolidaysListCalculator> factory);
    Task<IHolidaysListCalculator> GetOrCreateAsync(Guid calendarSelectionId, int year, Func<Task<IHolidaysListCalculator>> factory);
    void Invalidate(Guid calendarSelectionId);
    void InvalidateAll();
}
