using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

public interface IShiftSearchService
{
    IQueryable<Shift> ApplySearchFilter(IQueryable<Shift> query, string searchString, bool includeClient);
    
    IQueryable<Shift> ApplyKeywordSearch(IQueryable<Shift> query, string[] keywords, bool includeClient);
    
    IQueryable<Shift> ApplyFirstSymbolSearch(IQueryable<Shift> query, string symbol);
}