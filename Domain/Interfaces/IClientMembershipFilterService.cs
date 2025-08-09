using Klacks.Api.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces;

public interface IClientMembershipFilterService
{
    IQueryable<Client> ApplyMembershipFilter(IQueryable<Client> query, bool activeMembership, bool formerMembership, bool futureMembership);
    IQueryable<Client> ApplyScopeFilter(IQueryable<Client> query, bool? scopeFromFlag, bool? scopeUntilFlag, DateTime? scopeFrom, DateTime? scopeUntil);
    IQueryable<Client> ApplyMembershipYearFilter(IQueryable<Client> query, BreakFilter filter);
    IQueryable<Client> ApplyBreaksYearFilter(IQueryable<Client> query, BreakFilter filter);
    bool IsActiveMembership(DateTime validFrom, DateTime? validUntil);
    bool IsFormerMembership(DateTime validFrom, DateTime? validUntil);
    bool IsFutureMembership(DateTime validFrom, DateTime? validUntil);
}