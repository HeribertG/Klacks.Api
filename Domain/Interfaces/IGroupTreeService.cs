using Klacks.Api.Models.Associations;

namespace Klacks.Api.Domain.Interfaces;

/// <summary>
/// Domain service for handling nested set model operations and tree structure management
/// </summary>
public interface IGroupTreeService
{
    /// <summary>
    /// Adds a new child node to the specified parent in the nested set model
    /// </summary>
    /// <param name="parentId">ID of the parent group</param>
    /// <param name="newGroup">New group to add as child</param>
    /// <returns>The added group with updated Lft/Rgt values</returns>
    Task<Group> AddChildNodeAsync(Guid parentId, Group newGroup);

    /// <summary>
    /// Adds a new root node to the tree structure
    /// </summary>
    /// <param name="newGroup">New group to add as root</param>
    /// <returns>The added group with updated Lft/Rgt values</returns>
    Task<Group> AddRootNodeAsync(Group newGroup);

    /// <summary>
    /// Moves a node to a new parent in the tree structure
    /// </summary>
    /// <param name="nodeId">ID of the node to move</param>
    /// <param name="newParentId">ID of the new parent</param>
    Task MoveNodeAsync(Guid nodeId, Guid newParentId);

    /// <summary>
    /// Deletes a node and all its descendants from the tree
    /// </summary>
    /// <param name="nodeId">ID of the node to delete</param>
    /// <returns>Width of the deleted subtree</returns>
    Task<int> DeleteNodeAsync(Guid nodeId);

    /// <summary>
    /// Calculates the width (Rgt - Lft + 1) of a subtree
    /// </summary>
    /// <param name="leftValue">Left boundary value</param>
    /// <param name="rightValue">Right boundary value</param>
    /// <returns>Width of the subtree</returns>
    int CalculateTreeWidth(int leftValue, int rightValue);

    /// <summary>
    /// Validates if a move operation is valid (new parent is not a descendant)
    /// </summary>
    /// <param name="nodeToMove">The node being moved</param>
    /// <param name="newParent">The proposed new parent</param>
    /// <returns>True if the move is valid</returns>
    bool ValidateTreeMovement(Group nodeToMove, Group newParent);

    /// <summary>
    /// Repairs nested set values after corruption or inconsistency
    /// </summary>
    /// <param name="rootId">Root node to repair from</param>
    Task RepairNestedSetValuesAsync(Guid rootId);

    /// <summary>
    /// Updates Lft/Rgt values for all nodes after a tree structure change
    /// </summary>
    /// <param name="rootId">Root of the affected tree</param> 
    /// <param name="startPosition">Position to start updates from</param>
    /// <param name="adjustment">Amount to adjust values by</param>
    Task UpdateTreePositionsAsync(Guid rootId, int startPosition, int adjustment);
}