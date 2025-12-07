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

public class ClientBreakPlaceholderRepository : IClientBreakPlaceholderRepository
{
    private readonly DataBaseContext context;
    private readonly IClientGroupFilterService _groupFilterService;
    private readonly IClientSearchFilterService _searchFilterService;

    public ClientBreakPlaceholderRepository(
        DataBaseContext context,
        IClientGroupFilterService groupFilterService,
        IClientSearchFilterService searchFilterService)
    {
        this.context = context;
        _groupFilterService = groupFilterService;
        _searchFilterService = searchFilterService;
    }

    public async Task<(List<Client> Clients, int TotalCount)> BreakList(BreakFilter filter)
    {
        if (filter.CurrentYear <= 0)
        {
            return (new List<Client>(), 0);
        }

        var startOfYear = new DateTime(filter.CurrentYear, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endOfYear = new DateTime(filter.CurrentYear, 12, 31, 23, 59, 59, DateTimeKind.Utc);

        var query = this.context.Client
            .Include(c => c.Membership)
            .Include(c => c.GroupItems)
            .Where(c => c.Type != EntityTypeEnum.Customer)
            .AsQueryable();

        query = await _groupFilterService.FilterClientsByGroupId(filter.SelectedGroup, query);

        query = _searchFilterService.ApplySearchFilter(query, filter.SearchString, false);

        query = query.Where(c =>
            c.Membership!.ValidFrom <= endOfYear &&
            (!c.Membership.ValidUntil.HasValue || c.Membership.ValidUntil.Value >= startOfYear));

        query = ApplySorting(query, filter.OrderBy, filter.SortOrder);

        var totalCount = await query.CountAsync();

        if (filter.StartRow.HasValue && filter.RowCount.HasValue)
        {
            query = query.Skip(filter.StartRow.Value).Take(filter.RowCount.Value);
        }

        if (filter.AbsenceIds?.Any() == true)
        {
            query = query.Include(c => c.BreakPlaceholders
                .Where(b => filter.AbsenceIds.Contains(b.AbsenceId) &&
                           b.From >= startOfYear &&
                           b.From <= endOfYear)
                .OrderBy(b => b.From)
                .ThenBy(b => b.Until));
        }

        var clients = await query.AsSingleQuery().ToListAsync();

        return (clients, totalCount);
    }

    private IQueryable<Client> ApplySorting(IQueryable<Client> query, string orderBy, string sortOrder)
    {
        var isDescending = sortOrder?.ToLower() == "desc";

        return (orderBy?.ToLower()) switch
        {
            "firstname" => isDescending
                ? query.OrderByDescending(c => c.FirstName).ThenByDescending(c => c.Name)
                : query.OrderBy(c => c.FirstName).ThenBy(c => c.Name),

            "company" => isDescending
                ? query.OrderByDescending(c => c.Company).ThenByDescending(c => c.Name)
                : query.OrderBy(c => c.Company).ThenBy(c => c.Name),

            "name" or _ => isDescending
                ? query.OrderByDescending(c => c.Name).ThenByDescending(c => c.FirstName)
                : query.OrderBy(c => c.Name).ThenBy(c => c.FirstName)
        };
    }
}
