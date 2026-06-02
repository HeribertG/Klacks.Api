// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

public record CustomerGroupingAssignment(
    Guid ClientId,
    string ClientName,
    IReadOnlyList<string> CurrentGroupNames,
    Guid TargetGroupId,
    string TargetGroupName,
    double DistanceKm,
    IReadOnlyList<Guid> RetireGroupIds);
