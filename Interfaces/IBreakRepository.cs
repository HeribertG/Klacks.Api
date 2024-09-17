using Klacks_api.Models.Schedules;

namespace Klacks_api.Interfaces;

public interface IBreakRepository : IBaseRepository<Break>
{
  Task<List<Break>> GetClientBreakList(Guid id);
}
