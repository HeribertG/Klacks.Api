// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Shifts;

public class ShiftTreeService : IShiftTreeService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<ShiftTreeService> _logger;

    public ShiftTreeService(
        DataBaseContext context,
        ILogger<ShiftTreeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public void SetHierarchyRelation(Shift shift, Guid? parentId, Guid? parentRootId)
    {
        if (parentId == null)
        {
            _logger.LogInformation("Setting shift {ShiftId} as ROOT node", shift.Id);
            shift.ParentId = null;
            shift.RootId = shift.Id;
            shift.Lft = null;
            shift.Rgt = null;
        }
        else
        {
            _logger.LogInformation("Setting shift {ShiftId} as CHILD of {ParentId}", shift.Id, parentId);
            shift.ParentId = parentId;
            shift.RootId = parentRootId ?? parentId.Value;
            shift.Lft = null;
            shift.Rgt = null;
        }
    }

    public async Task RecalculateNestedSetValuesAsync(Guid rootId)
    {
        _logger.LogInformation("=== Recalculating Nested Set values for Root {RootId} ===", rootId);

        var allShiftsInTree = await _context.Shift
            .Where(s => s.RootId == rootId
                && s.AnalyseToken == null
                && s.ScenarioSourceShiftId == null)
            .ToListAsync();

        if (!allShiftsInTree.Any())
        {
            _logger.LogWarning("No shifts found for root {RootId}", rootId);
            return;
        }

        _logger.LogInformation("Loaded {Count} shifts for tree calculation", allShiftsInTree.Count);

        var childrenMap = allShiftsInTree
            .Where(s => s.ParentId != null)
            .GroupBy(s => s.ParentId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());

        // The tree's roots are either the self-root node (Id == rootId), or — in the order-root
        // convention where root_id points at the order (which is itself NOT a tree node) — every
        // top-level node (ParentId == null). Walking all top-level nodes as a forest covers both
        // conventions and never throws.
        var selfRoot = allShiftsInTree.FirstOrDefault(s => s.Id == rootId);
        var rootNodes = selfRoot != null
            ? new List<Shift> { selfRoot }
            : allShiftsInTree.Where(s => s.ParentId == null).ToList();

        if (rootNodes.Count == 0)
        {
            _logger.LogWarning("No root nodes (Id == {RootId} or ParentId == null) found for tree", rootId);
            return;
        }

        int counter = 1;
        foreach (var rootNode in rootNodes.OrderBy(s => s.FromDate).ThenBy(s => s.StartShift))
        {
            TraverseAndSetValues(rootNode, childrenMap, ref counter);
        }

        _logger.LogInformation("=== Recalculation complete for Root {RootId} ===", rootId);
    }

    public async Task RecalculateAllAffectedTreesAsync(List<Shift> processedShifts)
    {
        var affectedRoots = processedShifts
            .Where(s => s.RootId != null)
            .Select(s => s.RootId!.Value)
            .Distinct()
            .ToList();

        _logger.LogInformation("Found {Count} affected root trees to recalculate", affectedRoots.Count);

        foreach (var rootId in affectedRoots)
        {
            await RecalculateNestedSetValuesAsync(rootId);
        }
    }

    public int CalculateTreeWidth(int leftValue, int rightValue)
    {
        return rightValue - leftValue + 1;
    }

    private void TraverseAndSetValues(
        Shift node,
        Dictionary<Guid, List<Shift>> childrenMap,
        ref int counter)
    {
        node.Lft = counter++;

        _logger.LogDebug("Shift {ShiftId}: Lft={Lft}", node.Id, node.Lft);

        if (childrenMap.TryGetValue(node.Id, out var children))
        {
            _logger.LogDebug("Shift {ShiftId} has {Count} children", node.Id, children.Count);

            foreach (var child in children.OrderBy(c => c.FromDate).ThenBy(c => c.StartShift))
            {
                TraverseAndSetValues(child, childrenMap, ref counter);
            }
        }

        node.Rgt = counter++;

        _logger.LogDebug("Shift {ShiftId}: Rgt={Rgt}", node.Id, node.Rgt);
    }
}
