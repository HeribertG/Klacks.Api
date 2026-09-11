// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

/// <summary>
/// Domain service for handling group temporal validity and date-based business rules
/// </summary>
public interface IGroupValidityService
{
    /// <summary>
    /// Filters groups based on their validity date ranges (active, former, future)
    /// </summary>
    /// <param name="query">Base query to filter</param>
    /// <param name="activeDateRange">Include currently active groups</param>
    /// <param name="formerDateRange">Include formerly active groups</param>
    /// <param name="futureDateRange">Include future groups</param>
    /// <param name="today">The company's own local day (per ICompanyClock.GetTodayDateAsync), resolved
    /// by the caller. Converted once inside this service to the UTC-midnight DateTime that
    /// ValidFrom/ValidUntil (timestamptz columns) require - Npgsql rejects any other Kind.</param>
    /// <returns>Filtered query with date range constraints</returns>
    IQueryable<Group> ApplyDateRangeFilter(IQueryable<Group> query, bool activeDateRange, bool formerDateRange, bool futureDateRange, DateOnly today);

    /// <summary>
    /// Gets all groups that are valid on a specific date
    /// </summary>
    /// <param name="date">Target date</param>
    /// <param name="rootId">Optional root ID to limit scope</param>
    /// <returns>Collection of groups valid on the specified date</returns>
    Task<IEnumerable<Group>> GetGroupsValidOnDateAsync(DateTime date, Guid? rootId = null);

    /// <summary>
    /// Validates that ValidFrom is before ValidUntil for a group
    /// </summary>
    /// <param name="group">Group to validate</param>
    /// <returns>True if dates are valid</returns>
    bool ValidateDateRange(Group group);

    /// <summary>
    /// Gets the effective validity period for a group, considering parent group constraints
    /// </summary>
    /// <param name="groupId">Group ID</param>
    /// <returns>Tuple of effective ValidFrom and ValidUntil dates</returns>
    Task<(DateTime ValidFrom, DateTime? ValidUntil)> GetEffectiveValidityPeriodAsync(Guid groupId);
}