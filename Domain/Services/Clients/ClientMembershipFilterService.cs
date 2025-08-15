using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Clients;

public class ClientMembershipFilterService : IClientMembershipFilterService
{
    private readonly DataBaseContext _context;

    public ClientMembershipFilterService(DataBaseContext context)
    {
        _context = context;
    }

    public IQueryable<Client> ApplyMembershipFilter(IQueryable<Client> query, bool activeMembership, bool formerMembership, bool futureMembership)
    {
        if (activeMembership && formerMembership && futureMembership)
        {
            return query; // No need for filters
        }

        var nowDate = DateTime.Now;

        // only active
        if (activeMembership && !formerMembership && !futureMembership)
        {
            return query.Where(co =>
                            co.Membership!.ValidFrom.Date <= nowDate &&
                            (co.Membership.ValidUntil.HasValue == false ||
                            (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date >= nowDate)
                            ));
        }

        // only former
        if (!activeMembership && formerMembership && !futureMembership)
        {
            return query.Where(co =>
                           (co.Membership!.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date < nowDate));
        }

        // only future
        if (!activeMembership && !formerMembership && futureMembership)
        {
            return query.Where(co =>
                           (co.Membership!.ValidFrom.Date > nowDate));
        }

        // former + active
        if (activeMembership && formerMembership && !futureMembership)
        {
            return query.Where(co =>
                            co.Membership!.ValidFrom.Date <= nowDate &&
                            (co.Membership.ValidUntil.HasValue == false ||
                            (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date > nowDate) ||
                            co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date < nowDate));
        }

        // active + future
        if (activeMembership && !formerMembership && futureMembership)
        {
            return query.Where(co =>
                             (co.Membership!.ValidFrom.Date <= nowDate &&
                             (co.Membership.ValidUntil.HasValue == false ||
                             (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date > nowDate)) ||
                             (co.Membership.ValidFrom.Date > nowDate)));
        }

        // former + future
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
        var startDate = new DateTime(filter.CurrentYear, 1, 1);
        var endDate = new DateTime(filter.CurrentYear, 12, 31);

        return query.Where(c => c.Membership!.ValidFrom.Date <= endDate && 
                               (!c.Membership.ValidUntil.HasValue || c.Membership.ValidUntil.Value.Date >= startDate));
    }

    public IQueryable<Client> ApplyBreaksYearFilter(IQueryable<Client> query, BreakFilter filter)
    {
        var startDate = new DateTime(filter.CurrentYear, 1, 1);
        var endDate = new DateTime(filter.CurrentYear, 12, 31);
        var absenceIds = filter.Absences.Where(x => x.Checked).Select(x => x.Id);

        // Use Include with filtered navigation to efficiently load breaks
        // EF Core 5+ supports filtered includes
        return query.Include(c => c.Breaks.Where(b => 
            absenceIds.Contains(b.AbsenceId) &&
            ((b.From >= startDate && b.From <= endDate) ||
             (b.Until >= startDate && b.Until <= endDate) ||
             (b.From <= startDate && b.Until >= endDate))))
        .ThenInclude(b => b.Absence);
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