using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Services.Common;
using Klacks.Api.Infrastructure.Interfaces;
using Klacks.Api.Infrastructure.Persistence;
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

    public async Task<List<Client>> BreakList(BreakFilter filter, PaginationParams pagination)
    {
        var query = this.context.Client
            .Include(c => c.Addresses)
            .Include(c => c.Communications)
            .Include(c => c.Membership)
            .AsQueryable();

        query = await FilterClientsByGroupId(filter.SelectedGroup, query);

        if (!string.IsNullOrEmpty(filter.SearchString))
        {
            if (_searchService.IsNumericSearch(filter.SearchString))
            {
                var numericValue = int.Parse(filter.SearchString.Trim());
                query = _searchService.ApplyIdNumberSearch(query, numericValue);
            }
            else
            {
                query = _searchService.ApplySearchFilter(query, filter.SearchString, false);
            }
        }

        if (filter.AbsenceIds?.Any() == true)
        {
            // Filter nach Abwesenheiten für das aktuelle Jahr
            var startOfYear = new DateTime(filter.CurrentYear, 1, 1);
            var endOfYear = new DateTime(filter.CurrentYear, 12, 31);
            
            query = query.Where(c => this.context.Break
                .Any(b => filter.AbsenceIds.Contains(b.AbsenceId) && 
                         b.ClientId == c.Id &&
                         b.From >= startOfYear && 
                         b.From <= endOfYear));
        }

        return await query
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync();
    }

    public async Task<PagedResult<Client>> GetFilteredClients(ClientFilter filter, PaginationParams pagination)
    {
        var query = await FilterClients(filter);
        
        var totalCount = await query.CountAsync();
        
        var totalPages = (int)Math.Ceiling((double)totalCount / pagination.PageSize);
        if (pagination.PageIndex >= totalPages && totalCount > 0)
        {
            return new PagedResult<Client>
            {
                Items = new List<Client>(),
                TotalCount = totalCount,
                PageNumber = pagination.PageIndex,
                PageSize = pagination.PageSize
            };
        }
        
        var items = await query
            .Skip(pagination.Skip)
            .Take(pagination.Take)
            .ToListAsync();

        return new PagedResult<Client>
        {
            Items = items.ToList(),
            TotalCount = totalCount,
            PageNumber = pagination.PageIndex,
            PageSize = pagination.PageSize
        };
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

    public async Task<IQueryable<Client>> FilterClients(ClientFilter filter)
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
            query = _clientFilterService.ApplyEntityTypeFilter(query, filter.Employee, filter.ExternEmp, filter.Customer);

            var addressTypeList = _clientFilterService.CreateAddressTypeList(filter.HomeAddress, filter.CompanyAddress, filter.InvoiceAddress);
            var gender = _clientFilterService.CreateGenderList(filter.Male, filter.Female, filter.LegalEntity, filter.Intersexuality);
            var clientType = _clientFilterService.CreateClientTypeList(filter.Employee, filter.Customer, filter.ExternEmp);

            query = _searchService.ApplySearchFilter(query, filter.SearchString, filter.IncludeAddress);

            if (!(filter.SearchOnlyByName.HasValue && filter.SearchOnlyByName.Value))
            {
                query = _clientFilterService.ApplyGenderFilter(query, gender);

                query = _clientFilterService.ApplyClientTypeFilter(query, clientType);

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

    public async Task<LastChangeMetaData> LastChangeMetaData()
    {
        var result = await _changeTrackingService.GetLastChangeMetadataAsync();
        return new LastChangeMetaData
        {
            LastChangesDate = result.lastChangeDate,
            Author = string.Join(", ", result.authors)
        };
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
        var existingClient = await this.context.Client
            .Include(c => c.Addresses)
            .Include(c => c.Communications)
            .Include(c => c.Annotations)
            .Include(c => c.Membership)
            .FirstOrDefaultAsync(c => c.Id == client.Id);

        if (existingClient == null)
        {
            throw new KeyNotFoundException($"Client with ID {client.Id} not found");
        }

        var entry = this.context.Entry(existingClient);
        entry.CurrentValues.SetValues(client);
        entry.State = EntityState.Modified;

        UpdateNestedEntitiesManually(existingClient, client);

        return existingClient;
    }

    private void UpdateNestedEntitiesManually(Client existingClient, Client updatedClient)
    {
        UpdateAddresses(existingClient, updatedClient);
        UpdateCommunications(existingClient, updatedClient);
        UpdateAnnotations(existingClient, updatedClient);
        UpdateMembership(existingClient, updatedClient);
    }

    private void UpdateAddresses(Client existingClient, Client updatedClient)
    {
        var existingAddresses = existingClient.Addresses.ToList();
        var updatedAddresses = updatedClient.Addresses.ToList();

        foreach (var address in existingAddresses.ToList())
        {
            var updatedAddress = updatedAddresses.FirstOrDefault(a => a.Id == address.Id);
            if (updatedAddress == null)
            {
                this.context.Entry(address).State = EntityState.Deleted;
            }
            else
            {
                var entry = this.context.Entry(address);
                entry.CurrentValues.SetValues(updatedAddress);
                entry.State = EntityState.Modified;
            }
        }

        foreach (var newAddress in updatedAddresses.Where(a => a.Id == Guid.Empty || !existingAddresses.Any(ea => ea.Id == a.Id)))
        {
            newAddress.ClientId = existingClient.Id;
            this.context.Entry(newAddress).State = EntityState.Added;
            existingClient.Addresses.Add(newAddress);
        }
    }

    private void UpdateCommunications(Client existingClient, Client updatedClient)
    {
        var existingCommunications = existingClient.Communications.ToList();
        var updatedCommunications = updatedClient.Communications.ToList();

        foreach (var communication in existingCommunications.ToList())
        {
            var updatedCommunication = updatedCommunications.FirstOrDefault(c => c.Id == communication.Id);
            if (updatedCommunication == null)
            {
                this.context.Entry(communication).State = EntityState.Deleted;
            }
            else
            {
                var entry = this.context.Entry(communication);
                entry.CurrentValues.SetValues(updatedCommunication);
                entry.State = EntityState.Modified;
            }
        }

        foreach (var newCommunication in updatedCommunications.Where(c => c.Id == Guid.Empty || !existingCommunications.Any(ec => ec.Id == c.Id)))
        {
            newCommunication.ClientId = existingClient.Id;
            this.context.Entry(newCommunication).State = EntityState.Added;
            existingClient.Communications.Add(newCommunication);
        }
    }

    private void UpdateAnnotations(Client existingClient, Client updatedClient)
    {
        var existingAnnotations = existingClient.Annotations.ToList();
        var updatedAnnotations = updatedClient.Annotations.ToList();

        foreach (var annotation in existingAnnotations.ToList())
        {
            var updatedAnnotation = updatedAnnotations.FirstOrDefault(a => a.Id == annotation.Id);
            if (updatedAnnotation == null)
            {
                this.context.Entry(annotation).State = EntityState.Deleted;
            }
            else
            {
                var entry = this.context.Entry(annotation);
                entry.CurrentValues.SetValues(updatedAnnotation);
                entry.State = EntityState.Modified;
            }
        }

        foreach (var newAnnotation in updatedAnnotations.Where(a => a.Id == Guid.Empty || !existingAnnotations.Any(ea => ea.Id == a.Id)))
        {
            newAnnotation.ClientId = existingClient.Id;
            this.context.Entry(newAnnotation).State = EntityState.Added;
            existingClient.Annotations.Add(newAnnotation);
        }
    }

    private void UpdateMembership(Client existingClient, Client updatedClient)
    {
        if (updatedClient.Membership != null)
        {
            if (existingClient.Membership == null)
            {
                updatedClient.Membership.ClientId = existingClient.Id;
                this.context.Entry(updatedClient.Membership).State = EntityState.Added;
                existingClient.Membership = updatedClient.Membership;
            }
            else
            {
                var entry = this.context.Entry(existingClient.Membership);
                entry.CurrentValues.SetValues(updatedClient.Membership);
                entry.State = EntityState.Modified;
            }
        }
        else if (existingClient.Membership != null)
        {
            this.context.Entry(existingClient.Membership).State = EntityState.Deleted;
            existingClient.Membership = null;
        }
    }

    public void Remove(Client client)
    {
        this.context.Client.Remove(client);
    }


    public async Task<List<Client>> WorkList(WorkFilter filter, PaginationParams pagination)
    {
        var query = this.context.Client
            .Include(c => c.Addresses)
            .Include(c => c.Communications)
            .Include(c => c.Membership)
            .AsQueryable();

        query = await FilterClientsByGroupId(filter.SelectedGroup, query);

        if (!string.IsNullOrEmpty(filter.SearchString))
        {
            if (_searchService.IsNumericSearch(filter.SearchString))
            {
                var numericValue = int.Parse(filter.SearchString.Trim());
                query = _searchService.ApplyIdNumberSearch(query, numericValue);
            }
            else
            {
                query = _searchService.ApplySearchFilter(query, filter.SearchString, false);
            }
        }

        if (filter.CurrentYear > 0 && filter.CurrentMonth > 0)
        {
            var startDate = new DateTime(filter.CurrentYear, filter.CurrentMonth, 1)
                .AddDays(-filter.DayVisibleBeforeMonth);
            var endDate = new DateTime(filter.CurrentYear, filter.CurrentMonth, DateTime.DaysInMonth(filter.CurrentYear, filter.CurrentMonth))
                .AddDays(filter.DayVisibleAfterMonth);

            query = _workFilterService.ApplyDateRangeFilter(query, startDate, endDate);
        }

        try
        {
            return await query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToListAsync();
        }
        catch (InvalidOperationException)
        {
            return query
                .Skip(pagination.Skip)
                .Take(pagination.Take)
                .ToList();
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
