// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Walks the Shift.OriginalId chain downward in BFS order to find every
/// descendant shift for the supplied roots. Bounded by ShiftConstants.MaxOriginalIdDescendantDepth
/// to keep pathological graphs from looping; a visited-set provides cycle protection.
/// </summary>
using Klacks.Api.Application.Interfaces.Exports;
using Klacks.Api.Application.Constants;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class ShiftDescendantResolver : IShiftDescendantResolver
{
    private readonly DataBaseContext _context;

    public ShiftDescendantResolver(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<Dictionary<Guid, HashSet<Guid>>> ResolveAsync(
        IReadOnlyCollection<Guid> rootIds,
        bool includeRoot,
        CancellationToken cancellationToken = default)
    {
        var result = rootIds.ToDictionary(
            id => id,
            id => includeRoot ? new HashSet<Guid> { id } : []);

        var frontier = rootIds.ToHashSet();
        var visited = new HashSet<Guid>(rootIds);
        var ancestorOfShift = rootIds.ToDictionary(id => id, id => id);

        for (var depth = 0; depth < ShiftConstants.MaxOriginalIdDescendantDepth && frontier.Count > 0; depth++)
        {
            var parents = frontier.ToList();
            var children = await _context.Shift
                .AsNoTracking()
                .Where(s => !s.IsDeleted && s.OriginalId.HasValue && parents.Contains(s.OriginalId!.Value))
                .Select(s => new { s.Id, ParentId = s.OriginalId!.Value })
                .ToListAsync(cancellationToken);

            var nextFrontier = new HashSet<Guid>();
            foreach (var c in children)
            {
                if (!visited.Add(c.Id)) continue;
                if (!ancestorOfShift.TryGetValue(c.ParentId, out var rootAncestor)) continue;
                ancestorOfShift[c.Id] = rootAncestor;
                result[rootAncestor].Add(c.Id);
                nextFrontier.Add(c.Id);
            }

            frontier = nextFrontier;
        }

        return result;
    }
}
