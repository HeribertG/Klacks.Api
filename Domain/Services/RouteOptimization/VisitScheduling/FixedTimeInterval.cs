// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A fixed absence (break) of a container day in minutes since midnight; an interval crossing midnight or with equal
/// bounds ends one day later (EndMinutes above 1440).
/// </summary>

namespace Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

public record FixedTimeInterval(int StartMinutes, int EndMinutes);
