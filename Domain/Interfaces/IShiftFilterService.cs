using Klacks.Api.Models.Schedules;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces;

public interface IShiftFilterService
{
    IQueryable<Shift> ApplyAllFilters(IQueryable<Shift> query, ShiftFilter filter);
}