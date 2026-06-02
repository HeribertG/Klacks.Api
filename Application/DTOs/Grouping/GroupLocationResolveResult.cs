// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

public record GroupLocationResolveResult(
    Guid GroupId,
    string GroupName,
    GroupLocationResolveOutcome Outcome,
    double? Latitude,
    double? Longitude);
