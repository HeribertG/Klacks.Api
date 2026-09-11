// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Groups;

public class GroupValidityService : IGroupValidityService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<GroupValidityService> _logger;

    public GroupValidityService(DataBaseContext context, ILogger<GroupValidityService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IQueryable<Group> ApplyDateRangeFilter(IQueryable<Group> query, bool activeDateRange, bool formerDateRange, bool futureDateRange, DateOnly today)
    {
        _logger.LogDebug("Filtering by date range: active={Active}, former={Former}, future={Future}",
            activeDateRange, formerDateRange, futureDateRange);

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

        // Must be Kind=Utc, not Local: this value goes into an EF predicate against timestamptz
        // columns, and Npgsql rejects a Kind=Local parameter outright. ValidFrom/ValidUntil are stored
        // as UTC-midnight calendar markers, so comparing against the company's own local day (the
        // caller's ICompanyClock.GetTodayDateAsync), converted here to a UTC-midnight marker, is also
        // the semantically correct side.
        var nowDate = DateTime.SpecifyKind(today.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        _logger.LogDebug("Using reference date: {ReferenceDate}", nowDate);

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

        var ancestors = await _context.Group
            .Where(g => g.Lft < group.Lft && g.Rgt > group.Rgt && g.Root == group.Root)
            .OrderBy(g => g.Lft)
            .ToListAsync();

        var effectiveValidFrom = group.ValidFrom;
        DateTime? effectiveValidUntil = group.ValidUntil;

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
}