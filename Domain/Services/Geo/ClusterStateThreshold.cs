// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Geo;

/// <summary>
/// The centre threshold the cluster planner applied to one state.
/// </summary>
/// <param name="Country">Country code of the state.</param>
/// <param name="State">State code.</param>
/// <param name="Population">Number of addresses of the state the structure was computed from.</param>
/// <param name="MinimumForCentre">Addresses a city needs to become a centre (the largest city is a centre regardless).</param>
public sealed record ClusterStateThreshold(
    string Country,
    string State,
    int Population,
    int MinimumForCentre);
