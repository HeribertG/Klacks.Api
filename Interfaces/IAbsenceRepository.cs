using Klacks_api.Models.Schedules;
using Klacks_api.Resources;
using Klacks_api.Resources.Filter;

namespace Klacks_api.Interfaces;

public interface IAbsenceRepository : IBaseRepository<Absence>
{
  HttpResultResource CreateExcelFile(string language);

  Task<TruncatedAbsence_dto> Truncated(AbsenceFilter filter);
}
