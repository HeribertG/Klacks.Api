using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Interfaces.Associations;

/// <summary>
/// Domain service for handling group hierarchy queries and tree structure traversal
/// </summary>
public interface IGroupHierarchyService
{
    /// <summary>
    /// Gets all child groups of a parent group
    /// </summary>
    /// <param name="parentId">ID of the parent group</param>
    /// <returns>Collection of direct child groups</returns>
    Task<IEnumerable<Group>> GetChildrenAsync(Guid parentId);

    /// <summary>
    /// Gets the depth/level of a node in the tree structure
    /// </summary>
    /// <param name="nodeId">ID of the node</param>
    /// <returns>Depth level (0 for root nodes)</returns>
    Task<int> GetNodeDepthAsync(Guid nodeId);

    /// <summary>
    /// Gets the complete path from root to a specific node
    /// </summary>
    /// <param name="nodeId">ID of the target node</param>
    /// <returns>Collection of groups from root to target node</returns>
    Task<IEnumerable<Group>> GetPathAsync(Guid nodeId);

    /// <summary>
    /// Gets the complete tree structure for a specific root or all trees
    /// </summary>
    /// <param name="rootId">Optional root ID to limit to specific tree</param>
    /// <returns>Collection of groups in tree order</returns>
    Task<IEnumerable<Group>> GetTreeAsync(Guid? rootId = null);

    /// <summary>
    /// Gets all root nodes (groups without parents)
    /// </summary>
    /// <returns>Collection of root groups</returns>
    Task<IEnumerable<Group>> GetRootsAsync();

    /// <summary>
    /// Determines if a group is an ancestor of another group
    /// </summary>
    /// <param name="ancestorId">Potential ancestor group ID</param>
    /// <param name="descendantId">Potential descendant group ID</param>
    /// <returns>True if first group is ancestor of second</returns>
    Task<bool> IsAncestorOfAsync(Guid ancestorId, Guid descendantId);

    /// <summary>
    /// Gets all descendants of a group within the nested set boundaries
    /// </summary>
    /// <param name="parentId">Parent group ID</param>
    /// <param name="includeParent">Whether to include the parent in results</param>
    /// <returns>Collection of descendant groups</returns>
    Task<IEnumerable<Group>> GetDescendantsAsync(Guid parentId, bool includeParent = false);

    /// <summary>
    /// Gets all ancestors of a group within the nested set boundaries
    /// </summary>
    /// <param name="nodeId">Target node ID</param>
    /// <param name="includeNode">Whether to include the node itself in results</param>
    /// <returns>Collection of ancestor groups</returns>
    Task<IEnumerable<Group>> GetAncestorsAsync(Guid nodeId, bool includeNode = false);
}