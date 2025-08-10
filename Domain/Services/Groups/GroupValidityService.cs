using Klacks.Api.Datas;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Domain.Services.Groups;

public class GroupValidityService : IGroupValidityService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<GroupValidityService> _logger;

    public GroupValidityService(DataBaseContext context, ILogger<GroupValidityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IQueryable<Group> ApplyDateRangeFilter(IQueryable<Group> query, bool activeDateRange, bool formerDateRange, bool futureDateRange)
    {
        _logger.LogDebug("Filtering by date range: active={Active}, former={Former}, future={Future}", 
            activeDateRange, formerDateRange, futureDateRange);

        // If all three are true or all three are false, return accordingly
        if (activeDateRange && formerDateRange && futureDateRange)
        {
            _logger.LogDebug("All date ranges selected - returning all groups");
            return query;
        }

        if (!activeDateRange && !formerDateRange && !futureDateRange)
        {
            _logger.LogDebug("No date ranges selected - returning empty result");
            return Enumerable.Empty<Group>().AsQueryable();
        }

        var nowDate = DateTime.Now;
        _logger.LogDebug("Using reference date: {ReferenceDate}", nowDate);

        // Build compound filter based on selected ranges
        var predicates = new List<System.Linq.Expressions.Expression<Func<Group, bool>>>();

        if (activeDateRange)
        {
            predicates.Add(g => g.ValidFrom.Date <= nowDate &&
                               (!g.ValidUntil.HasValue || g.ValidUntil.Value.Date >= nowDate));
        }

        if (formerDateRange)
        {
            predicates.Add(g => g.ValidUntil.HasValue && g.ValidUntil.Value.Date < nowDate);
        }

        if (futureDateRange)
        {
            predicates.Add(g => g.ValidFrom.Date > nowDate);
        }

        // Combine predicates with OR logic
        var combinedPredicate = predicates.Aggregate((expr1, expr2) => 
        {
            var param = System.Linq.Expressions.Expression.Parameter(typeof(Group), "g");
            var body1 = System.Linq.Expressions.Expression.Invoke(expr1, param);
            var body2 = System.Linq.Expressions.Expression.Invoke(expr2, param);
            var orExpression = System.Linq.Expressions.Expression.OrElse(body1, body2);
            return System.Linq.Expressions.Expression.Lambda<Func<Group, bool>>(orExpression, param);
        });

        return query.Where(combinedPredicate);
    }

    public bool IsGroupActive(Group group, DateTime? referenceDate = null)
    {
        var refDate = (referenceDate ?? DateTime.Now).Date;
        _logger.LogDebug("Checking if group {GroupId} is active on {Date}", group.Id, refDate);

        var isActive = group.ValidFrom.Date <= refDate &&
                      (!group.ValidUntil.HasValue || group.ValidUntil.Value.Date >= refDate);

        _logger.LogDebug("Group {GroupId} active status: {IsActive}", group.Id, isActive);
        return isActive;
    }

    public bool IsGroupFormer(Group group, DateTime? referenceDate = null)
    {
        var refDate = (referenceDate ?? DateTime.Now).Date;
        _logger.LogDebug("Checking if group {GroupId} is former on {Date}", group.Id, refDate);

        var isFormer = group.ValidUntil.HasValue && group.ValidUntil.Value.Date < refDate;

        _logger.LogDebug("Group {GroupId} former status: {IsFormer}", group.Id, isFormer);
        return isFormer;
    }

    public bool IsGroupFuture(Group group, DateTime? referenceDate = null)
    {
        var refDate = (referenceDate ?? DateTime.Now).Date;
        _logger.LogDebug("Checking if group {GroupId} is future on {Date}", group.Id, refDate);

        var isFuture = group.ValidFrom.Date > refDate;

        _logger.LogDebug("Group {GroupId} future status: {IsFuture}", group.Id, isFuture);
        return isFuture;
    }

    public async Task<IEnumerable<Group>> GetGroupsValidOnDateAsync(DateTime date, Guid? rootId = null)
    {
        _logger.LogInformation("Getting groups valid on {Date} for root {RootId}", date.Date, rootId?.ToString() ?? "all");

        var query = _context.Group.AsQueryable();

        if (rootId.HasValue)
        {
            query = query.Where(g => g.Root == rootId || g.Id == rootId);
        }

        var validGroups = await query
            .Where(g => g.ValidFrom.Date <= date.Date &&
                       (!g.ValidUntil.HasValue || g.ValidUntil.Value.Date >= date.Date))
            .OrderBy(g => g.Name)
            .ToListAsync();

        _logger.LogInformation("Found {Count} groups valid on {Date}", validGroups.Count, date.Date);
        return validGroups;
    }

    public bool ValidateDateRange(Group group)
    {
        _logger.LogDebug("Validating date range for group {GroupId}", group.Id);

        if (!group.ValidUntil.HasValue)
        {
            _logger.LogDebug("Group {GroupId} has no ValidUntil - date range is valid", group.Id);
            return true;
        }

        var isValid = group.ValidFrom <= group.ValidUntil.Value;
        _logger.LogDebug("Group {GroupId} date range validation: {IsValid} (ValidFrom: {ValidFrom}, ValidUntil: {ValidUntil})", 
            group.Id, isValid, group.ValidFrom, group.ValidUntil);

        return isValid;
    }

    public async Task<(DateTime ValidFrom, DateTime? ValidUntil)> GetEffectiveValidityPeriodAsync(Guid groupId)
    {
        _logger.LogInformation("Getting effective validity period for group {GroupId}", groupId);

        var group = await _context.Group
            .Where(g => g.Id == groupId)
            .FirstOrDefaultAsync();

        if (group == null)
        {
            _logger.LogWarning("Group {GroupId} not found", groupId);
            throw new KeyNotFoundException($"Group with ID {groupId} not found");
        }

        // Get all ancestors to determine constraining validity periods
        var ancestors = await _context.Group
            .Where(g => g.Lft < group.Lft && g.Rgt > group.Rgt && g.Root == group.Root)
            .OrderBy(g => g.Lft)
            .ToListAsync();

        var effectiveValidFrom = group.ValidFrom;
        DateTime? effectiveValidUntil = group.ValidUntil;

        // Apply parent constraints
        foreach (var ancestor in ancestors)
        {
            if (ancestor.ValidFrom > effectiveValidFrom)
            {
                effectiveValidFrom = ancestor.ValidFrom;
            }

            if (ancestor.ValidUntil.HasValue)
            {
                if (!effectiveValidUntil.HasValue || ancestor.ValidUntil.Value < effectiveValidUntil.Value)
                {
                    effectiveValidUntil = ancestor.ValidUntil;
                }
            }
        }

        _logger.LogInformation("Effective validity period for group {GroupId}: {ValidFrom} - {ValidUntil}", 
            groupId, effectiveValidFrom, effectiveValidUntil?.ToString() ?? "∞");

        return (effectiveValidFrom, effectiveValidUntil);
    }

    public async Task<IEnumerable<Group>> GetGroupsExpiringWithinAsync(int withinDays, Guid? rootId = null)
    {
        _logger.LogInformation("Finding groups expiring within {Days} days for root {RootId}", 
            withinDays, rootId?.ToString() ?? "all");

        var cutoffDate = DateTime.Now.Date.AddDays(withinDays);

        var query = _context.Group.AsQueryable();

        if (rootId.HasValue)
        {
            query = query.Where(g => g.Root == rootId || g.Id == rootId);
        }

        var expiringGroups = await query
            .Where(g => g.ValidUntil.HasValue && 
                       g.ValidUntil.Value.Date <= cutoffDate &&
                       g.ValidUntil.Value.Date >= DateTime.Now.Date) // Still valid today
            .OrderBy(g => g.ValidUntil)
            .ThenBy(g => g.Name)
            .ToListAsync();

        _logger.LogInformation("Found {Count} groups expiring within {Days} days", expiringGroups.Count, withinDays);
        return expiringGroups;
    }
}