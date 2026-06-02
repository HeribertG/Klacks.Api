// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

public enum GroupLocationResolveOutcome
{
    Resolved,
    AlreadySet,
    NotAPlace,
    GeocodeFailed,
    NotFound
}
