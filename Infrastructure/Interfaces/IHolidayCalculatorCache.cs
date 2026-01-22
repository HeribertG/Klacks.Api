using Klacks.Api.Domain.Interfaces;

namespace Klacks.Api.Infrastructure.Interfaces;

public interface IHolidayCalculatorCache
{
    IHolidaysListCalculator GetOrCreate(Guid calendarSelectionId, int year, Func<IHolidaysListCalculator> factory);
    void Invalidate(Guid calendarSelectionId);
    void InvalidateAll();
}
