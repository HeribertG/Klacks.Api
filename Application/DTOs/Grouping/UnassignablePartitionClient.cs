// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// A client GroupPartitionPlanner could not place because its resolved current address is missing the
/// field(s) the requested partition level needs.
/// </summary>
/// <param name="ClientId">The client that could not be placed.</param>
/// <param name="ClientName">Display name of the client, for the preview.</param>
/// <param name="Reason">Why no group could be planned for this client (e.g. "no address on record").</param>
public sealed record UnassignablePartitionClient(Guid ClientId, string ClientName, string Reason);
