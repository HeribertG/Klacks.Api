// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Settings.GeoServices;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Settings.GeoServices;

public record GetGeoServicesConfigQuery() : IRequest<GeoServicesConfigResource>;
