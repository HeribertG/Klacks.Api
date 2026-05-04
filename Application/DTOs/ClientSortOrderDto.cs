// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Transfer object for a single client sort position.
/// </summary>
/// <param name="ClientId">The client's unique ID</param>
/// <param name="SortOrder">0-based position in the user's sort order for the group</param>

namespace Klacks.Api.Application.DTOs;

public record ClientSortOrderDto(Guid ClientId, int SortOrder);
