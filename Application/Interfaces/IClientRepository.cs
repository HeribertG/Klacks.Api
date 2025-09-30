using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Filters;
using Klacks.Api.Domain.Models.Results;

namespace Klacks.Api.Application.Interfaces;

public interface IClientRepository : IBaseRepository<Client>
{

    Task<PagedResult<Client>> GetFilteredClients(ClientFilter filter, PaginationParams pagination);

    int Count();

    Task<IQueryable<Client>> FilterClients(ClientFilter filter);


    Task<LastChangeMetaData> LastChangeMetaData();

}
