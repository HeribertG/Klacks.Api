// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Geocoding;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Geocoding;

public record SearchGeocodingQuery(string Query, string? Language) : IRequest<GeocodingSearchResource>;
