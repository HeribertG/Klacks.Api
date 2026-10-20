// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Geo;

/// <summary>
/// A place that is not a cluster centre and joined a centre of its state as a whole.
/// </summary>
/// <param name="Country">Country code of the place.</param>
/// <param name="State">State code of the place.</param>
/// <param name="Place">Canonical name of the place.</param>
/// <param name="CentreCity">Centre city of the cluster the place joined.</param>
/// <param name="ClientCount">Addresses of the place.</param>
/// <param name="LocatedAddressCount">Addresses of the place that carry coordinates; the place position is their mean.</param>
/// <param name="DistanceKm">Distance from the place position to the centre; null when the state has a single centre and the distance could not be measured.</param>
public sealed record ClusterPlaceAttachment(
    string Country,
    string State,
    string Place,
    string CentreCity,
    int ClientCount,
    int LocatedAddressCount,
    double? DistanceKm);
