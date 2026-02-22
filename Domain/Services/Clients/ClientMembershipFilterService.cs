using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Domain.Services.Clients;

public class ClientMembershipFilterService : IClientMembershipFilterService
{

    public IQueryable<Client> ApplyMembershipFilter(IQueryable<Client> query, bool activeMembership, bool formerMembership, bool futureMembership)
    {
        if (activeMembership && formerMembership && futureMembership)
        {
            return query;
        }

        var nowDate = DateTime.Now;

        if (activeMembership && !formerMembership && !futureMembership)
        {
            return query.Where(co =>
                            co.Membership!.ValidFrom.Date <= nowDate &&
                            (co.Membership.ValidUntil.HasValue == false ||
                            (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date >= nowDate)
                            ));
        }

        if (!activeMembership && formerMembership && !futureMembership)
        {
            return query.Where(co =>
                           (co.Membership!.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date < nowDate));
        }

        if (!activeMembership && !formerMembership && futureMembership)
        {
            return query.Where(co =>
                           (co.Membership!.ValidFrom.Date > nowDate));
        }

        if (activeMembership && formerMembership && !futureMembership)
        {
            return query.Where(co =>
                            co.Membership!.ValidFrom.Date <= nowDate &&
                            (co.Membership.ValidUntil.HasValue == false ||
                            (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date > nowDate) ||
                            co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date < nowDate));
        }

        if (activeMembership && !formerMembership && futureMembership)
        {
            return query.Where(co =>
                             (co.Membership!.ValidFrom.Date <= nowDate &&
                             (co.Membership.ValidUntil.HasValue == false ||
                             (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date > nowDate)) ||
                             (co.Membership.ValidFrom.Date > nowDate)));
        }

        if (!activeMembership && formerMembership && futureMembership)
        {
            return query.Where(co =>
                          (co.Membership!.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date < nowDate) ||
                          (co.Membership.ValidFrom.Date > nowDate));
        }

        return query;
    }

    public IQueryable<Client> ApplyScopeFilter(IQueryable<Client> query, bool? scopeFromFlag, bool? scopeUntilFlag, DateTime? scopeFrom, DateTime? scopeUntil)
    {
        if (scopeFromFlag.HasValue && scopeFromFlag.Value && scopeFrom.HasValue)
        {
            query = query.Where(co => co.Membership!.ValidFrom.Date >= scopeFrom.Value.Date);
        }

        if (scopeUntilFlag.HasValue && scopeUntilFlag.Value && scopeUntil.HasValue)
        {
            query = query.Where(co => co.Membership!.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date <= scopeUntil.Value.Date);
        }

        return query;
    }

    public IQueryable<Client> ApplyMembershipYearFilter(IQueryable<Client> query, BreakFilter filter)
    {
        var (startDate, endDate) = DateRangeUtility.GetYearRange(filter.CurrentYear);

        return query.Where(c => c.Membership!.ValidFrom < endDate &&
                               (!c.Membership.ValidUntil.HasValue || c.Membership.ValidUntil.Value >= startDate));
    }


    public bool IsActiveMembership(DateTime validFrom, DateTime? validUntil)
    {
        var nowDate = DateTime.Now;
        return validFrom.Date <= nowDate &&
               (!validUntil.HasValue || validUntil.Value.Date >= nowDate);
    }

    public bool IsFormerMembership(DateTime validFrom, DateTime? validUntil)
    {
        var nowDate = DateTime.Now;
        return validUntil.HasValue && validUntil.Value.Date < nowDate;
    }

    public bool IsFutureMembership(DateTime validFrom, DateTime? validUntil)
    {
        var nowDate = DateTime.Now;
        return validFrom.Date > nowDate;
    }
}