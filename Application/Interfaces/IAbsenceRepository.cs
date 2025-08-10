using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Presentation.DTOs;
using Klacks.Api.Presentation.DTOs.Filter;

namespace Klacks.Api.Application.Interfaces;

public interface IAbsenceRepository : IBaseRepository<Absence>
{
    HttpResultResource CreateExcelFile(string language);

    Task<TruncatedAbsence> Truncated(AbsenceFilter filter);
}
