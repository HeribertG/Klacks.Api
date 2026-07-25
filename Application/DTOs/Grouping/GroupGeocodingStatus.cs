// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// Aggregate progress of the background group-geocoding queue across all non-deleted groups.
/// </summary>
/// <param name="TotalGroups">All non-deleted groups, regardless of geocoding state.</param>
/// <param name="WithCoordinates">Groups successfully resolved to a location.</param>
/// <param name="AttemptedNotAPlaceOrFailed">Groups the resolver already ran for but that stayed without coordinates — classified as not a place, or the geocode lookup itself failed.</param>
/// <param name="Pending">Groups never processed yet — still queued, or lost from the in-memory queue by a restart; call geocode_location_groups to (re-)enqueue them.</param>
public record GroupGeocodingStatus(
    int TotalGroups,
    int WithCoordinates,
    int AttemptedNotAPlaceOrFailed,
    int Pending);
