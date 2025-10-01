using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Domain.Models.Results;
using Klacks.Api.Domain.Models.Filters;

namespace Klacks.Api.Application.Interfaces;

public interface IClientRepository : IBaseRepository<Client>
{
    int Count();
    Task<LastChangeMetaData> LastChangeMetaData();
}
