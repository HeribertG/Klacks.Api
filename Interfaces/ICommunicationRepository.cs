using Klacks_api.Models.Settings;
using Klacks_api.Models.Staffs;

namespace Klacks_api.Interfaces;

public interface ICommunicationRepository : IBaseRepository<Communication>
{
  Task<List<CommunicationType>> TypeList();

  Task<List<Communication>> GetClient(Guid id);
}
