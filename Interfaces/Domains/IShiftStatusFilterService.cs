using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Interfaces.Domains;

public interface IShiftStatusFilterService
{
    IQueryable<Shift> ApplyStatusFilter(IQueryable<Shift> query, bool isOriginal);
}