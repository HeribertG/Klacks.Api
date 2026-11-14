// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Domain.DTOs.Filter;

namespace Klacks.Api.Infrastructure.Services.Clients;

/// <summary>
/// Filters clients by membership state. The last validity day is inclusive: a membership whose ValidUntil is today is still active.
/// </summary>
/// <param name="companyClock">Supplies the company's current calendar date</param>
public class ClientMembershipFilterService : IClientMembershipFilterService
{
    private readonly ICompanyClock _companyClock;

    public ClientMembershipFilterService(ICompanyClock companyClock)
    {
        _companyClock = companyClock;
    }

    public async Task<IQueryable<Client>> ApplyMembershipFilter(IQueryable<Client> query, bool activeMembership, bool formerMembership, bool futureMembership)
    {
        if (activeMembership && formerMembership && futureMembership)
        {
            return query;
        }

        var nowDate = await _companyClock.GetTodayAsync();

        if (activeMembership && !formerMembership && !futureMembership)
        {
            return query.Where(co =>
                            co.Membership!.ValidFrom.Date <= nowDate &&
                            (!co.Membership.ValidUntil.HasValue ||
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
                            (co.Membership!.ValidFrom.Date <= nowDate &&
                            (!co.Membership.ValidUntil.HasValue ||
                            co.Membership.ValidUntil.Value.Date >= nowDate)) ||
                            (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date < nowDate));
        }

        if (activeMembership && !formerMembership && futureMembership)
        {
            return query.Where(co =>
                             (co.Membership!.ValidFrom.Date <= nowDate &&
                             (!co.Membership.ValidUntil.HasValue ||
                             co.Membership.ValidUntil.Value.Date >= nowDate)) ||
                             (co.Membership.ValidFrom.Date > nowDate));
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


    public async Task<bool> IsActiveMembership(DateTime validFrom, DateTime? validUntil)
    {
        var nowDate = await _companyClock.GetTodayAsync();
        return validFrom.Date <= nowDate &&
               (!validUntil.HasValue || validUntil.Value.Date >= nowDate);
    }

    public async Task<bool> IsFormerMembership(DateTime validFrom, DateTime? validUntil)
    {
        var nowDate = await _companyClock.GetTodayAsync();
        return validUntil.HasValue && validUntil.Value.Date < nowDate;
    }

    public async Task<bool> IsFutureMembership(DateTime validFrom, DateTime? validUntil)
    {
        var nowDate = await _companyClock.GetTodayAsync();
        return validFrom.Date > nowDate;
    }
}