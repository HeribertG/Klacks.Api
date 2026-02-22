using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Application.DTOs.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Clients;

public class ClientChangeTrackingService : IClientChangeTrackingService
{
    private readonly DataBaseContext _context;
    private readonly IClientSortingService _sortingService;

    public ClientChangeTrackingService(DataBaseContext context, IClientSortingService sortingService)
    {
        _context = context;
        _sortingService = sortingService;
    }

    public async Task<(DateTime lastChangeDate, List<Guid> clientIds, List<string> authors)> GetLastChangeMetadataAsync()
    {
        DateTime lastChangesDate = new DateTime(0);
        var lst = new List<Guid>();
        var lstAutorKeys = new List<string>();

        var c = await _context.Client.Where(x => x.CreateTime.HasValue).OrderByDescending(x => x.CreateTime).FirstOrDefaultAsync();
        if (c != null)
        {
            if (c.CreateTime != null && lastChangesDate.Date < c.CreateTime.Value.Date)
            {
                lastChangesDate = c.CreateTime.Value.Date;
                var date = lastChangesDate;
                lst = await _context.Client.Where(x => x.CreateTime.HasValue && x.CreateTime.Value.Date == date).Select(x => x.Id).ToListAsync();
                var changesDate = lastChangesDate;
                lstAutorKeys = await _context.Client.Where(x => x.CreateTime!.HasValue! && x.CreateTime!.Value!.Date == changesDate).Select(x => x.CurrentUserCreated!).ToListAsync();
            }

            if (c.CreateTime != null && lastChangesDate.Date == c.CreateTime.Value.Date)
            {
                var date = lastChangesDate;
                lst.AddRange(await _context.Client
                  .Where(x => x.CreateTime.HasValue && x.CreateTime.Value.Date == date).Select(x => x.Id)
                  .ToListAsync());
                var changesDate = lastChangesDate;
#pragma warning disable CS8620
                lstAutorKeys.AddRange(await _context.Client.Where(x => x.CreateTime.HasValue && x.CreateTime.Value.Date == changesDate).Select(x => x.CurrentUserCreated).ToListAsync());
#pragma warning restore CS8620
            }
        }

        c = await _context.Client.Where(x => x.UpdateTime.HasValue).OrderByDescending(x => x.UpdateTime).FirstOrDefaultAsync();
        if (c != null)
        {
            if (c.UpdateTime != null && lastChangesDate.Date < c.UpdateTime.Value.Date)
            {
                lstAutorKeys.Clear();
                lst.Clear();

                lastChangesDate = c.UpdateTime.Value.Date;
                var date = lastChangesDate;
                lst = await _context.Client.Where(x => x.UpdateTime.HasValue && x.UpdateTime.Value.Date == date).Select(x => x.Id).ToListAsync();
                var changesDate = lastChangesDate;
#pragma warning disable CS8619
                lstAutorKeys = await _context.Client.Where(x => x.UpdateTime.HasValue && x.UpdateTime.Value.Date == changesDate).Select(x => x.CurrentUserUpdated).ToListAsync();
#pragma warning restore CS8619
            }
            if (c.UpdateTime != null && lastChangesDate.Date == c.UpdateTime.Value.Date)
            {
                var date = lastChangesDate;
                lst.AddRange(await _context.Client.Where(x => x.UpdateTime.HasValue && x.UpdateTime.Value.Date == date).Select(x => x.Id).ToListAsync());
                var changesDate = lastChangesDate;
#pragma warning disable CS8620
                lstAutorKeys.AddRange(await _context.Client.Where(x => x.UpdateTime.HasValue && x.UpdateTime.Value.Date == changesDate).Select(x => x.CurrentUserUpdated).ToListAsync());
#pragma warning restore CS8620
            }
        }

        c = await _context.Client.IgnoreQueryFilters().Where(x => x.DeletedTime.HasValue).OrderByDescending(x => x.DeletedTime).FirstOrDefaultAsync();
        if (c != null)
        {
            if (c.DeletedTime != null && lastChangesDate.Date < c.DeletedTime.Value.Date)
            {
                lstAutorKeys.Clear();
                lst.Clear();

                lastChangesDate = c.DeletedTime.Value.Date;
                var date = lastChangesDate;
                lst = await _context.Client.IgnoreQueryFilters().Where(x => x.DeletedTime.HasValue && x.DeletedTime.Value.Date == date).Select(x => x.Id).ToListAsync();
                var changesDate = lastChangesDate;
#pragma warning disable CS8619
                lstAutorKeys = await _context.Client.IgnoreQueryFilters().Where(x => x.DeletedTime.HasValue && x.DeletedTime.Value.Date == changesDate).Select(x => x.CurrentUserDeleted).ToListAsync();
#pragma warning restore CS8619
            }
            if (c.DeletedTime != null && lastChangesDate.Date == c.DeletedTime.Value.Date)
            {
                var date = lastChangesDate;
                lst.AddRange(await _context.Client.IgnoreQueryFilters().Where(x => x.DeletedTime.HasValue && x.DeletedTime.Value.Date == date).Select(x => x.Id).ToListAsync());
                var changesDate = lastChangesDate;
#pragma warning disable CS8620
                lstAutorKeys.AddRange(await _context.Client.IgnoreQueryFilters().Where(x => x.DeletedTime.HasValue && x.DeletedTime.Value.Date == changesDate).Select(x => x.CurrentUserDeleted).ToListAsync());
#pragma warning restore CS8620
            }
        }

        var distinctUserKeys = lstAutorKeys.Where(x => x != null).Distinct().ToList();

        var userNames = await _context.Users
            .Where(u => distinctUserKeys.Contains(u.Id))
            .Select(u => u.UserName!)
            .ToListAsync();

        return (lastChangesDate, lst, userNames);
    }

    public async Task<(IQueryable<Client> clients, List<string> authors, DateTime lastChangeDate)> GetLastChangedClientsAsync(IQueryable<Client> baseQuery, FilterResource filter)
    {
        var res = await GetLastChangeMetadataAsync();
        var lastChangesDate = res.lastChangeDate;
        var lst = res.clientIds;
        var lstAutor = res.authors;

        var tmp = baseQuery;
        tmp = _sortingService.ApplySorting(tmp, filter.OrderBy, filter.SortOrder);
        tmp = tmp.Where(x => lst.Contains(x.Id));

        return (tmp, lstAutor, lastChangesDate);
    }

    public async Task<LastChangeMetaDataResource> GetLastChangeMetadataResourceAsync()
    {
        var result = new LastChangeMetaDataResource();
        var res = await GetLastChangeMetadataAsync();

        result.LastChangesDate = res.lastChangeDate;
        result.Autor = string.Join(", ", res.authors.ToArray());
        return result;
    }
}