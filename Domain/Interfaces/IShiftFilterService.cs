using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces;

public interface IShiftFilterService
{
    IQueryable<Shift> ApplyAllFilters(IQueryable<Shift> query, ShiftFilter filter);
}