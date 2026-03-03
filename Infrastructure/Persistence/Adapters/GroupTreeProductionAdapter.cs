// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Persistence.Adapters;

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
    }

    public async Task UpdateLftValuesAsync(int minLft, Guid root, int adjustment)
    {
        await _context.Database.ExecuteSqlRawAsync(
            "UPDATE \"group\" SET \"lft\" = \"lft\" + @p0 WHERE \"lft\" > @p1 AND (\"root\" = @p2 OR \"id\" = @p2) AND \"is_deleted\" = false",
            adjustment, minLft, root);
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
