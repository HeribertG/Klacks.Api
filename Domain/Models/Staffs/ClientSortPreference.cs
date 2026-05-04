// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Stores the per-user, per-group display order for a client in the schedule view.
/// </summary>
/// <param name="UserId">Identity user ID (string from IdentityUser.Id)</param>
/// <param name="GroupId">Group the sort order applies to</param>
/// <param name="ClientId">The client being positioned</param>
/// <param name="SortOrder">0-based position index</param>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.Staffs;

public class ClientSortPreference : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public Guid GroupId { get; set; }
    public Guid ClientId { get; set; }
    public int SortOrder { get; set; }
}
