// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Routing;

public class RouteGeometryResource
{
    [JsonConverter(typeof(JsonStringEnumConverter<RouteGeometryStatus>))]
    public RouteGeometryStatus Status { get; set; }

    public List<RoutePointResource> Points { get; set; } = [];
}
