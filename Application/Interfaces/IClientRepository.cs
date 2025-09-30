using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Results;

namespace Klacks.Api.Application.Interfaces;

public interface IClientRepository : IBaseRepository<Client>
{
    Task<List<Client>> BreakList(BreakFilter filter, PaginationParams pagination);

    Task<PagedResult<Client>> GetFilteredClients(ClientFilter filter, PaginationParams pagination);

    int Count();

    Task<IQueryable<Client>> FilterClients(ClientFilter filter);

    Task<Client?> FindByMail(string mail);

    Task<List<Client>> FindList(string? company = null, string? name = null, string? firstName = null);

    Task<string> FindStatePostCode(string zip);

    Task<LastChangeMetaData> LastChangeMetaData();

    Task<List<Client>> WorkList(WorkFilter filter, PaginationParams pagination);
}
