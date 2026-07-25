// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Groups;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Application.DTOs.Filter;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Infrastructure.Repositories.Associations;

public class GroupRepository : BaseRepository<Group>, IGroupRepository
{
    private const int FuzzySearchCandidateCap = 500;

    private readonly IGroupServiceFacade groupServices;
    private readonly IGroupCacheService _groupCacheService;

    public GroupRepository(
        DataBaseContext context,
        IGroupServiceFacade groupServices,
        IGroupCacheService groupCacheService,
        ILogger<Group> logger)
       : base(context, logger)
    {
        this.groupServices = groupServices;
        _groupCacheService = groupCacheService;
    }

    public new async Task Add(Group model)
    {
        Logger.LogInformation("Adding new group: {GroupName}", model.Name);
        try
        {
            if (model.Parent.HasValue)
            {
                await groupServices.TreeService.AddChildNodeAsync(model.Parent.Value, model);
            }
            else
            {
                await groupServices.TreeService.AddRootNodeAsync(model);
            }

            await context.SaveChangesAsync();
            _groupCacheService.InvalidateGroupHierarchyCache();

            Logger.LogInformation("Group {GroupName} added successfully.", model.Name);
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Failed to add group {GroupName}. Database update error.", model.Name);
            throw new InvalidRequestException($"Failed to add group {model.Name} due to a database error.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred while adding group {GroupName}.", model.Name);
            throw;
        }
    }

    public override async Task<Group?> Delete(Guid id)
    {
        Logger.LogInformation("Attempting to delete group with ID: {GroupId}", id);
        var groupEntity = await context.Group
            .Where(g => g.Id == id)
            .FirstOrDefaultAsync();

        if (groupEntity == null)
        {
            Logger.LogWarning("Group with ID: {GroupId} not found for deletion.", id);
            throw new KeyNotFoundException($"Group with ID {id} not found.");
        }

        try
        {
            await groupServices.TreeService.DeleteNodeAsync(id);
            await context.SaveChangesAsync();
            _groupCacheService.InvalidateGroupHierarchyCache();
            Logger.LogInformation("Group with ID: {GroupId} deleted successfully.", id);
            return groupEntity;
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Failed to delete group with ID: {GroupId}. Database update error.", id);
            throw new InvalidRequestException($"Failed to delete group with ID {id} due to a database error.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred while deleting group with ID: {GroupId}.", id);
            throw;
        }
    }

    public IQueryable<Group> FilterGroup(GroupFilter filter)
    {
        Logger.LogInformation("Filtering groups using domain services");
        
        // Repository creates the base query with includes
        var baseQuery = context.Group
            .Include(gr => gr.GroupItems)
            .ThenInclude(gi => gi.Client)
            .AsNoTracking()
            .OrderBy(g => g.Root);
        
        // Domain services apply filters to the query
        return groupServices.SearchService.ApplyFilters(baseQuery, filter);
    }

    public new async Task<Group?> Get(Guid id)
    {
        Logger.LogInformation("Fetching group with ID: {GroupId}", id);
        var group = await context.Group.Where(x => x.Id == id)
            .Include(gr => gr.GroupItems.Where(gi => !gi.ShiftId.HasValue && gi.ClientId.HasValue))
                .ThenInclude(gi => gi.Client)
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (group == null)
        {
            Logger.LogWarning("Group with ID: {GroupId} not found.", id);
            throw new KeyNotFoundException($"Group with ID {id} not found.");
        }

        Logger.LogDebug("Group {GroupId} loaded with {ItemCount} items", id, group.GroupItems?.Count ?? 0);

        Logger.LogInformation("Group with ID: {GroupId} found successfully.", id);
        return group;
    }

    public async Task<bool> SetCoordinatesAsync(Guid groupId, double latitude, double longitude, CancellationToken cancellationToken = default)
    {
        var group = await context.Group.FirstOrDefaultAsync(x => x.Id == groupId, cancellationToken);
        if (group == null)
        {
            Logger.LogWarning("SetCoordinatesAsync: Group with ID {GroupId} not found.", groupId);
            return false;
        }

        group.Latitude = latitude;
        group.Longitude = longitude;
        group.GeocodingAttempted = true;
        await context.SaveChangesAsync(cancellationToken);
        Logger.LogInformation("Group {GroupId} coordinates updated.", groupId);
        return true;
    }

    public async Task<bool> MarkGeocodingAttemptedAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var group = await context.Group.FirstOrDefaultAsync(x => x.Id == groupId, cancellationToken);
        if (group == null)
        {
            Logger.LogWarning("MarkGeocodingAttemptedAsync: Group with ID {GroupId} not found.", groupId);
            return false;
        }

        group.GeocodingAttempted = true;
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<Guid>> GetUnattemptedGeocodingCandidateIdsAsync(CancellationToken cancellationToken = default)
    {
        return await context.Group
            .Where(g => !g.GeocodingAttempted && g.Latitude == null)
            .Select(g => g.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<GroupGeocodingStatus> GetGeocodingStatusAsync(CancellationToken cancellationToken = default)
    {
        var total = await context.Group.CountAsync(cancellationToken);
        var withCoordinates = await context.Group.CountAsync(g => g.Latitude != null, cancellationToken);
        var attemptedNotAPlaceOrFailed = await context.Group
            .CountAsync(g => g.GeocodingAttempted && g.Latitude == null, cancellationToken);
        var pending = await context.Group
            .CountAsync(g => !g.GeocodingAttempted && g.Latitude == null, cancellationToken);

        return new GroupGeocodingStatus(total, withCoordinates, attemptedNotAPlaceOrFailed, pending);
    }

    public async Task<IEnumerable<Group>> GetChildren(Guid parentId)
    {
        Logger.LogInformation("Getting children for parent {ParentId} using hierarchy service", parentId);
        return await groupServices.HierarchyService.GetChildrenAsync(parentId);
    }

    public async Task<int> GetNodeDepth(Guid nodeId)
    {
        Logger.LogInformation("Getting node depth for {NodeId} using hierarchy service", nodeId);
        return await groupServices.HierarchyService.GetNodeDepthAsync(nodeId);
    }

    public async Task<IEnumerable<Group>> GetPath(Guid nodeId)
    {
        Logger.LogInformation("Getting path for node {NodeId} using hierarchy service", nodeId);
        return await groupServices.HierarchyService.GetPathAsync(nodeId);
    }

    public async Task<IEnumerable<Group>> GetTree(Guid? rootId = null)
    {
        Logger.LogInformation("Getting tree for root {RootId} using hierarchy service", rootId?.ToString() ?? "all");
        try
        {
            return await groupServices.HierarchyService.GetTreeAsync(rootId);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error in GetTree for root {RootId}", rootId?.ToString() ?? "all");
            throw;
        }
    }

    public async Task MoveNode(Guid nodeId, Guid newParentId)
    {
        Logger.LogInformation("Attempting to move group with ID: {NodeId} to new parent ID: {NewParentId}", nodeId, newParentId);
        
        try
        {
            await groupServices.TreeService.MoveNodeAsync(nodeId, newParentId);
            await context.SaveChangesAsync();
            _groupCacheService.InvalidateGroupHierarchyCache();
            Logger.LogInformation("Group with ID: {NodeId} moved successfully to new parent {NewParentId}.", nodeId, newParentId);
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Failed to move group with ID: {NodeId} to new parent {NewParentId}. Database update error.", nodeId, newParentId);
            throw new InvalidRequestException($"Failed to move group with ID {nodeId} to new parent {newParentId} due to a database error.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred while moving group with ID: {NodeId} to new parent {NewParentId}.", nodeId, newParentId);
            throw;
        }
    }

    public override async Task<Group?> Put(Group model)
    {
        Logger.LogInformation("Updating group with ID: {GroupId}", model.Id);
        try
        {
            // Update group membership using domain service
            var newClientIds = model.GroupItems
                .Where(x => x.ClientId.HasValue)
                .Select(x => x.ClientId!.Value);

            await groupServices.MembershipService.UpdateGroupMembershipAsync(model.Id, newClientIds);

            var existingGroup = await context.Group
                .FirstOrDefaultAsync(x => x.Id == model.Id);

            if (existingGroup == null)
            {
                Logger.LogWarning("Put: Group with ID {GroupId} not found for update.", model.Id);
                throw new ValidationException($"Group with ID {model.Id} not found");
            }

            bool hierarchyChanged = existingGroup.Parent != model.Parent;

            existingGroup.Name = model.Name;
            existingGroup.Description = model.Description;
            existingGroup.ValidFrom = model.ValidFrom;
            existingGroup.ValidUntil = model.ValidUntil;
            existingGroup.PaymentInterval = model.PaymentInterval;
            existingGroup.CalendarSelectionId = model.CalendarSelectionId;

            if (hierarchyChanged)
            {
                existingGroup.Parent = model.Parent;

                if (model.Parent.HasValue)
                {
                    await MoveNode(model.Id, model.Parent.Value);
                }
            }

            context.Group.Update(existingGroup);
            await context.SaveChangesAsync();
            _groupCacheService.InvalidateGroupHierarchyCache();
            Logger.LogInformation("Group with ID: {GroupId} updated successfully.", model.Id);
            return model;
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Failed to update group with ID: {GroupId}. Database update error.", model.Id);
            throw new InvalidRequestException($"Failed to update group with ID {model.Id} due to a database error.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred while updating group with ID: {GroupId}.", model.Id);
            throw;
        }
    }

    public async Task<TruncatedGroup> Truncated(GroupFilter filter)
    {
        Logger.LogInformation("Getting paginated groups using search service");

        // Repository creates the base query
        var baseQuery = context.Group
            .Include(gr => gr.GroupItems)
            .ThenInclude(gi => gi.Client)
            .AsNoTracking()
            .OrderBy(g => g.Root);

        // Apply filters first
        var filteredQuery = groupServices.SearchService.ApplyFilters(baseQuery, filter);

        // Then apply pagination
        var result = await groupServices.SearchService.ApplyPaginationAsync(filteredQuery, filter);

        // A misheard or decorated spoken name never survives the substring filter — rank it
        // fuzzily against the groups that pass every other filter instead of showing nothing.
        if (result.MaxItems == 0 && filter.RequiredPage <= 0 && !string.IsNullOrWhiteSpace(filter.SearchString))
        {
            var fuzzy = await TryFuzzySearchFallbackAsync(filter);
            if (fuzzy != null)
            {
                return fuzzy;
            }
        }

        return result;
    }

    private async Task<TruncatedGroup?> TryFuzzySearchFallbackAsync(GroupFilter filter)
    {
        var originalSearch = filter.SearchString;
        filter.SearchString = string.Empty;
        try
        {
            var baseQuery = context.Group
                .Include(gr => gr.GroupItems)
                .ThenInclude(gi => gi.Client)
                .AsNoTracking()
                .OrderBy(g => g.Root);
            var candidates = await groupServices.SearchService.ApplyFilters(baseQuery, filter)
                .Take(FuzzySearchCandidateCap)
                .ToListAsync();

            var resolution = Application.Skills.NameResolution.Resolve(
                candidates, g => g.Name, originalSearch, Application.Skills.GroupResolver.LabelWords);
            var matched = resolution.Match != null
                ? new List<Group> { resolution.Match }
                : resolution.Candidates.ToList();
            if (matched.Count == 0)
            {
                return null;
            }

            return new TruncatedGroup
            {
                Groups = matched,
                MaxItems = matched.Count,
                CurrentPage = filter.RequiredPage,
                FirstItemOnPage = 0,
                MaxPages = 0
            };
        }
        finally
        {
            filter.SearchString = originalSearch;
        }
    }

    public async Task RepairNestedSetValues()
    {
        Logger.LogInformation("Starting RepairNestedSetValues using integrity service.");
        try
        {
            await groupServices.IntegrityService.RepairNestedSetValuesAsync();
            await context.SaveChangesAsync();
            Logger.LogInformation("RepairNestedSetValues completed successfully.");
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "RepairNestedSetValues: Database update error.");
            throw new InvalidRequestException("Failed to repair nested set values due to a database error.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "RepairNestedSetValues: An unexpected error occurred.");
            throw;
        }
    }

    public async Task FixRootValues()
    {
        Logger.LogInformation("Fixing root values using integrity service.");
        await groupServices.IntegrityService.FixRootValuesAsync();
    }

    public async Task<IEnumerable<Group>> GetRoots()
    {
        Logger.LogInformation("Getting root groups using hierarchy service.");
        return await groupServices.HierarchyService.GetRootsAsync();
    }
}
