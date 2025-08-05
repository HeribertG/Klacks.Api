using Klacks.Api.Models.Associations;

namespace Klacks.Api.Interfaces.Domains;

/// <summary>
/// Domain service for maintaining group data integrity and performing consistency checks
/// </summary>
public interface IGroupIntegrityService
{
    /// <summary>
    /// Repairs nested set values after corruption or inconsistency
    /// </summary>
    /// <param name="rootId">Optional root ID to repair specific tree</param>
    /// <returns>Task representing the async repair operation</returns>
    Task RepairNestedSetValuesAsync(Guid? rootId = null);

    /// <summary>
    /// Fixes root values for groups that reference non-existent root nodes
    /// </summary>
    /// <returns>Task representing the async fix operation</returns>
    Task FixRootValuesAsync();

    /// <summary>
    /// Validates the integrity of nested set values for a tree
    /// </summary>
    /// <param name="rootId">Root of the tree to validate</param>
    /// <returns>True if nested set values are consistent</returns>
    Task<bool> ValidateNestedSetIntegrityAsync(Guid rootId);

    /// <summary>
    /// Rebuilds the entire subtree structure starting from a root node
    /// </summary>
    /// <param name="rootId">ID of the root node to rebuild from</param>
    /// <returns>Task representing the async rebuild operation</returns>
    Task RebuildSubtreeAsync(Guid rootId);

    /// <summary>
    /// Finds and reports inconsistencies in the nested set model
    /// </summary>
    /// <returns>Collection of groups with integrity issues</returns>
    Task<IEnumerable<Group>> FindIntegrityIssuesAsync();

    /// <summary>
    /// Validates that all required fields are properly set on a group
    /// </summary>
    /// <param name="group">Group to validate</param>
    /// <returns>Collection of validation error messages</returns>
    IEnumerable<string> ValidateGroupData(Group group);

    /// <summary>
    /// Ensures referential integrity between groups and their parent/root references
    /// </summary>
    /// <returns>Task representing the async integrity check</returns>
    Task EnsureReferentialIntegrityAsync();

    /// <summary>
    /// Detects and reports orphaned groups (groups with invalid parent references)
    /// </summary>
    /// <returns>Collection of orphaned groups</returns>
    Task<IEnumerable<Group>> FindOrphanedGroupsAsync();

    /// <summary>
    /// Validates that Lft/Rgt values are unique and properly ordered within a tree
    /// </summary>
    /// <param name="rootId">Root of the tree to validate</param>
    /// <returns>True if Lft/Rgt values are valid</returns>
    Task<bool> ValidateLftRgtUniquenessAsync(Guid rootId);

    /// <summary>
    /// Checks for circular references in parent-child relationships
    /// </summary>
    /// <returns>Collection of groups involved in circular references</returns>
    Task<IEnumerable<Group>> FindCircularReferencesAsync();

    /// <summary>
    /// Validates that nested set boundaries are properly maintained across all operations
    /// </summary>
    /// <param name="rootId">Root of the tree to validate</param>
    /// <returns>True if nested set boundaries are consistent</returns>
    Task<bool> ValidateNestedSetBoundariesAsync(Guid rootId);

    /// <summary>
    /// Performs a comprehensive integrity check on all group data
    /// </summary>
    /// <returns>Report containing all found integrity issues</returns>
    Task<GroupIntegrityReport> PerformFullIntegrityCheckAsync();
}

/// <summary>
/// Report containing results of group integrity checks
/// </summary>
public class GroupIntegrityReport
{
    public IEnumerable<Group> OrphanedGroups { get; set; } = Enumerable.Empty<Group>();
    public IEnumerable<Group> CircularReferences { get; set; } = Enumerable.Empty<Group>();
    public IEnumerable<Group> NestedSetInconsistencies { get; set; } = Enumerable.Empty<Group>();
    public IEnumerable<string> ValidationErrors { get; set; } = Enumerable.Empty<string>();
    public bool IsIntegrityValid => !OrphanedGroups.Any() && 
                                   !CircularReferences.Any() && 
                                   !NestedSetInconsistencies.Any() && 
                                   !ValidationErrors.Any();
}