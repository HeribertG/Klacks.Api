// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Staffs;

namespace Klacks.Api.Domain.Interfaces.Associations;

/// <summary>
/// Domain service for managing group-client relationships and membership operations
/// </summary>
public interface IGroupMembershipService
{
    /// <summary>
    /// Updates group membership by managing GroupItem associations
    /// </summary>
    /// <param name="groupId">ID of the group</param>
    /// <param name="newClientIds">Collection of client IDs that should be members</param>
    /// <param name="shiftId">ID of the shift (optional)</param>
    /// <returns>Task representing the async operation</returns>
    Task UpdateGroupMembershipAsync(Guid groupId, IEnumerable<Guid> newClientIds, Guid? shiftId = null);

    /// <summary>
    /// Adds a client to a group
    /// </summary>
    /// <param name="groupId">ID of the group</param>
    /// <param name="clientId">ID of the client to add</param>
    /// <param name="shiftId">ID of the shift (optional)</param>
    /// <returns>Task representing the async operation</returns>
    Task AddClientToGroupAsync(Guid groupId, Guid clientId, Guid? shiftId = null);

    /// <summary>
    /// Removes a client from a group
    /// </summary>
    /// <param name="groupId">ID of the group</param>
    /// <param name="clientId">ID of the client to remove</param>
    /// <returns>Task representing the async operation</returns>
    Task RemoveClientFromGroupAsync(Guid groupId, Guid clientId);

    /// <summary>
    /// Gets all clients that are members of a specific group
    /// </summary>
    /// <param name="groupId">ID of the group</param>
    /// <returns>Collection of clients in the group</returns>
    Task<IEnumerable<Client>> GetGroupMembersAsync(Guid groupId);

    /// <summary>
    /// Gets all groups that a client is a member of
    /// </summary>
    /// <param name="clientId">ID of the client</param>
    /// <returns>Collection of groups the client belongs to</returns>
    Task<IEnumerable<Group>> GetClientGroupsAsync(Guid clientId);

    /// <summary>
    /// Checks if a client is a member of a specific group
    /// </summary>
    /// <param name="groupId">ID of the group</param>
    /// <param name="clientId">ID of the client</param>
    /// <returns>True if client is a member of the group</returns>
    Task<bool> IsClientInGroupAsync(Guid groupId, Guid clientId);

    /// <summary>
    /// Gets the count of clients in a group
    /// </summary>
    /// <param name="groupId">ID of the group</param>
    /// <returns>Number of clients in the group</returns>
    Task<int> GetGroupMemberCountAsync(Guid groupId);

    /// <summary>
    /// Gets all clients that are members of a group or any of its descendant groups
    /// </summary>
    /// <param name="groupId">ID of the parent group</param>
    /// <returns>Collection of clients in the group hierarchy</returns>
    Task<IEnumerable<Client>> GetGroupHierarchyMembersAsync(Guid groupId);

    /// <summary>
    /// Bulk adds multiple clients to a group
    /// </summary>
    /// <param name="groupId">ID of the group</param>
    /// <param name="clientIds">Collection of client IDs to add</param>
    /// <param name="shiftId">ID of the shift (optional)</param>
    /// <returns>Task representing the async operation</returns>
    Task BulkAddClientsToGroupAsync(Guid groupId, IEnumerable<Guid> clientIds, Guid? shiftId = null);

    /// <summary>
    /// Bulk removes multiple clients from a group
    /// </summary>
    /// <param name="groupId">ID of the group</param>
    /// <param name="clientIds">Collection of client IDs to remove</param>
    /// <returns>Task representing the async operation</returns>
    Task BulkRemoveClientsFromGroupAsync(Guid groupId, IEnumerable<Guid> clientIds);
}