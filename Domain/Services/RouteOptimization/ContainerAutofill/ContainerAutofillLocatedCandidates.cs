// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

/// <summary>
/// What locating the tasks of an autofill found, before anything is stored.
/// </summary>
/// <param name="Locations">Route candidates, one per distinct address</param>
/// <param name="CoordinateUpdates">Freshly geocoded coordinates the real run stores</param>
/// <param name="Unlocated">Tasks without a candidate and why</param>
public sealed record ContainerAutofillLocatedCandidates(
    List<Location> Locations,
    List<ContainerAutofillCoordinateUpdate> CoordinateUpdates,
    List<ContainerAutofillUnlocatedTask> Unlocated);
