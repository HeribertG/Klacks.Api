using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Application.DTOs.Filter;

namespace Klacks.Api.Domain.Interfaces;

public interface IAbsencePaginationService
{
       Task<TruncatedAbsence> ApplyPaginationAsync(IQueryable<Absence> query, AbsenceFilter filter);
}