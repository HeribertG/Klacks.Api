using Klacks.Api.Models.Staffs;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Interfaces;

public interface IClientRepository : IBaseRepository<Client>
{
    Task<List<Client>> BreakList(BreakFilter filter);

    Task<TruncatedClient> ChangeList(FilterResource filter);

    int Count();

    Task<IQueryable<Client>> FilterClients(FilterResource filter);

    Task<Client?> FindByMail(string mail);

    Task<List<Client>> FindList(string? company = null, string? name = null, string? firstName = null);

    Task<string> FindStatePostCode(string zip);

    Task<LastChangeMetaDataResource> LastChangeMetaData();

    Task<TruncatedClient> Truncated(FilterResource filter);

    Task<List<Client>> WorkList(WorkFilter filter);
}
