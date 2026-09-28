// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Requests the aggregate progress of the background group-geocoding queue (how many groups have
/// coordinates, how many were classified but stayed without one, how many are still pending). Read-only.
/// </summary>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Grouping;

public record GetGroupGeocodingStatusQuery : IRequest<GroupGeocodingStatus>;
