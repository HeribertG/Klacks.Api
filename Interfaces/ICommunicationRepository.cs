using Klacks.Api.Models.Settings;
using Klacks.Api.Models.Staffs;

namespace Klacks.Api.Interfaces;

public interface ICommunicationRepository : IBaseRepository<Communication>
{
  Task<List<CommunicationType>> TypeList();

  Task<List<Communication>> GetClient(Guid id);
}
