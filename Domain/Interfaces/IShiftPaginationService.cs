using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces;

public interface IShiftPaginationService
{
    Task<TruncatedShift> ApplyPaginationAsync(IQueryable<Shift> filteredQuery, ShiftFilter filter);

    int CalculateFirstItem(ShiftFilter filter, int totalCount);
}