// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default strictness of the task TimeRange windows in the container autofill. The request default is what an
/// autofill call gets when the caller states no tolerance; the algorithm default is the fallback of the route
/// algorithm's own methods when they are called without one. Both are fractions: 1.0 enforces the windows
/// strictly, lower values widen them, 0 or less disables the check.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class ContainerAutofillDefaults
{
    public const double RequestTimeRangeTolerance = 0.5;

    public const double AlgorithmTimeRangeTolerance = 0.8;
}
