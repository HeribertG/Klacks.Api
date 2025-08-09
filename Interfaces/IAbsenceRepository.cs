using Klacks.Api.Models.Schedules;
using Klacks.Api.Presentation.Resources;
using Klacks.Api.Presentation.Resources.Filter;

namespace Klacks.Api.Interfaces;

public interface IAbsenceRepository : IBaseRepository<Absence>
{
    HttpResultResource CreateExcelFile(string language);

    Task<TruncatedAbsence> Truncated(AbsenceFilter filter);
}
