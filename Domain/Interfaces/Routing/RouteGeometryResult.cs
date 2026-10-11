// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Interfaces.Routing;

public record RouteGeometryResult(RouteGeometryStatus Status, IReadOnlyList<RoutePoint> Points);
