using Klacks.Api.Domain.Models.Settings;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Application.Interfaces;

public interface ICommunicationRepository : IBaseRepository<Communication>
{
    Task<List<CommunicationType>> TypeList();

    Task<List<Communication>> GetClient(Guid id);
}
