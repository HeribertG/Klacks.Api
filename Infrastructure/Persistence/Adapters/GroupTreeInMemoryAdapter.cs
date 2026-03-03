// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Persistence.Adapters;

public class GroupTreeInMemoryAdapter : IGroupTreeDatabaseAdapter
{
    private readonly DataBaseContext _context;

    public GroupTreeInMemoryAdapter(DataBaseContext context)
    {
        _context = context;
    }

    public async Task UpdateRgtValuesAsync(int minRgt, Guid root, int adjustment)
    {
        var groupsToUpdate = await _context.Group
            .Where(g => g.Rgt >= minRgt && g.Root == root && !g.IsDeleted)
            .ToListAsync();

        foreach (var group in groupsToUpdate)
        {
            group.Rgt += adjustment;
            _context.Group.Update(group);
        }
    }

    public async Task UpdateLftValuesAsync(int minLft, Guid root, int adjustment)
    {
        var groupsToUpdate = await _context.Group
            .Where(g => g.Lft > minLft && g.Root == root && !g.IsDeleted)
            .ToListAsync();

        foreach (var group in groupsToUpdate)
        {
            group.Lft += adjustment;
            _context.Group.Update(group);
        }
    }

    public async Task UpdateRgtRangeAsync(int minRgt, int maxRgt, Guid root, int adjustment)
    {
        var groupsToUpdate = await _context.Group
            .Where(g => g.Rgt > minRgt && g.Rgt < maxRgt && g.Root == root && !g.IsDeleted)
            .ToListAsync();

        foreach (var group in groupsToUpdate)
        {
            group.Rgt += adjustment;
            _context.Group.Update(group);
        }
    }

    public async Task UpdateLftRangeAsync(int minLft, int maxLft, Guid root, int adjustment)
    {
        var groupsToUpdate = await _context.Group
            .Where(g => g.Lft > minLft && g.Lft < maxLft && g.Root == root && !g.IsDeleted)
            .ToListAsync();

        foreach (var group in groupsToUpdate)
        {
            group.Lft += adjustment;
            _context.Group.Update(group);
        }
    }

    public async Task MarkSubtreeAsDeletedAsync(int lft, int rgt, Guid root, DateTime deletedTime)
    {
        var subtreeNodes = await _context.Group
            .Where(g => g.Lft >= lft && g.Rgt <= rgt && g.Root == root)
            .ToListAsync();

        foreach (var node in subtreeNodes)
        {
            node.IsDeleted = true;
            node.DeletedTime = deletedTime;
            _context.Group.Update(node);
        }
    }

    public async Task ShiftNodesAfterDeleteAsync(int afterRgt, Guid root, int width)
    {
        var nodesToUpdateLft = await _context.Group
            .Where(g => g.Lft > afterRgt && g.Root == root && !g.IsDeleted)
            .ToListAsync();

        foreach (var node in nodesToUpdateLft)
        {
            node.Lft -= width;
            _context.Group.Update(node);
        }

        var nodesToUpdateRgt = await _context.Group
            .Where(g => g.Rgt > afterRgt && g.Root == root && !g.IsDeleted)
            .ToListAsync();

        foreach (var node in nodesToUpdateRgt)
        {
            node.Rgt -= width;
            _context.Group.Update(node);
        }
    }

    public async Task MarkSubtreeWithNegativeValuesAsync(int lft, int rgt, Guid root)
    {
        var subtreeNodes = await _context.Group
            .Where(g => g.Lft >= lft && g.Rgt <= rgt && g.Root == root)
            .ToListAsync();

        foreach (var node in subtreeNodes)
        {
            node.Lft = -node.Lft;
            node.Rgt = -node.Rgt;
            _context.Group.Update(node);
        }
    }

    public async Task ShiftNodesToCloseGapAsync(int afterRgt, Guid root, int width)
    {
        var nodesToUpdateLft = await _context.Group
            .Where(g => g.Lft > afterRgt && g.Root == root)
            .ToListAsync();

        foreach (var node in nodesToUpdateLft)
        {
            node.Lft -= width;
            _context.Group.Update(node);
        }

        var nodesToUpdateRgt = await _context.Group
            .Where(g => g.Rgt > afterRgt && g.Root == root)
            .ToListAsync();

        foreach (var node in nodesToUpdateRgt)
        {
            node.Rgt -= width;
            _context.Group.Update(node);
        }
    }

    public async Task MakeSpaceAtPositionAsync(int position, Guid root, int width)
    {
        var nodesToUpdateRgt = await _context.Group
            .Where(g => g.Rgt >= position && g.Root == root)
            .ToListAsync();

        foreach (var node in nodesToUpdateRgt)
        {
            node.Rgt += width;
            _context.Group.Update(node);
        }

        var nodesToUpdateLft = await _context.Group
            .Where(g => g.Lft > position && g.Root == root)
            .ToListAsync();

        foreach (var node in nodesToUpdateLft)
        {
            node.Lft += width;
            _context.Group.Update(node);
        }
    }

    public async Task MoveMarkedSubtreeAsync(int offset, Guid newRoot, Guid oldRoot)
    {
        var markedNodes = await _context.Group
            .Where(g => g.Lft <= 0 && g.Root == oldRoot)
            .ToListAsync();

        foreach (var node in markedNodes)
        {
            node.Lft = -node.Lft + offset;
            node.Rgt = -node.Rgt + offset;
            node.Root = newRoot;
            _context.Group.Update(node);
        }
    }
}
