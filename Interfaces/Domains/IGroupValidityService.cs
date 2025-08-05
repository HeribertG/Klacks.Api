using Klacks.Api.Models.Associations;

namespace Klacks.Api.Interfaces.Domains;

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
    /// <returns>Filtered query with date range constraints</returns>
    IQueryable<Group> ApplyDateRangeFilter(IQueryable<Group> query, bool activeDateRange, bool formerDateRange, bool futureDateRange);

    /// <summary>
    /// Checks if a group is currently active based on ValidFrom/ValidUntil dates
    /// </summary>
    /// <param name="group">Group to check</param>
    /// <param name="referenceDate">Date to check against (defaults to today)</param>
    /// <returns>True if group is active on reference date</returns>
    bool IsGroupActive(Group group, DateTime? referenceDate = null);

    /// <summary>
    /// Checks if a group was active in the past
    /// </summary>
    /// <param name="group">Group to check</param>
    /// <param name="referenceDate">Date to check against (defaults to today)</param>
    /// <returns>True if group was active before reference date</returns>
    bool IsGroupFormer(Group group, DateTime? referenceDate = null);

    /// <summary>
    /// Checks if a group will be active in the future
    /// </summary>
    /// <param name="group">Group to check</param>
    /// <param name="referenceDate">Date to check against (defaults to today)</param>
    /// <returns>True if group will be active after reference date</returns>
    bool IsGroupFuture(Group group, DateTime? referenceDate = null);

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

    /// <summary>
    /// Finds groups that will expire within a specified time period
    /// </summary>
    /// <param name="withinDays">Number of days to look ahead</param>
    /// <param name="rootId">Optional root ID to limit scope</param>
    /// <returns>Collection of groups expiring soon</returns>
    Task<IEnumerable<Group>> GetGroupsExpiringWithinAsync(int withinDays, Guid? rootId = null);
}