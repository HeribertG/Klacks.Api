using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class ClientBreakRepository : IClientBreakRepository
{
    private readonly DataBaseContext context;
    private readonly IClientGroupFilterService _groupFilterService;
    private readonly IClientSearchFilterService _searchFilterService;

    public ClientBreakRepository(
        DataBaseContext context,
        IClientGroupFilterService groupFilterService,
        IClientSearchFilterService searchFilterService)
    {
        this.context = context;
        _groupFilterService = groupFilterService;
        _searchFilterService = searchFilterService;
    }

    public async Task<List<Client>> BreakList(BreakFilter filter)
    {
        if (filter.CurrentYear <= 0)
        {
            return new List<Client>();
        }

        var query = this.context.Client
            .Include(c => c.Membership)
            .Where(c => c.Type != EntityTypeEnum.Customer)
            .AsQueryable();
            
        query = await _groupFilterService.FilterClientsByGroupId(filter.SelectedGroup, query);

        query = _searchFilterService.ApplySearchFilter(query, filter.SearchString, false);

        var startOfYear = new DateTime(filter.CurrentYear, 1, 1);
        var endOfYear = new DateTime(filter.CurrentYear, 12, 31);
        
        query = query.Where(c =>
            c.Membership!.ValidFrom.Date <= endOfYear.Date &&
            (c.Membership.ValidUntil.HasValue == false ||
            (c.Membership.ValidUntil.HasValue && c.Membership.ValidUntil.Value.Date >= startOfYear.Date)));

        if (filter.AbsenceIds?.Any() == true)
        {
            query = query.Include(c => c.Breaks.Where(b => filter.AbsenceIds.Contains(b.AbsenceId) && 
                                                          b.From >= startOfYear && 
                                                          b.From <= endOfYear).OrderBy(b => b.From).ThenBy(b => b.Until));
        }

        return await query.ToListAsync();
    }
}