// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Persistence.Adapters;

/// <summary>
/// Production adapter for nested-set tree operations on the Group table. Structural shifts are
/// applied with raw SQL (ExecuteSqlRawAsync) because they touch a variable, potentially large number
/// of rows at once; raw SQL bypasses EF Core's change tracker and identity map entirely, so a Group
/// entity already tracked in this DbContext is not updated by these statements. When several tree
/// mutations run against the same DbContext in one scope (e.g. a bulk import adding many groups),
/// re-querying an already-tracked parent by Id returns the stale identity-mapped instance instead of
/// the row EF Core would build fresh from the database, corrupting the nested set for the next mutation.
/// UpdateRgtValuesAsync and UpdateLftValuesAsync — the two shift methods GroupTreeService.AddChildNodeAsync
/// calls — therefore invalidate every tracked, unmodified Group entry a shift touches by detaching it, so
/// the next query for that Id is served fresh from the database instead of from the identity map; an entry
/// with a pending Modified or Added change is left tracked so that unrelated in-flight write is never
/// discarded. Every other method of this adapter runs its raw SQL without touching the change tracker.
/// The values are not corrected in place: GroupTreeService re-reads the same property (e.g. parent.Rgt)
/// several times after calling these two methods within a single node insertion, relying on it staying at
/// its pre-shift value for that one operation; detaching preserves that value on the already-loaded
/// instance while still fixing what the next, separate query will see.
/// </summary>
public class GroupTreeProductionAdapter : IGroupTreeDatabaseAdapter
{
    private readonly DataBaseContext _context;

    public GroupTreeProductionAdapter(DataBaseContext context)
    {
        _context = context;
    }

    public async Task UpdateRgtValuesAsync(int minRgt, Guid root, int adjustment)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"rgt\" = \"rgt\" + @p0 WHERE \"rgt\" >= @p1 AND (\"root\" = @p2 OR \"id\" = @p2) AND \"is_deleted\" = false",
            adjustment, minRgt, root);

        InvalidateTrackedEntries(root, g => g.Rgt >= minRgt);
    }

    public async Task UpdateLftValuesAsync(int minLft, Guid root, int adjustment)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"lft\" = \"lft\" + @p0 WHERE \"lft\" > @p1 AND (\"root\" = @p2 OR \"id\" = @p2) AND \"is_deleted\" = false",
            adjustment, minLft, root);

        InvalidateTrackedEntries(root, g => g.Lft > minLft);
    }

    private void InvalidateTrackedEntries(Guid root, Func<Group, bool> matchesShiftedRow)
    {
        var staleEntries = _context.ChangeTracker.Entries<Group>()
            .Where(entry => entry.State == EntityState.Unchanged)
            .Where(entry => !entry.Entity.IsDeleted && (entry.Entity.Root == root || entry.Entity.Id == root))
            .Where(entry => matchesShiftedRow(entry.Entity))
            .ToList();

        foreach (var entry in staleEntries)
        {
            entry.State = EntityState.Detached;
        }
    }

    public async Task UpdateRgtRangeAsync(int minRgt, int maxRgt, Guid root, int adjustment)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"rgt\" = \"rgt\" + @p0 WHERE \"rgt\" > @p1 AND \"rgt\" < @p2 AND (\"root\" = @p3 OR \"id\" = @p3) AND \"is_deleted\" = false",
            adjustment, minRgt, maxRgt, root);
    }

    public async Task UpdateLftRangeAsync(int minLft, int maxLft, Guid root, int adjustment)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"lft\" = \"lft\" + @p0 WHERE \"lft\" > @p1 AND \"lft\" < @p2 AND (\"root\" = @p3 OR \"id\" = @p3) AND \"is_deleted\" = false",
            adjustment, minLft, maxLft, root);
    }

    public async Task MarkSubtreeAsDeletedAsync(int lft, int rgt, Guid root, DateTime deletedTime)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"is_deleted\" = true, \"deleted_time\" = @p0 WHERE \"lft\" >= @p1 AND \"rgt\" <= @p2 AND (\"root\" = @p3 OR \"id\" = @p3)",
            deletedTime, lft, rgt, root);
    }

    public async Task ShiftNodesAfterDeleteAsync(int afterRgt, Guid root, int width)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"lft\" = \"lft\" - @p0 WHERE \"lft\" > @p1 AND (\"root\" = @p2 OR \"id\" = @p2) AND \"is_deleted\" = false",
            width, afterRgt, root);

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"rgt\" = \"rgt\" - @p0 WHERE \"rgt\" > @p1 AND (\"root\" = @p2 OR \"id\" = @p2) AND \"is_deleted\" = false",
            width, afterRgt, root);
    }

    public async Task MarkSubtreeWithNegativeValuesAsync(int lft, int rgt, Guid root)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"lft\" = -\"lft\", \"rgt\" = -\"rgt\" WHERE \"lft\" >= @p0 AND \"rgt\" <= @p1 AND (\"root\" = @p2 OR \"id\" = @p2)",
            lft, rgt, root);
    }

    public async Task ShiftNodesToCloseGapAsync(int afterRgt, Guid root, int width)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"lft\" = \"lft\" - @p0 WHERE \"lft\" > @p1 AND (\"root\" = @p2 OR \"id\" = @p2)",
            width, afterRgt, root);

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"rgt\" = \"rgt\" - @p0 WHERE \"rgt\" > @p1 AND (\"root\" = @p2 OR \"id\" = @p2)",
            width, afterRgt, root);
    }

    public async Task MakeSpaceAtPositionAsync(int position, Guid root, int width)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"rgt\" = \"rgt\" + @p0 WHERE \"rgt\" >= @p1 AND (\"root\" = @p2 OR \"id\" = @p2)",
            width, position, root);

        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"lft\" = \"lft\" + @p0 WHERE \"lft\" > @p1 AND (\"root\" = @p2 OR \"id\" = @p2)",
            width, position, root);
    }

    public async Task MoveMarkedSubtreeAsync(int offset, Guid newRoot, Guid oldRoot)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"lft\" = -\"lft\" + @p0, \"rgt\" = -\"rgt\" + @p0, \"root\" = @p1 WHERE \"lft\" <= 0 AND (\"root\" = @p2 OR \"id\" = @p2)",
            offset, newRoot, oldRoot);
    }
}
