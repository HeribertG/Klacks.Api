using Klacks.Api.Models.Schedules;

namespace Klacks.Api.Interfaces;

public interface IBreakRepository : IBaseRepository<Break>
{
  Task<List<Break>> GetClientBreakList(Guid id);
}
