using Klacks_api.Models.Staffs;
using Klacks_api.Resources.Filter;

namespace Klacks_api.Interfaces;

public interface IClientRepository : IBaseRepository<Client>
{
  Task<List<Client>> BreakList(BreakFilter filter);

  Task<TruncatedClient> ChangeList(FilterResource filter);

  int Count();

  IQueryable<Client> FilterClient(FilterResource filter);

  Task<Client?> FindByMail(string mail);

  Task<List<Client>> FindList(string? company = null, string? name = null, string? firstName = null);

  Task<string> FindStatePostCode(string zip);

  Task<LastChangeMetaDataResource> LastChangeMetaData();

  Task<TruncatedClient> Truncated(FilterResource filter);

  Task<List<Client>> WorkList(WorkFilter filter);
}
