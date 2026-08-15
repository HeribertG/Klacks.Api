// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Hubs;

/// <summary>
/// Schedule connections of one scenario partitioned by their group filter.
/// </summary>
/// <param name="Ungrouped">Connections without a group filter; they see every client</param>
/// <param name="ByGroup">Connections that filter on exactly one group, keyed by that group id</param>
public sealed record GroupedScheduleConnections(
    IReadOnlyList<ScheduleConnectionSnapshot> Ungrouped,
    IReadOnlyDictionary<Guid, IReadOnlyList<ScheduleConnectionSnapshot>> ByGroup);
