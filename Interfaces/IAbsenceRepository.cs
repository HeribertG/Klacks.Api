using Klacks.Api.Models.Schedules;
using Klacks.Api.Resources;
using Klacks.Api.Resources.Filter;

namespace Klacks.Api.Interfaces;

public interface IAbsenceRepository : IBaseRepository<Absence>
{
    HttpResultResource CreateExcelFile(string language);

    Task<TruncatedAbsence_dto> Truncated(AbsenceFilter filter);
}
