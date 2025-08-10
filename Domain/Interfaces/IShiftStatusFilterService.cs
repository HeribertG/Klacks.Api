using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces;

public interface IShiftStatusFilterService
{
    IQueryable<Shift> ApplyStatusFilter(IQueryable<Shift> query, bool isOriginal);
}