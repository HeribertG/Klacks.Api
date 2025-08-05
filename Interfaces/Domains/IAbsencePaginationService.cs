using Klacks.Api.Models.Schedules;
using Klacks.Api.Resources;
using Klacks.Api.Resources.Filter;

namespace Klacks.Api.Interfaces.Domains;

/// <summary>
/// Domain service for handling pagination of absences
/// </summary>
public interface IAbsencePaginationService
{
    /// <summary>
    /// Applies pagination to the absence query and returns the paginated result
    /// </summary>
    /// <param name="query">The absence query to paginate</param>
    /// <param name="filter">Filter containing pagination parameters</param>
    /// <returns>Paginated absence result with metadata</returns>
    Task<TruncatedAbsence> ApplyPaginationAsync(IQueryable<Absence> query, AbsenceFilter filter);
}