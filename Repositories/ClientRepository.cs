using Klacks_api.BasicScriptInterpreter;
using Klacks_api.Datas;
using Klacks_api.Enums;
using Klacks_api.Interfaces;
using Klacks_api.Models.Histories;
using Klacks_api.Models.Staffs;
using Klacks_api.Resources.Filter;
using Klacks_api.Resources.Settings;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Klacks_api.Repositories
{
  public class ClientRepository : IClientRepository
  {
    private readonly DataBaseContext context;
    private readonly IMacroEngine macroEngine;

    public ClientRepository(DataBaseContext context, IMacroEngine macroEngine)
    {
      this.context = context;
      this.macroEngine = macroEngine;
    }

    public void Add(Client client)
    {
      for (int i = client.Annotations.Count - 1; i > -1; i--)
      {
        var itm = client.Annotations.ToList()[i];
        if (string.IsNullOrEmpty(itm.Note) || string.IsNullOrWhiteSpace(itm.Note))
        {
          client.Annotations.Remove(itm);
        }
      }

      this.context.Client.Add(client);
    }

    /// <summary>
    /// Listet alle aktiven Mitarbeiter mit ihren Absenzen auf.
    /// </summary>
    /// <param name="filter">Der Filter.</param>
    /// <returns> Gibt alle gefilterten Mitarbeiter mit ihren Absenzen zurück. </returns>
    public Task<List<Client>> BreakList(BreakFilter filter)
    {
      var tmp = this.FilterClients();

      if (!string.IsNullOrEmpty(filter.Search))
      {
        // Ist der search string eine Nummer, sucht man nach der idNumber
        if (filter.Search.Trim().All(char.IsNumber) && int.TryParse(filter.Search.Trim(), out _))
        {
          tmp = tmp.Where(x => x.IdNumber == int.Parse(filter.Search.Trim()));
        }
        else
        {
          tmp = this.FilterBySearchString(filter.Search, false, tmp);
        }
      }

      tmp = this.FilterMembershipYear(filter, tmp);
      tmp = this.FilterBreaksYear(filter, tmp);
      tmp = this.Sort(filter.OrderBy, filter.SortOrder, tmp);
      return tmp.ToListAsync();
    }

    public async Task<TruncatedClient> ChangeList(FilterResource filter)
    {
      var myTuple = await this.LastChangeClientAsync(filter);
      var tmp = myTuple.Item1;

      var maxPages = 0;

      var count = tmp.Count();
      if (count > 0)
      {
        maxPages = count / filter.NumberOfItemsPerPage;
        if (maxPages <= 0)
        {
          maxPages = 1;
        }

        tmp = tmp.Skip(filter.RequiredPage * filter.NumberOfItemsPerPage).Take(filter.NumberOfItemsPerPage);
      }

      var res = new TruncatedClient
      {
        Clients = await tmp.ToListAsync(),
        MaxItems = count,
        MaxPages = maxPages,
        CurrentPage = filter.RequiredPage,
        Editor = string.Join(", ", myTuple.Item2.ToArray()),
        LastChange = myTuple.Item3,
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

    public IQueryable<Client> FilterClient(FilterResource filter)
    {
      IQueryable<Client> tmp;

      if (filter.ShowDeleteEntries)
      {
        tmp = this.context.Client.IgnoreQueryFilters()
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
        tmp = this.context.Client.Include(cu => cu.Addresses)
                            .Include(cu => cu.Communications)
                            .Include(cu => cu.Annotations)
                            .Include(cu => cu.Membership)
                            .AsNoTracking()
                            .AsQueryable();
      }

      // Ist der searchString eine Nummer, sucht man nach der idNumber
      if (filter.SearchString.Trim().All(char.IsNumber) && int.TryParse(filter.SearchString.Trim(), out _))
      {
        var tmp1 = tmp.Where(x => x.IdNumber == int.Parse(filter.SearchString.Trim()));

        if (filter.IncludeAddress)
        {
          tmp = this.FilterBySearchPhonOrZip(filter.SearchString.Trim(), tmp);
          if (tmp1.Any() && tmp.Any())
          {
            tmp = tmp.Union(tmp1);
          }

          if (tmp1.Any() && !tmp.Any())
          {
            tmp = tmp1;
          }
        }
        else
        {
          tmp = tmp1;
        }

        tmp = this.Sort(filter.OrderBy, filter.SortOrder, tmp);
      }
      else
      {
        if (filter.ClientType.HasValue && filter.ClientType.Value != -1)
        {
          tmp = tmp.Where(x => x.Type == filter.ClientType.Value);
        }

        var addressTypeList = this.CreateAddressTypeList(filter.HomeAddress, filter.CompanyAddress, filter.InvoiceAddress);
        var gender = this.CreateGenderList(filter.Male, filter.Female, filter.LegalEntity);

        tmp = this.FilterBySearchString(filter.SearchString, filter.IncludeAddress, tmp);

        if (!(filter.SearchOnlyByName.HasValue && filter.SearchOnlyByName.Value))
        {
          tmp = this.FilterByGender(gender, filter.LegalEntity, tmp);

          tmp = this.FilterByAnnotation(filter.HasAnnotation, tmp);

          tmp = this.FilterByAddresstype(addressTypeList, tmp);

          tmp = this.FilterByStateOrCountry(filter.FilteredStateToken, filter.Countries, tmp);

          tmp = this.FilterByMembership(
                                        filter.ActiveMembership != null && filter.ActiveMembership.Value,
                                        filter.FormerMembership != null && filter.FormerMembership.Value,
                                        filter.FutureMembership != null && filter.FutureMembership.Value,
                                        tmp);

          if ((filter.ScopeFromFlag.HasValue || filter.ScopeUntilFlag.HasValue) &&
              (filter.ScopeFrom.HasValue || filter.ScopeUntil.HasValue))
          {
            tmp = this.FilterScope(filter.ScopeFromFlag, filter.ScopeUntilFlag, filter.ScopeFrom, filter.ScopeUntil, tmp);
          }
        }

        tmp = this.Sort(filter.OrderBy, filter.SortOrder, tmp);
      }

      return tmp;
    }

    public async Task<Client?> FindByMail(string mail)
    {
      var tmp = await this.context.Communication.FirstOrDefaultAsync(x => x.Value == mail && (x.Type == CommunicationTypeEnum.OfficeMail || x.Type == CommunicationTypeEnum.PrivateMail));
      if (tmp != null)
      {
        return await this.context.Client.FirstOrDefaultAsync(x => x.Id == tmp.ClientId);
      }

      return null!;
    }

    public async Task<List<Client>> FindList(string? company = null, string? name = null, string? firstname = null)
    {
      var lst = new List<Client>();

      // Name
      if (string.IsNullOrWhiteSpace(company) && !string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(firstname))
      {
        lst = await this.context.Client.Where(x => x.Name!.ToLower().Contains(name.ToLower().Trim()) && x.FirstName!.ToLower().Contains(firstname.ToLower().Trim())).ToListAsync();
      } // company
      else if (!string.IsNullOrWhiteSpace(company) && string.IsNullOrWhiteSpace(name) && string.IsNullOrWhiteSpace(firstname))
      {
        lst = await this.context.Client.Where(x => x.Company!.ToLower().Contains(company.ToLower().Trim())).ToListAsync();
      } // company + firstname
      else if (!string.IsNullOrWhiteSpace(company) && string.IsNullOrWhiteSpace(company) && !string.IsNullOrWhiteSpace(firstname))
      {
        lst = await this.context.Client.Where(x => x.Company!.ToLower().Contains(company.ToLower().Trim()) && x.FirstName!.ToLower().Contains(firstname.ToLower().Trim())).ToListAsync();
      } // company + Name
      else if (!string.IsNullOrWhiteSpace(company) && !string.IsNullOrWhiteSpace(company) && string.IsNullOrWhiteSpace(firstname))
      {
        lst = await this.context.Client.Where(x => x.Company!.ToLower().Contains(company.ToLower().Trim()) &&
                                              x.Name!.ToLower().Contains(name!.ToLower().Trim())).ToListAsync();
      }
      else if (!string.IsNullOrWhiteSpace(company) && !string.IsNullOrWhiteSpace(company) && !string.IsNullOrWhiteSpace(firstname))
      {
        lst = await this.context.Client.Where(x => x.Company!.ToLower().Contains(company.ToLower().Trim()) &&
                                              (x.Name!.ToLower().Contains(name!.ToLower().Trim()) &&
                                               x.FirstName!.ToLower().Contains(firstname.ToLower().Trim()))).ToListAsync();
      }

      return lst;
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

    public Task<Client?> Get(Guid id, params Expression<Func<Client, object>>[] includes)
    {
      throw new NotImplementedException();
    }

    public async Task<TruncatedHistory> GetHistoryList(FilterHistoryResource filter)
    {
      var tmp = this.context.History
        .Where(x => x.ClientId == filter.Key)
        .AsQueryable();

      tmp = this.SortHistory(filter.OrderBy, filter.SortOrder, tmp);

      var maxPages = 0;

      var count = tmp.Count();

      if (count > 0)
      {
        maxPages = count / filter.NumberOfItemsPerPage;
        if (maxPages <= 0)
        {
          maxPages = 1;
        }
        tmp = tmp.Skip((filter.RequiredPage - 1) * filter.NumberOfItemsPerPage).Take(filter.NumberOfItemsPerPage);
      }

      var res = new TruncatedHistory
      {
        Histories = await tmp.ToListAsync(),
        MaxItems = count,
        MaxPages = maxPages,
        CurrentPage = filter.RequiredPage,
      };
      return res;
    }

    public async Task<LastChangeMetaDataResource> LastChangeMetaData()
    {
      var result = new LastChangeMetaDataResource();
      var res = await this.LastChangeMetaDataAsync();

      result.LastChangesDate = res.Item1;
      result.Autor = string.Join(", ", res.Item3.ToArray());
      return result;
    }

    public async Task<List<Client>> List()
    {
      return await this.context.Client.Include(cu => cu.Addresses)
                                      .Include(cu => cu.Communications)
                                      .Include(cu => cu.Annotations)
                                      .Include(cu => cu.Membership)
                                      .ToListAsync();
    }

    public Task<List<Client>> List(params Expression<Func<Client, object>>[] includes)
    {
      throw new NotImplementedException();
    }

    public Client Put(Client client)
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

    public Client Put(Client model, params Expression<Func<Client, object>>[] includes)
    {
      throw new NotImplementedException();
    }

    public void Remove(Client client)
    {
      this.context.Client.Remove(client);
    }

    public async Task<Client?> Simple(Guid id)
    {
      return await this.context.Client.SingleOrDefaultAsync(emp => emp.Id == id);
    }

    public async Task<TruncatedClient> Truncated(FilterResource filter)
    {
      var tmp = this.FilterClient(filter);

      var count = tmp.Count();
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

      tmp = tmp.Skip(firstItem).Take(filter.NumberOfItemsPerPage);

      var res = new TruncatedClient
      {
        Clients = await tmp.ToListAsync(),
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

    public Task<List<Client>> WorkList(WorkFilter filter)
    {
      var tmp = this.FilterClients();

      if (!string.IsNullOrEmpty(filter.Search))
      {
        // Ist der search string eine Nummer, sucht man nach der idNumber
        if (filter.Search.Trim().All(char.IsNumber) && int.TryParse(filter.Search.Trim(), out _))
        {
          tmp = tmp.Where(x => x.IdNumber == int.Parse(filter.Search.Trim()));
        }
        else
        {
          tmp = this.FilterBySearchString(filter.Search, false, tmp);
        }
      }

      tmp = this.FilterMembershipYearMonth(filter, tmp);
      tmp = this.FilterWorks(filter, tmp);
      tmp = this.Sort(filter.OrderBy, filter.SortOrder, tmp);
      return tmp.ToListAsync();
    }

    private int[] ArrayOfClientType(List<ResultMessage> message)
    {
      return message.Where(x => x.Type == (int)MacroFilterEnum.clientType).Select(x => int.Parse(x.Message)).ToArray();
    }

    private void ChangeClientNestedLists(Client client)
    {
      var communicationIds = client.Communications.Select(x => x.Id).ToArray();
      IEnumerable<Communication> communications;
      if (communicationIds.Length == 0)
      {
        communications = this.context.Communication.Where(x => x.ClientId == client.Id).AsEnumerable();
      }
      else
      {
        communications = this.context.Communication.Where(x => x.ClientId == client.Id && !communicationIds.Contains(x.Id)).AsEnumerable();
      }

      foreach (var communication in communications)
      {
        this.context.Communication.Remove(communication);
      }

      var addressesIds = client.Addresses.Select(x => x.Id).ToArray();
      IEnumerable<Address> addresses;
      if (addressesIds.Length == 0)
      {
        addresses = this.context.Address.Where(x => x.ClientId == client.Id).AsEnumerable();
      }
      else
      {
        addresses = this.context.Address.Where(x => x.ClientId == client.Id && !addressesIds.Contains(x.Id)).AsEnumerable();
      }

      foreach (var address in addresses)
      {
        this.context.Address.Remove(address);
      }

      var annotationsIds = client.Annotations.Select(x => x.Id).ToArray();
      IEnumerable<Annotation> annotations;
      if (annotationsIds.Length == 0)
      {
        annotations = this.context.Annotation.Where(x => x.ClientId == client.Id).AsEnumerable();
      }
      else
      {
        annotations = this.context.Annotation.Where(x => x.ClientId == client.Id && !annotationsIds.Contains(x.Id)).AsEnumerable();
      }

      foreach (var annotation in annotations)
      {
        this.context.Annotation.Remove(annotation);
      }

      if (communications.Count() > 0 || addresses.Count() > 0 || annotations.Count() > 0)
      {
        this.context.SaveChanges();
      }
    }

    private int[] CreateAddressTypeList(bool? homeAddress, bool? companyAddress, bool? invoiceAddress)
    {
      var tmp = new List<int>();

      if (homeAddress != null && homeAddress.Value)
      {
        tmp.Add(0);
      }

      if (companyAddress != null && companyAddress.Value)
      {
        tmp.Add(1);
      }

      if (invoiceAddress != null && invoiceAddress.Value)
      {
        tmp.Add(2);
      }

      return tmp.ToArray();
    }

    private int[] CreateGenderList(bool? male, bool? female, bool? legalEntity)
    {
      var tmp = new List<int>();

      if (male != null && male.Value) { tmp.Add(1); }
      if (female != null && female.Value) { tmp.Add(0); }
      if (legalEntity != null && legalEntity.Value) { tmp.Add(2); }

      return tmp.ToArray();
    }

    private IQueryable<Client> FilterBreaksYear(BreakFilter filter, IQueryable<Client> tmp)
    {
      var startDate = new DateTime(filter.CurrentYear, 1, 1);
      var endDate = new DateTime(filter.CurrentYear, 12, 31);
      var absenceIds = filter.Absences.Where(x => x.Checked).Select(x => x.Id);

      var breaks = this.context.Break
                                .Where(b => absenceIds.Contains(b.AbsenceId) &&
                                            tmp.Select(c => c.Id).Contains(b.ClientId) &&
                                            ((b.From.Date >= startDate && b.From.Date <= endDate) ||
                                            (b.Until.Date >= startDate && b.Until.Date <= endDate) ||
                                            (b.From.Date <= startDate && b.Until.Date >= endDate)))
                                .OrderBy(b => b.From).ThenBy(b => b.Until)
                                .ToList();

      foreach (var c in tmp)
      {
        c.Breaks = breaks.Where(x => x.ClientId == c.Id).ToList();
      }

      return tmp;
    }

    private IQueryable<Client> FilterByAddresstype(int[] addresstype, IQueryable<Client> tmp)
    {
      if (addresstype.Length < 3)
      {
        tmp = tmp.Where(x => x.Addresses.Count == 0 || x.Addresses.Any(y => addresstype.Contains((int)y.Type)));
      }

      return tmp;
    }

    private IQueryable<Client> FilterByAnnotation(bool? hasAnnotation, IQueryable<Client> tmp)
    {
      if (hasAnnotation != null && hasAnnotation.Value) { tmp = tmp.Where(co => co.Annotations.Count > 0); }

      return tmp;
    }

    private IQueryable<Client> FilterByGender(int[] gender, bool? legalEntity, IQueryable<Client> tmp)
    {
      if ((legalEntity == null || (legalEntity.Value == false)) && gender.Length == 0)
      {
        tmp = tmp.Where(co => (int)co.Gender == -1 && co.LegalEntity == false);
      }

      if (legalEntity == null || (legalEntity.Value == false))
      {
        tmp = tmp.Where(co => gender.Any(y => y == ((int)co.Gender)));
      }
      else if (legalEntity != null && legalEntity.Value)
      {
        tmp = tmp.Where(co => gender.Any(y => y == ((int)co.Gender)) || co.LegalEntity);
      }

      return tmp;
    }

    private IQueryable<Client> FilterByMembership(bool activeMembership, bool formerMembership, bool futureMembership, IQueryable<Client> tmp)
    {
      if (activeMembership && formerMembership && futureMembership)
      {
        // No need for filters
      }
      else
      {
        var nowDate = DateTime.Now;

        // only active
        if (activeMembership && !formerMembership && !futureMembership)
        {
          tmp = tmp.Where(co =>
                          co.Membership!.ValidFrom.Date <= nowDate &&
                          (co.Membership.ValidUntil.HasValue == false ||
                          (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date >= nowDate)
                          ));
        }

        // only former
        if (!activeMembership && formerMembership && !futureMembership)
        {
          tmp = tmp.Where(co =>
                         (co.Membership!.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date < nowDate));
        }

        // only future
        if (!activeMembership && !formerMembership && futureMembership)
        {
          tmp = tmp.Where(co =>
                         (co.Membership!.ValidFrom.Date > nowDate));
        }

        // former + active
        if (activeMembership && formerMembership && !futureMembership)
        {
          tmp = tmp.Where(co =>
                          co.Membership!.ValidFrom.Date <= nowDate &&
                          (co.Membership.ValidUntil.HasValue == false ||
                          (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date > nowDate) ||
                          co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date < nowDate));
        }

        // active + future
        if (activeMembership && !formerMembership && futureMembership)
        {
          tmp = tmp.Where(co =>
                           (co.Membership!.ValidFrom.Date <= nowDate &&
                           (co.Membership.ValidUntil.HasValue == false ||
                           (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date > nowDate)) ||
                           (co.Membership.ValidFrom.Date > nowDate)));
        }

        // former + future
        if (!activeMembership && formerMembership && futureMembership)
        {
          tmp = tmp.Where(co =>
                        (co.Membership!.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date < nowDate) ||
                        (co.Membership.ValidFrom.Date > nowDate));
        }
      }

      return tmp;
    }

    private IQueryable<Client> FilterBySearchPhonOrZip(string number, IQueryable<Client> tmp)
    {
      tmp = tmp.Where(co =>
                co.Communications.Any(com => (com.Type == CommunicationTypeEnum.EmergencyPhone ||
                                              com.Type == CommunicationTypeEnum.PrivateCellPhone ||
                                              com.Type == CommunicationTypeEnum.OfficeCellPhone) && com.Value == number) ||
                co.Addresses.Any(ad => ad.Zip.Trim() == number));
      return tmp;
    }

    private IQueryable<Client> FilterBySearchString(string searchString, bool includeAddress, IQueryable<Client> tmp)
    {
      if (!string.IsNullOrEmpty(searchString))
      {
        if (searchString.Contains("+"))
        {
          var keywordList = searchString.ToLower().Split("+");
          tmp = FilterBySearchStringExact(keywordList, includeAddress, tmp);
        }
        else
        {
          var keywordList = searchString.TrimEnd().TrimStart().ToLower().Split(' ');

          if (keywordList.Length == 1)
          {
            if (keywordList[0].Length == 1)
            {
              tmp = this.FilterBySearchStringFirstSymbol(keywordList[0], tmp);
            }
            else
            {
              tmp = this.FilterBySearchStringExact(keywordList, includeAddress, tmp);
            }
          }
          else
          {
            tmp = this.FilterBySearchStringStandart(keywordList, includeAddress, tmp);
          }
        }
      }

      return tmp;
    }

    private IQueryable<Client> FilterBySearchStringExact(string[] keywordList, bool includeAddress, IQueryable<Client> tmp)
    {
      foreach (var keyword in keywordList)
      {
        var tmpKeyword = keyword.TrimEnd().TrimEnd().ToLower();

        if (tmpKeyword.Contains("@"))
        {
          tmp = tmp.Where(co =>
            co.Communications.Any(com => com.Value != null && com.Value.ToLower() == tmpKeyword));
        }
        else
        {
          if (includeAddress)
          {
            tmp = tmp.Where(co =>
                (co.FirstName != null && co.FirstName.ToLower().Contains(tmpKeyword)) ||
                (co.SecondName != null && co.SecondName.ToLower().Contains(tmpKeyword)) ||
                co.Name.ToLower().Contains(tmpKeyword) ||
                (co.MaidenName != null && co.MaidenName.ToLower().Contains(tmpKeyword)) ||
                (co.Company != null && co.Company.ToLower().Contains(tmpKeyword)) ||
                co.Addresses.Any(ad => ad.Street.ToLower().Contains(tmpKeyword)) ||
                co.Addresses.Any(ad => (ad.Street2 != null && ad.Street2.ToLower().Contains(tmpKeyword))) ||
                co.Addresses.Any(ad => (ad.Street3 != null && ad.Street3.ToLower().Contains(tmpKeyword))) ||
                co.Addresses.Any(ad => ad.City.ToLower() == tmpKeyword));
          }
          else
          {
            tmp = tmp.Where(co =>
                (co.FirstName != null && co.FirstName.ToLower().Contains(tmpKeyword)) ||
                (co.SecondName != null && co.SecondName.ToLower().Contains(tmpKeyword)) ||
                co.Name.ToLower().Contains(tmpKeyword) ||
                (co.MaidenName != null && co.MaidenName.ToLower().Contains(tmpKeyword)) ||
                (co.Company != null && co.Company.ToLower().Contains(tmpKeyword)));
          }
        }
      }

      return tmp;
    }

    private IQueryable<Client> FilterBySearchStringFirstSymbol(string keyword, IQueryable<Client> tmp)
    {
      tmp = tmp.Where(co =>
          co.Name.ToLower().Substring(0, 1) == keyword.ToLower());

      return tmp;
    }

    private IQueryable<Client> FilterBySearchStringStandart(string[] keywordList, bool includeAddress, IQueryable<Client> tmp)
    {
      var list = new List<Guid>();
      foreach (var keyword in keywordList)
      {
        foreach (var item in tmp)
        {
          var tmpWord = keyword.TrimEnd().TrimStart().ToLower();

          if (item.FirstName != null && item.FirstName.ToLower().Contains(tmpWord))
          {
            list.Add(item.Id);
          }

          if (item.SecondName != null && item.SecondName.ToLower().Contains(tmpWord))
          {
            list.Add(item.Id);
          }

          if (item.Name != null && item.Name.ToLower().Contains(tmpWord))
          {
            list.Add(item.Id);
          }

          if (item.MaidenName != null && item.MaidenName.ToLower().Contains(tmpWord))
          {
            list.Add(item.Id);
          }

          if (item.Company != null && item.Company.ToLower().Contains(tmpWord))
          {
            list.Add(item.Id);
          }

          if (includeAddress)
          {
            foreach (var addr in item.Addresses)
            {
              if (addr.Street != null && addr.Street.ToLower().Contains(tmpWord))
              {
                list.Add(item.Id);
              }

              if (addr.Street2 != null && addr.Street2.ToLower().Contains(tmpWord))
              {
                list.Add(item.Id);
              }

              if (addr.Street3 != null && addr.Street3.ToLower().Contains(tmpWord))
              {
                list.Add(item.Id);
              }

              if (addr.City != null && addr.City.ToLower().Contains(tmpWord))
              {
                list.Add(item.Id);
              }
            }
          }
        }
      }

      tmp = tmp.Where(x => list.Contains(x.Id));
      return tmp;
    }

    private IQueryable<Client> FilterByStateOrCountry(List<StateCountryToken> list, List<CountryResource> countries, IQueryable<Client> tmp)
    {
      var filteredStateList = list.Where(x => x.Select == true).Select(x => x.State).ToList();
      var filteredCountryList = list.Where(x => x.Select == true).Select(x => x.Country).Distinct().ToList();
      var allStateCountryList = list.Select(x => x.Country).Distinct().ToList();
      var filteredOnlyCountryList = countries.Where(x => x.Select == true && !filteredCountryList.Contains(x.Abbreviation)).Select(x => x.Abbreviation).ToList();

      if (!filteredStateList.Any() && !filteredCountryList.Any() && filteredOnlyCountryList.Any())
      {
        tmp = tmp.Where(co => co.Addresses.Count == 0 || co.Addresses.Any(ad => filteredOnlyCountryList.Contains(ad.Country)));
      }
      else
      {
        tmp = tmp.Where(co => co.Addresses.Count == 0 || co.Addresses.Any(ad => string.IsNullOrEmpty(ad.Country) || filteredCountryList.Contains(ad.Country) || filteredOnlyCountryList.Contains(ad.Country)));
        tmp = tmp.Where(co => co.Addresses.Count == 0 || co.Addresses.Any(ad => string.IsNullOrEmpty(ad.State) || filteredStateList.Contains(ad.State) || filteredOnlyCountryList.Contains(ad.Country)));
      }

      return tmp;
    }

    private IQueryable<Client> FilterClients()
    {
      return this.context.Client.Include(c => c.Membership).OrderBy(x => x.Name).ThenBy(x => x.FirstName).ThenBy(x => x.Company).AsQueryable();
    }

    private IQueryable<Client> FilterMembershipYear(BreakFilter filter, IQueryable<Client> tmp)
    {
      var startDate = new DateTime(filter.CurrentYear, 1, 1);
      var endDate = new DateTime(filter.CurrentYear, 12, 31);

      tmp = tmp.Where(co =>
                          co.Membership!.ValidFrom.Date <= startDate &&
                          (co.Membership.ValidUntil.HasValue == false ||
                          (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date > endDate)));

      return tmp;
    }

    private IQueryable<Client> FilterMembershipYearMonth(WorkFilter filter, IQueryable<Client> tmp)
    {
      var startDate = new DateTime(filter.CurrentYear, filter.CurrentMonth, 1);
      var endDate = startDate.AddMonths(1);

      tmp = tmp.Where(co =>
                          co.Membership!.ValidFrom.Date <= startDate &&
                          (co.Membership.ValidUntil.HasValue == false ||
                          (co.Membership.ValidUntil.HasValue && co.Membership.ValidUntil.Value.Date > endDate)));

      return tmp;
    }

    private IQueryable<Client> FilterScope(bool? scopeFromFlag, bool? scopeUntilFlag, DateTime? scopeFrom,
                                           DateTime? scopeUntil, IQueryable<Client> tmp)
    {
      if ((scopeFromFlag.HasValue && scopeFromFlag.Value) &&
          (scopeUntilFlag.HasValue && scopeUntilFlag.Value) &&
          (scopeFrom.HasValue && scopeUntil.HasValue))
      {
        return this.ScopeFrom2Date1(scopeFrom.Value, scopeUntil.Value, tmp);
      }
      else
      {
        if (scopeFromFlag.HasValue && scopeFromFlag.Value)
        {
          if (scopeFrom.HasValue && scopeUntil.HasValue)
          {
            return this.ScopeFrom2Date(scopeFrom.Value, scopeUntil.Value, tmp);
          }
          else
          {
            return this.ScopeFrom1Date(scopeFrom, scopeUntil, tmp);
          }
        }
        else if (scopeUntilFlag.HasValue && scopeUntilFlag.Value)
        {
          if (scopeFrom.HasValue && scopeUntil.HasValue)
          {
            return this.ScopeUntil2Date(scopeFrom.Value, scopeUntil.Value, tmp);
          }
          else
          {
            return this.ScopeUntil1Date(scopeFrom, scopeUntil, tmp);
          }
        }
      }

      return tmp;
    }

    private IQueryable<Client> FilterWorks(WorkFilter filter, IQueryable<Client> tmp)
    {
      var startDate = new DateTime(filter.CurrentYear, 1, 1);
      var endDate = new DateTime(filter.CurrentYear, 12, 31);

      var work = this.context.Work.Where(b => tmp.Select(c => c.Id).Contains(b.ClientId) &&
                                            ((b.From.Date >= startDate && b.From.Date <= endDate) ||
                                            (b.Until.Date >= startDate && b.Until.Date <= endDate) ||
                                            (b.From.Date <= startDate && b.Until.Date >= endDate)))
                                .OrderBy(b => b.From).ThenBy(b => b.Until)
                                .ToList();

      foreach (var c in tmp)
      {
        c.Works = work.Where(x => x.ClientId == c.Id).ToList();
      }

      return tmp;
    }

    private async Task<Tuple<IQueryable<Client>, List<string>, DateTime>> LastChangeClientAsync(FilterResource filter)
    {
      var res = await this.LastChangeMetaDataAsync();
      var lastChangesDate = res.Item1;
      var lst = res.Item2;
      var lstAutor = res.Item3;

      var tmp = this.context.Client.IgnoreQueryFilters()
                              .Include(cu => cu.Addresses)
                              .Include(cu => cu.Communications)
                              .Include(cu => cu.Annotations)
                              .Include(cu => cu.Membership)
                              .AsQueryable();

      tmp = this.Sort(filter.OrderBy, filter.SortOrder, tmp);

      tmp = tmp.Where(x => lst.Contains(x.Id));

      return new Tuple<IQueryable<Client>, List<string>, DateTime>(tmp, lstAutor, lastChangesDate);
    }

    private async Task<Tuple<DateTime, List<Guid>, List<string>>> LastChangeMetaDataAsync()
    {
      DateTime lastChangesDate = new DateTime(0);
      var lst = new List<Guid>();
      var lstAutor = new List<string>();

      var c = await this.context.Client.Where(x => x.CreateTime.HasValue).OrderByDescending(x => x.CreateTime).FirstOrDefaultAsync();
      if (c != null)
      {
        if (c.CreateTime != null && lastChangesDate.Date < c.CreateTime.Value.Date)
        {
          lastChangesDate = c.CreateTime.Value.Date;
          var date = lastChangesDate;
          lst = await this.context.Client.Where(x => x.CreateTime.HasValue && x.CreateTime.Value.Date == date).Select(x => x.Id).ToListAsync();
          var changesDate = lastChangesDate;
          lstAutor = await this.context.Client.Where(x => x.CreateTime!.HasValue! && x.CreateTime!.Value!.Date == changesDate).Select(x => x.CurrentUserCreated!).ToListAsync();
        }
        if (c.CreateTime != null && lastChangesDate.Date == c.CreateTime.Value.Date)
        {
          var date = lastChangesDate;
          lst.AddRange(await this.context.Client
            .Where(x => x.CreateTime.HasValue && x.CreateTime.Value.Date == date).Select(x => x.Id)
            .ToListAsync());
          var changesDate = lastChangesDate;
#pragma warning disable CS8620 // Das Argument kann aufgrund von Unterschieden bei der NULL-Zul�ssigkeit von Verweistypen nicht f�r den Parameter verwendet werden.
          lstAutor.AddRange(await this.context.Client.Where(x => x.CreateTime.HasValue && x.CreateTime.Value.Date == changesDate).Select(x => x.CurrentUserCreated).ToListAsync());
#pragma warning restore CS8620 // Das Argument kann aufgrund von Unterschieden bei der NULL-Zul�ssigkeit von Verweistypen nicht f�r den Parameter verwendet werden.
        }
      }
      c = await this.context.Client.Where(x => x.UpdateTime.HasValue).OrderByDescending(x => x.UpdateTime).FirstOrDefaultAsync();
      if (c != null)
      {
        if (c.UpdateTime != null && lastChangesDate.Date < c.UpdateTime.Value.Date)
        {
          lstAutor.Clear();
          lst.Clear();

          lastChangesDate = c.UpdateTime.Value.Date;
          var date = lastChangesDate;
          lst = await this.context.Client.Where(x => x.UpdateTime.HasValue && x.UpdateTime.Value.Date == date).Select(x => x.Id).ToListAsync();
          var changesDate = lastChangesDate;
#pragma warning disable CS8619 // Die NULL-Zul�ssigkeit von Verweistypen im Wert entspricht nicht dem Zieltyp.
          lstAutor = await this.context.Client.Where(x => x.UpdateTime.HasValue && x.UpdateTime.Value.Date == changesDate).Select(x => x.CurrentUserUpdated).ToListAsync();
#pragma warning restore CS8619 // Die NULL-Zul�ssigkeit von Verweistypen im Wert entspricht nicht dem Zieltyp.
        }
        if (c.UpdateTime != null && lastChangesDate.Date == c.UpdateTime.Value.Date)
        {
          var date = lastChangesDate;
          lst.AddRange(await this.context.Client.Where(x => x.UpdateTime.HasValue && x.UpdateTime.Value.Date == date).Select(x => x.Id).ToListAsync());
          var changesDate = lastChangesDate;
#pragma warning disable CS8620 // Das Argument kann aufgrund von Unterschieden bei der NULL-Zul�ssigkeit von Verweistypen nicht f�r den Parameter verwendet werden.
          lstAutor.AddRange(await this.context.Client.Where(x => x.UpdateTime.HasValue && x.UpdateTime.Value.Date == changesDate).Select(x => x.CurrentUserUpdated).ToListAsync());
#pragma warning restore CS8620 // Das Argument kann aufgrund von Unterschieden bei der NULL-Zul�ssigkeit von Verweistypen nicht f�r den Parameter verwendet werden.
        }
      }
      c = await this.context.Client.IgnoreQueryFilters().Where(x => x.DeletedTime.HasValue).OrderByDescending(x => x.DeletedTime).FirstOrDefaultAsync();
      if (c != null)
      {
        if (c.DeletedTime != null && lastChangesDate.Date < c.DeletedTime.Value.Date)
        {
          lstAutor.Clear();
          lst.Clear();

          lastChangesDate = c.DeletedTime.Value.Date;
          var date = lastChangesDate;
          lst = await this.context.Client.IgnoreQueryFilters().Where(x => x.DeletedTime.HasValue && x.DeletedTime.Value.Date == date).Select(x => x.Id).ToListAsync();
          var changesDate = lastChangesDate;
#pragma warning disable CS8619 // Die NULL-Zul�ssigkeit von Verweistypen im Wert entspricht nicht dem Zieltyp.
          lstAutor = await this.context.Client.IgnoreQueryFilters().Where(x => x.DeletedTime.HasValue && x.DeletedTime.Value.Date == changesDate).Select(x => x.CurrentUserDeleted).ToListAsync();
#pragma warning restore CS8619 // Die NULL-Zul�ssigkeit von Verweistypen im Wert entspricht nicht dem Zieltyp.
        }
        if (c.DeletedTime != null && lastChangesDate.Date == c.DeletedTime.Value.Date)
        {
          var date = lastChangesDate;
          lst.AddRange(await this.context.Client.IgnoreQueryFilters().Where(x => x.DeletedTime.HasValue && x.DeletedTime.Value.Date == date).Select(x => x.Id).ToListAsync());
          var changesDate = lastChangesDate;
#pragma warning disable CS8620 // Das Argument kann aufgrund von Unterschieden bei der NULL-Zul�ssigkeit von Verweistypen nicht f�r den Parameter verwendet werden.
          lstAutor.AddRange(await this.context.Client.IgnoreQueryFilters().Where(x => x.DeletedTime.HasValue && x.DeletedTime.Value.Date == changesDate).Select(x => x.CurrentUserDeleted).ToListAsync());
#pragma warning restore CS8620 // Das Argument kann aufgrund von Unterschieden bei der NULL-Zul�ssigkeit von Verweistypen nicht f�r den Parameter verwendet werden.
        }
      }

      var autors = lstAutor.Where(x => x != null).Distinct().ToList();

      return new Tuple<DateTime, List<Guid>, List<string>>(lastChangesDate, lst, autors);
    }

    private IQueryable<Client> ScopeFrom1Date(DateTime? scopeFrom, DateTime? scopeUntil, IQueryable<Client> tmp)
    {
      if (scopeFrom.HasValue)
      {
        return tmp.Where(x => x.Membership!.ValidFrom.Date >= scopeFrom.Value.Date);
      }
      else if (scopeUntil.HasValue)
      {
        return tmp.Where(x => x.Membership!.ValidFrom.Date <= scopeUntil.Value.Date);
      }

      return tmp;
    }

    private IQueryable<Client> ScopeFrom2Date(DateTime scopeFrom, DateTime scopeUntil, IQueryable<Client> tmp)
    {
      return tmp.Where(x => x.Membership!.ValidFrom.Date >= scopeFrom.Date && x.Membership.ValidFrom.Date <= scopeUntil.Date);
    }

    private IQueryable<Client> ScopeFrom2Date1(DateTime scopeFrom, DateTime scopeUntil, IQueryable<Client> tmp)
    {
      return tmp.Where(x => (x.Membership!.ValidFrom.Date >= scopeFrom.Date &&
                            x.Membership.ValidFrom.Date <= scopeUntil.Date) ||
                            (x.Membership.ValidUntil!.Value.Date >= scopeFrom.Date &&
                            x.Membership.ValidUntil.Value.Date <= scopeUntil.Date));
    }

    private IQueryable<Client> ScopeUntil1Date(DateTime? scopeFrom, DateTime? scopeUntil, IQueryable<Client> tmp)
    {
      if (scopeFrom.HasValue)
      {
        return tmp.Where(x => x.Membership!.ValidUntil.HasValue && x.Membership.ValidUntil.Value.Date >= scopeFrom.Value.Date);
      }
      else if (scopeUntil.HasValue)
      {
        return tmp.Where(x => x.Membership!.ValidUntil!.Value.Date <= scopeUntil.Value.Date);
      }

      return tmp;
    }

    private IQueryable<Client> ScopeUntil2Date(DateTime scopeFrom, DateTime scopeUntil, IQueryable<Client> tmp)
    {
      return tmp.Where(x => x.Membership!.ValidUntil.HasValue && (x.Membership.ValidUntil.Value.Date >= scopeFrom.Date && x.Membership.ValidUntil.Value.Date <= scopeUntil.Date));
    }

    private IQueryable<Client> Sort(string orderBy, string sortOrder, IQueryable<Client> tmp)
    {
      if (sortOrder != string.Empty)
      {
        if (orderBy == "firstName")
        {
          return sortOrder == "asc" ? tmp.OrderBy(x => x.FirstName).ThenBy(x => x.Name).ThenBy(x => x.IdNumber) : tmp.OrderByDescending(x => x.FirstName).ThenByDescending(x => x.Name).ThenByDescending(x => x.IdNumber);
        }
        else if (orderBy == "idNumber")
        {
          return sortOrder == "asc" ? tmp.OrderBy(x => x.IdNumber) : tmp.OrderByDescending(x => x.IdNumber);
        }
        else if (orderBy == "company")
        {
          return sortOrder == "asc" ? tmp.OrderBy(x => x.Company).ThenBy(x => x.IdNumber) : tmp.OrderByDescending(x => x.Company).ThenByDescending(x => x.IdNumber);
        }
        else if (orderBy == "name")
        {
          return sortOrder == "asc" ? tmp.OrderBy(x => x.Name).ThenBy(x => x.FirstName).ThenBy(x => x.IdNumber) : tmp.OrderByDescending(x => x.Name).ThenByDescending(x => x.FirstName).ThenByDescending(x => x.IdNumber);
        }
      }

      return tmp;
    }

    private IQueryable<History> SortHistory(string orderBy, string sortOrder, IQueryable<History> tmp)
    {
      if (sortOrder != string.Empty)
      {
        if (orderBy == "validFrom")
        {
          return sortOrder == "asc" ? tmp.OrderBy(x => x.ValidFrom) : tmp.OrderByDescending(x => x.ValidFrom);
        }
        else if (orderBy == "currentUserCreated")
        {
          return sortOrder == "asc" ? tmp.OrderBy(x => x.CurrentUserCreated) : tmp.OrderByDescending(x => x.CurrentUserCreated);
        }
        else if (orderBy == "data")
        {
          return sortOrder == "asc" ? tmp.OrderBy(x => x.Data) : tmp.OrderByDescending(x => x.Data);
        }
        else if (orderBy == "newData")
        {
          return sortOrder == "asc" ? tmp.OrderBy(x => x.NewData) : tmp.OrderByDescending(x => x.NewData);
        }
        else if (orderBy == "oldData")
        {
          return sortOrder == "asc" ? tmp.OrderBy(x => x.OldData) : tmp.OrderByDescending(x => x.OldData);
        }
      }

      return tmp;
    }
  }
}
