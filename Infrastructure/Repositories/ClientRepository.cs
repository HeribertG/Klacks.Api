using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Presentation.DTOs.Filter;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories;

public class ClientRepository : IClientRepository
{
    private readonly DataBaseContext context;
    private readonly IMacroEngine macroEngine;
    private readonly IGetAllClientIdsFromGroupAndSubgroups groupClient;
    private readonly IGroupVisibilityService groupVisibility;
    private readonly IClientFilterService _clientFilterService;
    private readonly IClientMembershipFilterService _membershipFilterService;
    private readonly IClientSearchService _searchService;
    private readonly IClientSortingService _sortingService;
    private readonly IClientChangeTrackingService _changeTrackingService;
    private readonly IClientEntityManagementService _entityManagementService;
    private readonly IClientWorkFilterService _workFilterService;

    public ClientRepository(
        DataBaseContext context,
        IMacroEngine macroEngine,
        IGetAllClientIdsFromGroupAndSubgroups groupClient,
        IGroupVisibilityService groupVisibility,
        IClientFilterService clientFilterService,
        IClientMembershipFilterService membershipFilterService,
        IClientSearchService searchService,
        IClientSortingService sortingService,
        IClientChangeTrackingService changeTrackingService,
        IClientEntityManagementService entityManagementService,
        IClientWorkFilterService workFilterService)
    {
        this.context = context;
        this.macroEngine = macroEngine;
        this.groupClient = groupClient;
        this.groupVisibility = groupVisibility;
        _clientFilterService = clientFilterService;
        _membershipFilterService = membershipFilterService;
        _searchService = searchService;
        _sortingService = sortingService;
        _changeTrackingService = changeTrackingService;
        _entityManagementService = entityManagementService;
        _workFilterService = workFilterService;
    }

    public async Task Add(Client client)
    {
        _entityManagementService.PrepareClientForAdd(client);
        this.context.Client.Add(client);
    }

    public async Task<List<Client>> BreakList(BreakFilter filter)
    {
        var query = await FilterClients(filter.SelectedGroup);

         if (!string.IsNullOrEmpty(filter.SearchString))
        {
            // Ist der search string eine Nummer, sucht man nach der idNumber
            if (_searchService.IsNumericSearch(filter.SearchString))
            {
                query = _searchService.ApplyIdNumberSearch(query, int.Parse(filter.SearchString.Trim()));
            }
            else
            {
                query = _searchService.ApplySearchFilter(query, filter.SearchString, false);
            }
        }

        query = _membershipFilterService.ApplyMembershipYearFilter(query, filter);
        query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder);
        
        var clients = await query.ToListAsync();
        var clientIds = clients.Select(c => c.Id).ToList();
        
        var (startDate, endDate) = DateRangeUtility.GetYearRange(filter.CurrentYear);
        var absenceIds = filter.Absences.Where(x => x.Checked).Select(x => x.Id).ToList();
        
        var breaks = await this.context.Break
            .Include(b => b.Absence)
            .Where(b => clientIds.Contains(b.ClientId) &&
                       absenceIds.Contains(b.AbsenceId) &&
                       b.From < endDate && b.Until >= startDate)
            .OrderBy(b => b.From)
            .ToListAsync();
        
        var breaksByClientId = breaks.ToLookup(b => b.ClientId);
        
        // Assign breaks to clients
        foreach (var client in clients)
        {
            client.Breaks = breaksByClientId.Contains(client.Id) ? breaksByClientId[client.Id].ToList() : new List<Break>();
        }
        
        return clients;
    }

    public async Task<TruncatedClient> ChangeList(FilterResource filter)
    {
        var baseQuery = this.context.Client.IgnoreQueryFilters()
                                .Include(cu => cu.Addresses)
                                .Include(cu => cu.Communications)
                                .Include(cu => cu.Annotations)
                                .Include(cu => cu.Membership)
                                .AsQueryable();

        var myTuple = await _changeTrackingService.GetLastChangedClientsAsync(baseQuery, filter);
        var query = myTuple.clients;

        var maxPages = 0;

        var count = query.Count();
        if (count > 0)
        {
            maxPages = count / filter.NumberOfItemsPerPage;
            if (maxPages <= 0)
            {
                maxPages = 1;
            }

            query = query.Skip(filter.RequiredPage * filter.NumberOfItemsPerPage).Take(filter.NumberOfItemsPerPage);
        }

        var res = new TruncatedClient
        {
            Clients = await query.ToListAsync(),
            MaxItems = count,
            MaxPages = maxPages,
            CurrentPage = filter.RequiredPage,
            Editor = string.Join(", ", myTuple.authors.ToArray()),
            LastChange = myTuple.lastChangeDate,
        };

        return res;
    }

    public int Count()
    {
        if (this.context.Client.Count() > 0)
        {
            return this.context.Client.IgnoreQueryFilters().Max(c => c.IdNumber);
        }
        else
        {
            return 0;
        }
    }

    public async Task<Client?> Delete(Guid id)
    {
        var client = await this.context.Client.Include(a => a.Addresses)
                                             .Include(co => co.Communications)
                                             .Include(co => co.Membership)
                                             .SingleOrDefaultAsync(emp => emp.Id == id);

        if (client != null)
        {
            this.context.Client.Remove(client);
        }

        return client;
    }

    public async Task<bool> Exists(Guid id)
    {
        return await this.context.Client.AnyAsync(e => e.Id == id);
    }

    public async Task<IQueryable<Client>> FilterClients(FilterResource filter)
    {
        IQueryable<Client> query;

        if (filter.ShowDeleteEntries)
        {
            query = this.context.Client.IgnoreQueryFilters()
                                .Include(cu => cu.Addresses)
                                .Include(cu => cu.Communications)
                                .Include(cu => cu.Annotations)
                                .Include(cu => cu.Membership)
                                .Where(cu => cu.IsDeleted)
                                .AsNoTracking()
                                .AsQueryable();
        }
        else
        {
            query = this.context.Client.Include(cu => cu.Addresses)
                                .Include(cu => cu.Communications)
                                .Include(cu => cu.Annotations)
                                .Include(cu => cu.Membership)
                                .AsNoTracking()
                                .AsQueryable();
        }

        query = await FilterClientsByGroupId(filter.SelectedGroup, query);

        // Ist der searchString eine Nummer, sucht man nach der idNumber
        if (_searchService.IsNumericSearch(filter.SearchString))
        {
            var query1 = _searchService.ApplyIdNumberSearch(query, int.Parse(filter.SearchString.Trim()));

            if (filter.IncludeAddress)
            {
                query = _searchService.ApplyPhoneOrZipSearch(query, filter.SearchString.Trim());
                if (query1.Any() && query.Any())
                {
                    query = query.Union(query1);
                }

                if (query1.Any() && !query.Any())
                {
                    query = query1;
                }
            }
            else
            {
                query = query1;
            }

            query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder);
        }
        else
        {
            // Apply entity type filter (Employee, ExternEmp, Customer)
            query = _clientFilterService.ApplyEntityTypeFilter(query, filter.Employee, filter.ExternEmp, filter.Customer);

            var addressTypeList = _clientFilterService.CreateAddressTypeList(filter.HomeAddress, filter.CompanyAddress, filter.InvoiceAddress);
            var gender = _clientFilterService.CreateGenderList(filter.Male, filter.Female, filter.LegalEntity, filter.Intersexuality);

            query = _searchService.ApplySearchFilter(query, filter.SearchString, filter.IncludeAddress);

            if (!(filter.SearchOnlyByName.HasValue && filter.SearchOnlyByName.Value))
            {
                query = _clientFilterService.ApplyGenderFilter(query, gender, filter.LegalEntity);

                query = _clientFilterService.ApplyAnnotationFilter(query, filter.HasAnnotation);

                query = _clientFilterService.ApplyAddressTypeFilter(query, addressTypeList);

                query = _clientFilterService.ApplyStateOrCountryFilter(query, filter.FilteredStateToken, filter.Countries);

                query = _membershipFilterService.ApplyMembershipFilter(query,
                                              filter.ActiveMembership.HasValue && filter.ActiveMembership.Value,
                                              filter.FormerMembership.HasValue && filter.FormerMembership.Value,
                                              filter.FutureMembership.HasValue && filter.FutureMembership.Value);

                if ((filter.ScopeFromFlag.HasValue || filter.ScopeUntilFlag.HasValue) &&
                    (filter.ScopeFrom.HasValue || filter.ScopeUntil.HasValue))
                {
                    query = _membershipFilterService.ApplyScopeFilter(query, filter.ScopeFromFlag, filter.ScopeUntilFlag, filter.ScopeFrom, filter.ScopeUntil);
                }
            }

            query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder);
        }

        return query;
    }

    public async Task<Client?> FindByMail(string mail)
    {
        var query = await this.context.Communication.FirstOrDefaultAsync(x => x.Value == mail && (x.Type == CommunicationTypeEnum.OfficeMail || x.Type == CommunicationTypeEnum.PrivateMail));
        if (query != null)
        {
            return await this.context.Client.FirstOrDefaultAsync(x => x.Id == query.ClientId);
        }

        return null!;
    }

    public async Task<List<Client>> FindList(string? company = null, string? name = null, string? firstname = null)
    {
        var query = this.context.Client.AsQueryable();

        if (!string.IsNullOrWhiteSpace(company))
        {
            query = query.Where(x => x.Company!.ToLower().Contains(company.ToLower().Trim()));
        }

        if (!string.IsNullOrWhiteSpace(name))
        {
            query = query.Where(x => x.Name!.ToLower().Contains(name.ToLower().Trim()));
        }

        if (!string.IsNullOrWhiteSpace(firstname))
        {
            query = query.Where(x => x.FirstName!.ToLower().Contains(firstname.ToLower().Trim()));
        }

        return await query.ToListAsync();
    }

    public async Task<string> FindStatePostCode(string zip)
    {
        int zipCode;
        if (int.TryParse(zip, out zipCode))
        {
            var result = await this.context.PostcodeCH.FirstOrDefaultAsync(x => x.Zip == zipCode);
            if (result != null)
            {
                return result.State;
            }
        }

        return string.Empty;
    }

    public async Task<Client?> Get(Guid id)
    {
        var res = await this.context.Client.Include(cu => cu.Addresses)
                                      .Include(cu => cu.Communications)
                                      .Include(cu => cu.Annotations)
                                      .Include(cu => cu.Membership)
                                      .AsNoTracking()
                                      .SingleOrDefaultAsync(emp => emp.Id == id);

        return res!;
    }

    public async Task<LastChangeMetaDataResource> LastChangeMetaData()
    {
        return await _changeTrackingService.GetLastChangeMetadataResourceAsync();
    }

    public async Task<List<Client>> List()
    {
        return await this.context.Client.Include(cu => cu.Addresses)
                                        .Include(cu => cu.Communications)
                                        .Include(cu => cu.Annotations)
                                        .Include(cu => cu.Membership)
                                        .ToListAsync();
    }

    public async Task<Client> Put(Client client)
    {
        this.ChangeClientNestedLists(client);

        this.context.Update(client);

        var result = this.context.Client.Include(cu => cu.Addresses)
                                        .Include(cu => cu.Communications)
                                        .Include(cu => cu.Annotations)
                                        .Include(cu => cu.Membership)
                                        .FirstOrDefault(x => x.Id == client.Id);

        return result!;
    }

    public void Remove(Client client)
    {
        this.context.Client.Remove(client);
    }

    public async Task<TruncatedClient> Truncated(FilterResource filter)
    {
        var query = await this.FilterClients(filter);

        var count = query.Count();
        var maxPage = filter.NumberOfItemsPerPage > 0 ? (count / filter.NumberOfItemsPerPage) : 0;

        var firstItem = 0;

        if (count > 0 && count > filter.NumberOfItemsPerPage)
        {
            if ((filter.IsNextPage.HasValue || filter.IsPreviousPage.HasValue) && filter.FirstItemOnLastPage.HasValue)
            {
                if (filter.IsNextPage.HasValue)
                {
                    firstItem = filter.FirstItemOnLastPage.Value + filter.NumberOfItemsPerPage;
                }
                else
                {
                    var numberOfItem = filter.NumberOfItemOnPreviousPage ?? filter.NumberOfItemsPerPage;
                    firstItem = filter.FirstItemOnLastPage.Value - numberOfItem;
                    if (firstItem < 0)
                    {
                        firstItem = 0;
                    }
                }
            }
            else
            {
                firstItem = filter.RequiredPage * filter.NumberOfItemsPerPage;
            }
        }
        else
        {
            firstItem = filter.RequiredPage * filter.NumberOfItemsPerPage;
        }

        query = query.Skip(firstItem).Take(filter.NumberOfItemsPerPage);

        var res = new TruncatedClient
        {
            Clients = await query.ToListAsync(),
            MaxItems = count,
        };

        if (filter.NumberOfItemsPerPage > 0)
        {
            res.MaxPages = count % filter.NumberOfItemsPerPage == 0 ? maxPage - 1 : maxPage;
        }

        res.CurrentPage = filter.RequiredPage;
        res.FirstItemOnPage = res.MaxItems <= firstItem ? -1 : firstItem;

        return res;
    }

    public async Task<List<Client>> WorkList(WorkFilter filter)
    {
        var query = await this.FilterClients(filter.SelectedGroup);

        if (!string.IsNullOrEmpty(filter.SearchString))
        {
            // Ist der search string eine Nummer, sucht man nach der idNumber
            if (_searchService.IsNumericSearch(filter.SearchString))
            {
                query = _searchService.ApplyIdNumberSearch(query, int.Parse(filter.SearchString.Trim()));
            }
            else
            {
                query = _searchService.ApplySearchFilter(query, filter.SearchString, false);
            }
        }

        query = _workFilterService.FilterByMembershipYearMonth(query, filter.CurrentYear, filter.CurrentMonth);
        query = _workFilterService.FilterByWorkSchedule(query, filter, this.context);
        query = _sortingService.ApplySorting(query, filter.OrderBy, filter.SortOrder);
        return await Task.FromResult(query.ToList());
    }

    private void ChangeClientNestedLists(Client client)
    {
        _entityManagementService.UpdateNestedEntities<Communication>(
             client.Id,
             client.Communications.Select(x => x.Id).ToArray(),
             id => this.context.Communication.Where(x => x.ClientId == id),
             entity => this.context.Communication.Remove(entity)
         );

        _entityManagementService.UpdateNestedEntities<Address>(
             client.Id,
             client.Addresses.Select(x => x.Id).ToArray(),
             id => this.context.Address.Where(x => x.ClientId == id),
             entity => this.context.Address.Remove(entity)
         );

        _entityManagementService.UpdateNestedEntities<Annotation>(
               client.Id,
               client.Annotations.Select(x => x.Id).ToArray(),
               id => this.context.Annotation.Where(x => x.ClientId == id),
               entity => this.context.Annotation.Remove(entity)
           );

        if (this.context.ChangeTracker.HasChanges())
        {
            this.context.SaveChanges();
        }
    }
        
    private async Task<IQueryable<Client>> FilterClients(Guid? selectedGroup)
    {
        var query = this.context.Client.Where(c => c.Type != EntityTypeEnum.Customer).Include(c => c.Membership).AsQueryable();

        query = await FilterClientsByGroupId(selectedGroup, query);

        return query;
    }

    private async Task<IQueryable<Client>> FilterClientsByGroupId(Guid? selectedGroupId, IQueryable<Client> query)
    {
        if (selectedGroupId.HasValue)
        {
            var clientIds = await this.groupClient.GetAllClientIdsFromGroupAndSubgroups(selectedGroupId.Value);
            query = query.Where(client => clientIds.Contains(client.Id));
        }
        else
        {
            if (!await groupVisibility.IsAdmin())
            {
                var rootlist = await groupVisibility.ReadVisibleRootIdList();
                if (rootlist.Any())
                {
                    var clientIds = await this.groupClient.GetAllClientIdsFromGroupsAndSubgroupsFromList(rootlist);
                    query = query.Where(client => clientIds.Contains(client.Id));
                }
            }
        }

        return query;
    }
}
