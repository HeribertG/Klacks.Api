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
                                      b.CurrentDate.Date >= startDate && b.CurrentDate.Date <= endDate)
                                .OrderBy(b => b.CurrentDate)
                                .ToList();

        var worksByClientId = works.ToLookup(w => w.ClientId);

        // Assign works to clients
        foreach (var client in clients)
        {
            client.Works = worksByClientId.Contains(client.Id) ? worksByClientId[client.Id].ToList() : new List<Work>();
        }

        return clients.AsQueryable();
    }


}