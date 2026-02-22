using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;

namespace Klacks.Api.Domain.Services.Clients;

public class ClientWorkFilterService : IClientWorkFilterService
{
    public IQueryable<Client> FilterByMembershipYearMonth(IQueryable<Client> query, int year, int month)
    {
        var (startDate, endDate) = DateRangeUtility.GetMonthRange(year, month + 1);

        return query.Where(co =>
            co.Membership!.ValidFrom.Date <= startDate &&
            (co.Membership.ValidUntil.HasValue == false ||
            (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date > endDate)));
    }
}
