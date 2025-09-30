using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Presentation.DTOs.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Clients;

public class ClientWorkFilterService : IClientWorkFilterService
{
    public IQueryable<Client> FilterByMembershipYearMonth(IQueryable<Client> query, int year, int month)
    {
        // Note: This uses month + 1 (different from standard month range)
        var (startDate, endDate) = DateRangeUtility.GetMonthRange(year, month + 1);

        return query.Where(co =>
            co.Membership!.ValidFrom.Date <= startDate &&
            (co.Membership.ValidUntil.HasValue == false ||
            (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date > endDate)));
    }

    public IQueryable<Client> FilterByWorkSchedule(IQueryable<Client> query, WorkFilter filter, DataBaseContext context)
    {
        var (startDate, endDate) = DateRangeUtility.GetExtendedMonthRange(
            filter.CurrentYear, 
            filter.CurrentMonth, 
            filter.DayVisibleAfterMonth, 
            filter.DayVisibleAfterMonth);

        // First materialize the clients
        var clients = query.ToList();
        var clientIds = clients.Select(c => c.Id).ToList();

        // Get works for these clients
        var works = context.Work.Where(b => clientIds.Contains(b.ClientId) &&
                                      ((b.From.Date >= startDate && b.From.Date <= endDate) ||
                                      (b.Until.Date >= startDate && b.Until.Date <= endDate) ||
                                      (b.From.Date <= startDate && b.Until.Date >= endDate)))
                                .OrderBy(b => b.From).ThenBy(b => b.Until)
                                .ToList();

        var worksByClientId = works.ToLookup(w => w.ClientId);

        // Assign works to clients
        foreach (var client in clients)
        {
            client.Works = worksByClientId.Contains(client.Id) ? worksByClientId[client.Id].ToList() : new List<Work>();
        }

        return clients.AsQueryable();
    }

    public IQueryable<Client> ApplyBreakYearFilter(IQueryable<Client> query, BreakFilter filter)
    {
        return query;
    }

    public IQueryable<Client> ApplyDateRangeFilter(IQueryable<Client> query, DateTime startDate, DateTime endDate)
    {
        return query.Where(c => c.Works.Any(w => 
            (w.From >= startDate && w.From <= endDate) ||
            (w.Until >= startDate && w.Until <= endDate) ||
            (w.From <= startDate && w.Until >= endDate)));
    }
}