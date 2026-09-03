// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// One client placed into the leaf group (city, canton or, at city level, a flat city node) of a
/// GroupPartitionPlanner plan.
/// </summary>
/// <param name="ClientId">The client that would be (or was) added to the leaf group.</param>
/// <param name="ClientName">Display name of the client, for diagnostics.</param>
/// <param name="LeafGroupKey">Key of the PlannedPartitionGroup this client belongs to.</param>
public sealed record PartitionClientAssignment(Guid ClientId, string ClientName, string LeafGroupKey);
